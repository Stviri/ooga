using System.Globalization;
using System.Text.RegularExpressions;

namespace Ooga;

// Runs an ooga program line by line. See Values.cs for how ooga values look in C#.
public class Interpreter
{
    enum Flow { Normal, Stop, Skip, Give }

    static readonly Regex NumberAnswer = new(@"^-?[0-9]+(\.[0-9]+)?$");

    readonly IOogaHost host;
    readonly RunOptions options;
    readonly Dictionary<string, object> globals = new();
    readonly Dictionary<string, CanStmt> actions = new();
    readonly Dictionary<string, OogaAction> builtIns;
    readonly Random rng;
    readonly DateTime started = DateTime.UtcNow;
    Dictionary<string, object> locals;   // null when not inside an action
    object giveValue;
    int depth;
    long steps;

    internal Interpreter(IOogaHost host, RunOptions options)
    {
        this.host = host;
        this.options = options;
        builtIns = OogaRunner.KnownActions(options);
        rng = options.Seed is int seed ? new Random(seed) : new Random();
    }

    // Runs a checked program (see OogaRunner.Compile). Throws OogaError on problems found while running.
    public static RunEnd Run(OogaProgram program, IOogaHost host, RunOptions options = null)
    {
        var it = new Interpreter(host, options ?? new RunOptions());
        foreach (var s in program.Body)
            if (s is CanStmt can) it.actions[can.Name] = can;
        try
        {
            it.ExecBlock(program.Body);
        }
        catch (DieSignal)
        {
            return RunEnd.Died;
        }
        return RunEnd.Finished;
    }

    // For talk mode: run one more piece of program, keeping the things and actions from before.
    internal RunEnd RunMore(OogaProgram program)
    {
        foreach (var s in program.Body)
            if (s is CanStmt can) actions[can.Name] = can;
        locals = null;
        depth = 0;
        LastAnswer = null;
        try
        {
            // A piece that is only one action use: keep its answer, so talk mode can show it.
            if (program.Body.Count == 1 && program.Body[0] is CallStmt only)
            {
                Step(only);
                LastAnswer = Call(only.Call);
            }
            else
            {
                ExecBlock(program.Body);
            }
        }
        catch (DieSignal)
        {
            return RunEnd.Died;
        }
        return RunEnd.Finished;
    }

    internal object LastAnswer { get; private set; }
    internal IEnumerable<string> ThingNames => globals.Keys;
    internal IReadOnlyDictionary<string, CanStmt> Actions => actions;

    // Counts work done, so tests (or an engine) can stop a program that never ends.
    void Step(Node at)
    {
        options.Cancel.ThrowIfCancellationRequested();
        if (options.MaxSteps > 0 && ++steps > options.MaxSteps)
            throw new OogaError(at, $"ooga tired. program take more than {options.MaxSteps} steps. maybe a loop never end?") { CanCatch = false };
    }

    static OogaError Problem(Node at, string message) => new(at, message);

    // ---------- lines ----------

    Flow ExecBlock(List<Stmt> body)
    {
        foreach (var s in body)
        {
            var flow = Exec(s);
            if (flow != Flow.Normal) return flow;
        }
        return Flow.Normal;
    }

