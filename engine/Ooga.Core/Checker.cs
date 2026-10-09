namespace Ooga;

// Looks over the whole program before running it, to catch typos and missing names early.
public class Checker
{
    readonly Dictionary<string, CanStmt> actions = new();
    readonly IReadOnlyDictionary<string, OogaAction> builtIns;
    readonly HashSet<string> globals = new();

    Checker(IReadOnlyDictionary<string, OogaAction> builtIns) { this.builtIns = builtIns; }

    public static void Check(OogaProgram program, IReadOnlyDictionary<string, OogaAction> builtIns = null) =>
        new Checker(builtIns ?? Library.Standard).Run(program);

    void Run(OogaProgram program)
    {
        // Pass 1: learn every action and every top-level thing "me has".
        foreach (var s in program.Body)
        {
            if (s is CanStmt can)
            {
                if (actions.TryGetValue(can.Name, out var old))
                    throw new OogaError(can, $"me already know how to {can.Name} ({Where(old, can)}). each action only taught once.");
                if (can.Name == "die" || builtIns.ContainsKey(can.Name))
                    throw new OogaError(can, $"\"{can.Name}\" is built in. me already know how. pick other action name.");
                actions[can.Name] = can;
            }
        }
        CollectNames(program.Body, globals, new Dictionary<string, Node>());

        // Pass 2: check every line.
        CheckBlock(program.Body, globals, loops: 0, inAction: false);

        foreach (var can in actions.Values)
        {
            var scope = new HashSet<string>(globals);
            var seen = new Dictionary<string, Node>();
            foreach (var param in can.Params)
            {
                if (seen.ContainsKey(param))
                    throw new OogaError(can, $"action {can.Name} has \"{param}\" two times. give each thing different name.");
                scope.Add(param);
                seen[param] = can;
            }
            CollectNames(can.Body, scope, seen);
            CheckBlock(can.Body, scope, loops: 0, inAction: true);
        }
    }

    static string Where(Node old, Node now) =>
        old.File == now.File ? $"line {old.Line}" : $"{old.File} line {old.Line}";

    // Finds names made with "me has", "count", "each" and "oops" inside a block (not inside actions).
    static void CollectNames(List<Stmt> body, HashSet<string> into, Dictionary<string, Node> made)
    {
        foreach (var s in body)
        {
            switch (s)
            {
                case HasStmt h:
                    if (made.TryGetValue(h.Name, out var old))
                        throw new OogaError(h, $"me already has {h.Name} ({Where(old, h)}). to change it, write: {h.Name} is ...");
                    made[h.Name] = h;
                    into.Add(h.Name);
                    break;
                case CountStmt c:
                    into.Add(c.Name);
                    CollectNames(c.Body, into, made);
                    break;
                case EachStmt e:
                    into.Add(e.Name);
                    CollectNames(e.Body, into, made);
                    break;
                case TryStmt t:
                    CollectNames(t.Body, into, made);
                    if (t.OopsName != null) into.Add(t.OopsName);
                    CollectNames(t.Oops, into, made);
                    break;
                case IfStmt i:
                    foreach (var b in i.Branches) CollectNames(b.Body, into, made);
                    if (i.Else != null) CollectNames(i.Else, into, made);
                    break;
                case RepeatStmt r: CollectNames(r.Body, into, made); break;
                case RepeatWhileStmt w: CollectNames(w.Body, into, made); break;
            }
        }
    }

    void CheckBlock(List<Stmt> body, HashSet<string> scope, int loops, bool inAction)
    {
        foreach (var s in body)
        {
            switch (s)
            {
                case KindStmt:
                case DieStmt:
                case CanStmt:
                case UseStmt:
                    break;
                case HasStmt h: CheckExpr(h.Value, scope); break;
                case SetStmt set: CheckTarget(set.Target, scope); CheckExpr(set.Value, scope); break;
                case ChangeStmt ch: CheckTarget(ch.Target, scope); CheckExpr(ch.Amount, scope); break;
                case SayStmt say: CheckExpr(say.Value, scope); break;
                case WaitStmt wait: CheckExpr(wait.Seconds, scope); break;
                case FailStmt fail: CheckExpr(fail.Message, scope); break;
                case CallStmt call: CheckExpr(call.Call, scope); break;
                case IfStmt i:
                    foreach (var b in i.Branches)
                    {
                        CheckExpr(b.Cond, scope);
                        CheckBlock(b.Body, scope, loops, inAction);
                    }
                    if (i.Else != null) CheckBlock(i.Else, scope, loops, inAction);
                    break;
                case RepeatStmt r:
                    CheckExpr(r.Times, scope);
                    CheckBlock(r.Body, scope, loops + 1, inAction);
                    break;
                case RepeatWhileStmt w:
                    CheckExpr(w.Cond, scope);
                    CheckBlock(w.Body, scope, loops + 1, inAction);
                    break;
                case CountStmt c:
                    CheckExpr(c.From, scope);
                    CheckExpr(c.To, scope);
                    CheckBlock(c.Body, scope, loops + 1, inAction);
                    break;
                case EachStmt e:
                    CheckExpr(e.Source, scope);
                    CheckBlock(e.Body, scope, loops + 1, inAction);
                    break;
                case TryStmt t:
                    CheckBlock(t.Body, scope, loops, inAction);
                    CheckBlock(t.Oops, scope, loops, inAction);
                    break;
                case StopStmt:
                    if (loops == 0) throw new OogaError(s, "stop only work inside repeat, count or each.");
                    break;
                case SkipStmt:
                    if (loops == 0) throw new OogaError(s, "skip only work inside repeat, count or each.");
                    break;
                case GiveStmt g:
                    if (!inAction) throw new OogaError(s, "give only work inside \"me can\" action.");
                    if (g.Value != null) CheckExpr(g.Value, scope);
                    break;
                default:
                    throw new OogaError(s, "ooga checker no know this line. (this is engine bug, not your fault)");
            }
        }
    }

