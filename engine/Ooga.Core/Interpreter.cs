using System.Globalization;
using System.Text.RegularExpressions;

namespace Ooga;

// Runs an ooga program line by line.
// Values inside ooga are: double (number), string (text), bool (yes/no), null (nothing).
public class Interpreter
{
    enum Flow { Normal, Stop, Skip, Give }

    static readonly Regex NumberAnswer = new(@"^-?[0-9]+(\.[0-9]+)?$");

    readonly IOogaHost host;
    readonly RunOptions options;
    readonly Dictionary<string, object> globals = new();
    readonly Dictionary<string, CanStmt> actions = new();
    readonly Random rng;
    Dictionary<string, object> locals;   // null when not inside an action
    object giveValue;
    int depth;
    long steps;

    Interpreter(IOogaHost host, RunOptions options)
    {
        this.host = host;
        this.options = options;
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

    // Counts work done, so tests (or an engine) can stop a program that never ends.
    void Step(Node at)
    {
        options.Cancel.ThrowIfCancellationRequested();
        if (options.MaxSteps > 0 && ++steps > options.MaxSteps)
            throw new OogaError(at, $"ooga tired. program take more than {options.MaxSteps} steps. maybe a loop never end?");
    }

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
                return Flow.Normal;

            case HasStmt h:
                (locals ?? globals)[h.Name] = Eval(h.Value);
                return Flow.Normal;

            case SetStmt set:
                Assign(set.Name, Eval(set.Value), set);
                return Flow.Normal;

            case ChangeStmt ch:
            {
                object cur = Lookup(ch.Name, ch);
                object amount = Eval(ch.Amount);
                if (ch.Gain && cur is string text)
                    Assign(ch.Name, text + Show(amount), ch);
                else if (cur is double a && amount is double b)
                    Assign(ch.Name, ch.Gain ? a + b : a - b, ch);
                else
                    throw new OogaError(ch.Line, ch.Col, $"{ch.Name} is {Describe(cur)}. no can {(ch.Gain ? "gain" : "lose")} {Describe(amount)}.");
                return Flow.Normal;
            }

            case SayStmt say:
                host.Say(Show(Eval(say.Value)));
                return Flow.Normal;

            case IfStmt i:
                foreach (var (cond, body) in i.Branches)
                    if (Truth(cond)) return ExecBlock(body);
                return i.Else != null ? ExecBlock(i.Else) : Flow.Normal;

            case RepeatStmt r:
            {
                double n = Number(Eval(r.Times), r.Times, "repeat");
                if (n < 0 || n != Math.Floor(n))
                    throw new OogaError(r.Times.Line, r.Times.Col, $"repeat need whole number 0 or more, not {Show(n)}.");
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

            case StopStmt: return Flow.Stop;
            case SkipStmt: return Flow.Skip;

            case GiveStmt g:
                giveValue = g.Value != null ? Eval(g.Value) : null;
                return Flow.Give;

            case WaitStmt wait:
            {
                double secs = Number(Eval(wait.Seconds), wait.Seconds, "wait");
                if (secs < 0) throw new OogaError(wait.Line, wait.Col, "no can wait less than 0 seconds.");
                host.Wait(secs);
                return Flow.Normal;
            }

            case DieStmt:
                throw new DieSignal();

            case CallStmt call:
                Call(call.Call);
                return Flow.Normal;
        }
        throw new OogaError(s.Line, s.Col, "ooga engine no know this line. (this is engine bug, not your fault)");
    }

    // ---------- names ----------

    object Lookup(string name, Node at)
    {
        if (locals != null && locals.TryGetValue(name, out var v)) return v;
        if (globals.TryGetValue(name, out v)) return v;
        throw new OogaError(at.Line, at.Col, $"\"{name}\" no have value yet. the \"me has {name}\" line must happen before this.");
    }

    void Assign(string name, object value, Node at)
    {
        if (locals != null && locals.ContainsKey(name)) locals[name] = value;
        else if (globals.ContainsKey(name)) globals[name] = value;
        else throw new OogaError(at.Line, at.Col, $"\"{name}\" no have value yet. the \"me has {name}\" line must happen before this.");
    }