    Flow Exec(Stmt s)
    {
        Step(s);
        switch (s)
        {
            case KindStmt:
            case CanStmt:
            case UseStmt:
                return Flow.Normal;

            case HasStmt h:
                (locals ?? globals)[h.Name] = Eval(h.Value);
                return Flow.Normal;

            case SetStmt set:
                Store(set.Target, Eval(set.Value));
                return Flow.Normal;

            case ChangeStmt ch:
            {
                object cur = Eval(ch.Target);
                object amount = Eval(ch.Amount);
                string verb = ch.Gain ? "gain" : "lose";
                switch (cur)
                {
                    case string text when ch.Gain:
                        Store(ch.Target, text + Values.Show(amount));
                        break;
                    case double a when amount is double b:
                        Store(ch.Target, Finite(ch, ch.Gain ? a + b : a - b));
                        break;
                    case OogaList list when ch.Gain:
                        list.Items.Add(amount);
                        break;
                    case OogaList list:
                    {
                        int at = list.Items.FindIndex(x => Values.Same(x, amount));
                        if (at < 0) throw Problem(ch, $"list no have {Values.Describe(amount)} to lose.");
                        list.Items.RemoveAt(at);
                        break;
                    }
                    default:
                        throw Problem(ch, $"{TargetName(ch.Target)} is {Values.Describe(cur)}. no can {verb} {Values.Describe(amount)}.");
                }
                return Flow.Normal;
            }

            case SayStmt say:
                host.Say(Values.Show(Eval(say.Value)));
                return Flow.Normal;

            case IfStmt i:
                foreach (var (cond, body) in i.Branches)
                    if (Truth(cond)) return ExecBlock(body);
                return i.Else != null ? ExecBlock(i.Else) : Flow.Normal;

            case RepeatStmt r:
            {
                double n = Number(Eval(r.Times), r.Times, "repeat");
                if (n < 0 || n != Math.Floor(n))
                    throw Problem(r.Times, $"repeat need whole number 0 or more, not {Values.Show(n)}.");
                for (long k = 0; k < (long)n; k++)
                {
                    Step(r);
                    var flow = ExecBlock(r.Body);
                    if (flow == Flow.Stop) break;
                    if (flow == Flow.Give) return flow;
                }
                return Flow.Normal;
            }

            case RepeatWhileStmt w:
                while (Truth(w.Cond))
                {
                    Step(w);
                    var flow = ExecBlock(w.Body);
                    if (flow == Flow.Stop) break;
                    if (flow == Flow.Give) return flow;
                }
                return Flow.Normal;

            case CountStmt c:
            {
                double from = Number(Eval(c.From), c.From, "count");
                double to = Number(Eval(c.To), c.To, "count");
                double step = from <= to ? 1 : -1;
                var scope = locals ?? globals;
                for (double v = from; step > 0 ? v <= to : v >= to; v += step)
                {
                    Step(c);
                    scope[c.Name] = v;
                    var flow = ExecBlock(c.Body);
                    if (flow == Flow.Stop) break;
                    if (flow == Flow.Give) return flow;
                }
                return Flow.Normal;
            }

            case EachStmt e:
            {
                object source = Eval(e.Source);
                IEnumerable<object> items = source switch
                {
                    OogaList list => list.Items.ToList(),                 // a copy, so the loop is safe if the list changes
                    string text => text.Select(ch => (object)ch.ToString()).ToList(),
                    OogaBox box => box.Keys.ToList(),
                    _ => throw Problem(e.Source, $"each need a list, a text or a box, but got {Values.Describe(source)}."),
                };
                var scope = locals ?? globals;
                foreach (var item in items)
                {
                    Step(e);
                    scope[e.Name] = item;
                    var flow = ExecBlock(e.Body);
                    if (flow == Flow.Stop) break;
                    if (flow == Flow.Give) return flow;
                }
                return Flow.Normal;
            }

            case TryStmt t:
            {
                int savedDepth = depth;
                var savedLocals = locals;
                try
                {
                    return ExecBlock(t.Body);
                }
                catch (OogaError problem) when (problem.CanCatch)
                {
                    depth = savedDepth;
                    locals = savedLocals;
                    if (t.OopsName != null) (locals ?? globals)[t.OopsName] = problem.Message;
                    return ExecBlock(t.Oops);
                }
            }

            case FailStmt f:
                throw Problem(f, Values.Show(Eval(f.Message)));

            case StopStmt: return Flow.Stop;
            case SkipStmt: return Flow.Skip;

            case GiveStmt g:
                giveValue = g.Value != null ? Eval(g.Value) : null;
                return Flow.Give;

            case WaitStmt wait:
            {
                double secs = Number(Eval(wait.Seconds), wait.Seconds, "wait");
                if (secs < 0) throw Problem(wait, "no can wait less than 0 seconds.");
                host.Wait(secs);
                return Flow.Normal;
            }

            case DieStmt:
                throw new DieSignal();

            case CallStmt call:
                Call(call.Call);
                return Flow.Normal;
        }
        throw Problem(s, "ooga engine no know this line. (this is engine bug, not your fault)");
    }