    // The left side of "is", "gain", "lose": a thing, a part of a box, or an item of a list.
    void CheckTarget(Expr target, HashSet<string> scope)
    {
        switch (target)
        {
            case NameExpr n: CheckName(n.Name, n, scope); break;
            case PartExpr p: CheckExpr(p.Holder, scope); break;
            case ItemExpr i: CheckExpr(i.Index, scope); CheckExpr(i.Holder, scope); break;
            default: throw new OogaError(target, "no can change this. only things, parts (health of player) and items (item 1 of bag) can change.");
        }
    }

    void CheckExpr(Expr e, HashSet<string> scope)
    {
        switch (e)
        {
            case LiteralExpr:
                break;
            case NameExpr n:
                CheckName(n.Name, n, scope);
                break;
            case MathExpr m: CheckExpr(m.Left, scope); CheckExpr(m.Right, scope); break;
            case NegateExpr neg: CheckExpr(neg.Inner, scope); break;
            case CompareExpr c: CheckExpr(c.Left, scope); CheckExpr(c.Right, scope); break;
            case HasExpr h: CheckExpr(h.Holder, scope); CheckExpr(h.Item, scope); break;
            case LogicExpr l: CheckExpr(l.Left, scope); CheckExpr(l.Right, scope); break;
            case NotExpr not: CheckExpr(not.Inner, scope); break;
            case AskExpr ask: if (ask.Prompt != null) CheckExpr(ask.Prompt, scope); break;
            case RandomExpr r: CheckExpr(r.Low, scope); CheckExpr(r.High, scope); break;
            case ListExpr list: foreach (var item in list.Items) CheckExpr(item, scope); break;
            case BoxExpr box: foreach (var part in box.Parts) CheckExpr(part.Value, scope); break;
            case PartExpr p: CheckExpr(p.Holder, scope); break;
            case ItemExpr i: CheckExpr(i.Index, scope); CheckExpr(i.Holder, scope); break;
            case SizeExpr size: CheckExpr(size.Inner, scope); break;
            case KindExpr kind: CheckExpr(kind.Inner, scope); break;
            case ActionRefExpr r:
                if (!actions.ContainsKey(r.Name) && !builtIns.ContainsKey(r.Name))
                {
                    string hint = Spelling.Suggest(r.Name, actions.Keys.Concat(builtIns.Keys));
                    throw new OogaError(r, hint != null
                        ? $"me no know action {r.Name}. you mean {hint}?"
                        : $"me no know action {r.Name}. teach me first with: me can {r.Name}");
                }
                break;
            case CallExpr call:
                CheckCall(call);
                foreach (var a in call.Args) CheckExpr(a, scope);
                break;
            default:
                throw new OogaError(e, "ooga checker no know this value. (this is engine bug, not your fault)");
        }
    }

    void CheckCall(CallExpr call)
    {
        if (actions.TryGetValue(call.Name, out var can))
        {
            if (call.Args.Count != can.Params.Count)
                throw new OogaError(call,
                    $"{call.Name} want {Things(can.Params.Count)} but got {Things(call.Args.Count)}. (see {Where(can, call)})");
            return;
        }
        if (builtIns.TryGetValue(call.Name, out var built))
        {
            int n = call.Args.Count;
            if (n < built.MinThings || (built.MaxThings >= 0 && n > built.MaxThings))
                throw new OogaError(call, $"{call.Name} want {built.ThingsText()} but got {Things(n)}.");
            return;
        }
        string hint = Spelling.Suggest(call.Name, actions.Keys.Concat(builtIns.Keys));
        throw new OogaError(call,
            hint != null
                ? $"me no know how to {call.Name}. you mean {hint}?"
                : $"me no know how to {call.Name}. teach me first with: me can {call.Name}");
    }

    static string Things(int n) => n == 1 ? "1 thing" : $"{n} things";

    void CheckName(string name, Node at, HashSet<string> scope)
    {
        if (scope.Contains(name)) return;
        if (actions.TryGetValue(name, out var can))
            throw new OogaError(at, $"\"{name}\" is an action ({Where(can, at)}), not a thing. to use it, write: me {name}");
        string hint = Spelling.Suggest(name, scope);
        throw new OogaError(at,
            hint != null
                ? $"no thing called \"{name}\". you mean \"{hint}\"?"
                : $"no thing called \"{name}\". make it first with: me has {name} 0");
    }
}