    object Call(CallExpr call)
    {
        var can = actions[call.Name];
        var args = call.Args.Select(Eval).ToList();

        if (++depth > options.MaxDepth)
            throw new OogaError(call.Line, call.Col, $"me dizzy. {call.Name} called too many times inside itself. need a way to stop. (math given to an action need ( ) around it, like: me {call.Name} (n - 1))");

        var saved = locals;
        locals = new Dictionary<string, object>();
        for (int k = 0; k < args.Count; k++) locals[can.Params[k]] = args[k];
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
                if (m.Op == "+" && (l is string || r is string))
                    return Show(l) + Show(r);
                if (l is not double a || r is not double b)
                    throw new OogaError(m.Line, m.Col, $"no can do {Describe(l)} {m.Op} {Describe(r)}. math need numbers.");
                if (m.Op == "/" && b == 0)
                    throw new OogaError(m.Line, m.Col, "no can split by 0.");
                double result = m.Op switch
                {
                    "+" => a + b,
                    "-" => a - b,
                    "*" => a * b,
                    _ => a / b,
                };
                if (double.IsInfinity(result) || double.IsNaN(result))
                    throw new OogaError(m.Line, m.Col, "number too big for ooga.");
                return result;
            }

            case NegateExpr neg:
                return -Number(Eval(neg.Inner), neg.Inner, "-");

            case CompareExpr c:
            {
                object l = Eval(c.Left), r = Eval(c.Right);
                bool result;
                if (c.Op == CompareOp.Same)
                {
                    result = Equals(l, r);
                }
                else
                {
                    if (l is not double a || r is not double b)
                        throw new OogaError(c.Line, c.Col, $"no can check if {Describe(l)} is big or small than {Describe(r)}. need numbers.");
                    result = c.Op switch
                    {
                        CompareOp.Big => a > b,
                        CompareOp.Small => a < b,
                        CompareOp.BigOrSame => a >= b,
                        _ => a <= b,
                    };
                }
                return c.Not ? !result : result;
            }

            case LogicExpr l:
                return l.Op == "and"
                    ? Truth(l.Left) && Truth(l.Right)
                    : Truth(l.Left) || Truth(l.Right);

            case NotExpr not:
                return !Truth(not.Inner);

            case AskExpr ask:
            {
                string prompt = ask.Prompt != null ? Show(Eval(ask.Prompt)) : null;
                string answer = host.Ask(prompt);
                if (answer == null)
                    throw new OogaError(ask.Line, ask.Col, "ooga ask, but no more answers can come. (the typing ended)");
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
                    throw new OogaError(rnd.Line, rnd.Col, "random need whole numbers, like: random 1 to 6");
                if (lo > hi) (lo, hi) = (hi, lo);
                return (double)rng.NextInt64((long)lo, (long)hi + 1);
            }

            case CallExpr call:
                return Call(call);
        }
        throw new OogaError(e.Line, e.Col, "ooga engine no know this value. (this is engine bug, not your fault)");
    }

    bool Truth(Expr e)
    {
        object v = Eval(e);
        if (v is bool b) return b;
        throw new OogaError(e.Line, e.Col, $"need yes or no here, but got {Describe(v)}. try a check, like: x is big 0");
    }

    static double Number(object v, Node at, string who)
    {
        if (v is double d) return d;
        throw new OogaError(at.Line, at.Col, $"{who} need a number, but got {Describe(v)}.");
    }

    public static string Show(object v) => v switch
    {
        null => "nothing",
        bool b => b ? "yes" : "no",
        double d => FormatNumber(d),
        _ => v.ToString(),
    };

    static string Describe(object v) => v switch
    {
        null => "nothing",
        bool b => b ? "yes" : "no",
        double d => "number " + FormatNumber(d),
        string s => "text \"" + s + "\"",
        _ => v.ToString(),
    };

    static string FormatNumber(double d)
    {
        if (d == 0) return "0";
        return d.ToString("G15", CultureInfo.InvariantCulture);
    }
}