    // ---------- names, parts, items ----------

    object Lookup(string name, Node at)
    {
        if (locals != null && locals.TryGetValue(name, out var v)) return v;
        if (globals.TryGetValue(name, out v)) return v;
        throw Problem(at, $"\"{name}\" no have value yet. the \"me has {name}\" line must happen before this.");
    }

    // Puts a value into a thing, a part of a box, or an item of a list.
    void Store(Expr target, object value)
    {
        switch (target)
        {
            case NameExpr n:
                if (locals != null && locals.ContainsKey(n.Name)) locals[n.Name] = value;
                else if (globals.ContainsKey(n.Name)) globals[n.Name] = value;
                else throw Problem(n, $"\"{n.Name}\" no have value yet. the \"me has {n.Name}\" line must happen before this.");
                break;

            case PartExpr p:
            {
                object holder = Eval(p.Holder);
                if (holder is not OogaBox box)
                    throw Problem(p, $"only a box has parts. {TargetName(p.Holder)} is {Values.Describe(holder)}.");
                box.Set(p.Part, value);
                break;
            }

            case ItemExpr i:
            {
                object holder = Eval(i.Holder);
                object index = Eval(i.Index);
                switch (holder)
                {
                    case OogaList list:
                        list.Items[ListPosition(list, index, i.Index)] = value;
                        break;
                    case OogaBox box:
                        box.Set(BoxKey(index, i.Index), value);
                        break;
                    case string:
                        throw Problem(i, "no can change one letter of a text. make a new text instead, like: name is (me replace name \"a\" \"b\")");
                    default:
                        throw Problem(i, $"only a list or a box has items. this is {Values.Describe(holder)}.");
                }
                break;
            }

            default:
                throw Problem(target, "no can change this.");
        }
    }

    static string TargetName(Expr e) => e switch
    {
        NameExpr n => n.Name,
        PartExpr p => p.Part + " of " + TargetName(p.Holder),
        ItemExpr => "that item",
        _ => "that",
    };

    int ListPosition(OogaList list, object index, Node at)
    {
        if (index is not double d || d != Math.Floor(d))
            throw Problem(at, $"list items are counted 1, 2, 3... need a whole number, but got {Values.Describe(index)}.");
        if (d < 1 || d > list.Items.Count)
            throw Problem(at, list.Items.Count == 0
                ? $"list is empty. no item {Values.Show(d)}."
                : $"list has {Library.CountText(list.Items.Count)} (1 to {list.Items.Count}). no item {Values.Show(d)}.");
        return (int)d - 1;
    }

    object BoxKey(object key, Node at) => key switch
    {
        double or string or bool => key,
        _ => throw Problem(at, $"box part name must be a text, a number or yes/no, not {Values.Describe(key)}."),
    };

    // ---------- actions ----------

    object Call(CallExpr call) => CallAction(call.Name, call.Args.Select(Eval).ToArray(), call);

    // Runs an action held as a value ("me call f 5", or inside keep / change_each / sort_by).
    object CallValue(object f, object[] args, Node at)
    {
        if (f is not ActionValue av)
            throw Problem(at, $"need an action here (like: action double), but got {Values.Describe(f)}.");
        int want, most;
        if (actions.TryGetValue(av.Name, out var can)) want = most = can.Params.Count;
        else (want, most) = (builtIns[av.Name].MinThings, builtIns[av.Name].MaxThings);
        if (args.Length < want || (most >= 0 && args.Length > most))
            throw Problem(at, $"action {av.Name} want {(want == most ? Library.CountText(want) : $"{want} or more things")} but got {Library.CountText(args.Length)}.");
        return CallAction(av.Name, args, at);
    }

    object CallAction(string name, object[] args, Node call)
    {
        if (!actions.TryGetValue(name, out var can))
        {
            var built = builtIns[name];
            try
            {
                return built.Run(new ActionCall
                {
                    Name = name, Things = args, At = call, Host = host,
                    Random = rng, Options = options, Started = started,
                    Invoke = (f, things) => CallValue(f, things, call),
                });
            }
            catch (OogaError e)
            {
                e.File ??= call.File;
                throw;
            }
        }

        if (++depth > options.MaxDepth)
            throw Problem(call, $"me dizzy. {name} called too many times inside itself. need a way to stop. (math given to an action need ( ) around it, like: me {name} (n - 1))");

        var saved = locals;
        locals = new Dictionary<string, object>();
        for (int k = 0; k < args.Length; k++) locals[can.Params[k]] = args[k];
        try
        {
            giveValue = null;
            var flow = ExecBlock(can.Body);
            return flow == Flow.Give ? giveValue : null;
        }
        finally
        {
            locals = saved;
            depth--;
        }
    }

    // ---------- values ----------

    object Eval(Expr e)
    {
        switch (e)
        {
            case LiteralExpr lit:
                return lit.Value;

            case NameExpr n:
                return Lookup(n.Name, n);

            case MathExpr m:
            {
                object l = Eval(m.Left), r = Eval(m.Right);
                if (m.Op == "+")
                {
                    if (l is string || r is string) return Values.Show(l) + Values.Show(r);
                    if (l is OogaList la && r is OogaList lb) return new OogaList(la.Items.Concat(lb.Items));
                }
                if (l is not double a || r is not double b)
                    throw Problem(m, $"no can do {Values.Describe(l)} {m.Op} {Values.Describe(r)}. math need numbers.");
                if ((m.Op == "/" || m.Op == "%") && b == 0)
                    throw Problem(m, "no can split by 0.");
                double result = m.Op switch
                {
                    "+" => a + b,
                    "-" => a - b,
                    "*" => a * b,
                    "/" => a / b,
                    _ => a - b * Math.Floor(a / b),   // what is left over; never below 0 when b is above 0
                };
                return Finite(m, result);
            }

            case NegateExpr neg:
                return -Number(Eval(neg.Inner), neg.Inner, "-");

            case CompareExpr c:
            {
                object l = Eval(c.Left), r = Eval(c.Right);
                bool result;
                if (c.Op == CompareOp.Same)
                {
                    result = Values.Same(l, r);
                }
                else
                {
                    int order;
                    if (l is double a && r is double b) order = a.CompareTo(b);
                    else if (l is string sa && r is string sb) order = string.CompareOrdinal(sa, sb);
                    else
                        throw Problem(c, $"no can check if {Values.Describe(l)} is big or small than {Values.Describe(r)}. need two numbers or two texts.");
                    result = c.Op switch
                    {
                        CompareOp.Big => order > 0,
                        CompareOp.Small => order < 0,
                        CompareOp.BigOrSame => order >= 0,
                        _ => order <= 0,
                    };
                }
                return c.Not ? !result : result;
            }

            case HasExpr h:
            {
                object holder = Eval(h.Holder), item = Eval(h.Item);
                return holder switch
                {
                    OogaList list => list.Items.Any(x => Values.Same(x, item)),
                    OogaBox box => box.Has(item),
                    string text when item is string part => text.Contains(part, StringComparison.Ordinal),
                    string => throw Problem(h, $"text can only have text inside, not {Values.Describe(item)}."),
                    _ => throw Problem(h, $"only a list, a box or a text can have things. this is {Values.Describe(holder)}."),
                };
            }

            case LogicExpr l:
                return l.Op == "and"
                    ? Truth(l.Left) && Truth(l.Right)
                    : Truth(l.Left) || Truth(l.Right);

            case NotExpr not:
                return !Truth(not.Inner);

            case AskExpr ask:
            {
                string prompt = ask.Prompt != null ? Values.Show(Eval(ask.Prompt)) : null;
                string answer = host.Ask(prompt);
                if (answer == null)
                    throw Problem(ask, "ooga ask, but no more answers can come. (the typing ended)");
                answer = answer.Trim().Trim('﻿').Trim();
                // Only plain numbers like 5, -3, 2.5 become numbers. Everything else stays text.
                if (NumberAnswer.IsMatch(answer))
                    return double.Parse(answer, CultureInfo.InvariantCulture);
                return answer;
            }

            case RandomExpr rnd:
            {
                double lo = Number(Eval(rnd.Low), rnd.Low, "random");
                double hi = Number(Eval(rnd.High), rnd.High, "random");
                if (lo != Math.Floor(lo) || hi != Math.Floor(hi))
                    throw Problem(rnd, "random need whole numbers, like: random 1 to 6");
                if (lo > hi) (lo, hi) = (hi, lo);
                return (double)rng.NextInt64((long)lo, (long)hi + 1);
            }

            case ListExpr list:
                return new OogaList(list.Items.Select(Eval).ToList());

            case BoxExpr boxExpr:
            {
                var box = new OogaBox();
                foreach (var (name, value) in boxExpr.Parts) box.Set(name, Eval(value));
                return box;
            }

            case PartExpr p:
            {
                object holder = Eval(p.Holder);
                if (holder is not OogaBox box)
                    throw Problem(p, $"only a box has parts. {TargetName(p.Holder)} is {Values.Describe(holder)}, so it no have {p.Part}.");
                if (!box.TryGet(p.Part, out var value))
                    throw Problem(p, MissingPart(box, p.Part));
                return value;
            }

            case ItemExpr i:
            {
                object holder = Eval(i.Holder);
                object index = Eval(i.Index);
                switch (holder)
                {
                    case OogaList list:
                        return list.Items[ListPosition(list, index, i.Index)];
                    case string text:
                    {
                        if (index is not double d || d != Math.Floor(d) || d < 1 || d > text.Length)
                            throw Problem(i.Index, $"text has {text.Length} letters (1 to {text.Length}). no letter {Values.Show(index)}.");
                        return text[(int)d - 1].ToString();
                    }
                    case OogaBox box:
                        if (!box.TryGet(BoxKey(index, i.Index), out var value))
                            throw Problem(i, MissingPart(box, Values.Show(index)));
                        return value;
                    default:
                        throw Problem(i, $"only a list, a text or a box has items. this is {Values.Describe(holder)}.");
                }
            }

            case SizeExpr size:
            {
                object v = Eval(size.Inner);
                return v switch
                {
                    OogaList list => (double)list.Items.Count,
                    string text => (double)text.Length,
                    OogaBox box => (double)box.Count,
                    _ => throw Problem(size, $"size of {Values.Describe(v)}? only lists, texts and boxes have a size."),
                };
            }

            case KindExpr kind:
                return Values.KindName(Eval(kind.Inner));

            case ActionRefExpr r:
                return new ActionValue(r.Name);

            case CallExpr call:
                return Call(call);
        }
        throw Problem(e, "ooga engine no know this value. (this is engine bug, not your fault)");
    }

    static string MissingPart(OogaBox box, string part)
    {
        if (box.Count == 0) return $"box is empty. no part {part}.";
        var names = box.Keys.Select(Values.Show).ToList();
        string hint = Spelling.Suggest(part, names);
        return hint != null
            ? $"box no have part {part}. you mean {hint}?"
            : $"box no have part {part}. it has: {string.Join(", ", names.Take(10))}";
    }

    static object Finite(Node at, double d) =>
        double.IsInfinity(d) || double.IsNaN(d) ? throw Problem(at, "number too big for ooga.") : d;

    bool Truth(Expr e)
    {
        object v = Eval(e);
        if (v is bool b) return b;
        throw Problem(e, $"need yes or no here, but got {Values.Describe(v)}. try a check, like: x is big 0");
    }

    static double Number(object v, Node at, string who)
    {
        if (v is double d) return d;
        throw Problem(at, $"{who} need a number, but got {Values.Describe(v)}.");
    }
}
