namespace Ooga;

// Looks over the whole program before running it, to catch typos and missing names early.
public class Checker
{
    readonly Dictionary<string, CanStmt> actions = new();
    readonly HashSet<string> globals = new();

    public static void Check(OogaProgram program) => new Checker().Run(program);

    void Run(OogaProgram program)
    {
        // Pass 1: learn every action and every top-level thing "me has".
        foreach (var s in program.Body)
        {
            if (s is CanStmt can)
            {
                if (actions.TryGetValue(can.Name, out var old))
                    throw new OogaError(can.Line, can.Col, $"me already know how to {can.Name} (line {old.Line}). each action only taught once.");
                if (can.Name == "die")
                    throw new OogaError(can.Line, can.Col, "\"die\" is built in. pick other action name.");
                actions[can.Name] = can;
            }
        }
        CollectNames(program.Body, globals, new Dictionary<string, int>());

        // Pass 2: check every line.
        CheckBlock(program.Body, globals, loops: 0, inAction: false);

        foreach (var can in actions.Values)
        {
            var scope = new HashSet<string>(globals);
            var seen = new Dictionary<string, int>();
            foreach (var param in can.Params)
            {
                if (!scope.Add(param) && seen.ContainsKey(param))
                    throw new OogaError(can.Line, can.Col, $"action {can.Name} has \"{param}\" two times. give each thing different name.");
                seen[param] = can.Line;
            }
            CollectNames(can.Body, scope, seen);
            CheckBlock(can.Body, scope, loops: 0, inAction: true);
        }
    }

    // Finds names made with "me has" and "count" inside a block (not inside actions).
    static void CollectNames(List<Stmt> body, HashSet<string> into, Dictionary<string, int> hasLines)
    {
        foreach (var s in body)
        {
            switch (s)
            {
                case HasStmt h:
                    if (hasLines.TryGetValue(h.Name, out int line))
                        throw new OogaError(h.Line, h.Col, $"me already has {h.Name} (line {line}). to change it, write: {h.Name} is ...");
                    hasLines[h.Name] = h.Line;
                    into.Add(h.Name);
                    break;
                case CountStmt c:
                    into.Add(c.Name);
                    CollectNames(c.Body, into, hasLines);
                    break;
                case IfStmt i:
                    foreach (var b in i.Branches) CollectNames(b.Body, into, hasLines);
                    if (i.Else != null) CollectNames(i.Else, into, hasLines);
                    break;
                case RepeatStmt r: CollectNames(r.Body, into, hasLines); break;
                case RepeatWhileStmt w: CollectNames(w.Body, into, hasLines); break;
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
                    break;
                case HasStmt h: CheckExpr(h.Value, scope); break;
                case SetStmt set: CheckName(set.Name, set, scope); CheckExpr(set.Value, scope); break;
                case ChangeStmt ch: CheckName(ch.Name, ch, scope); CheckExpr(ch.Amount, scope); break;
                case SayStmt say: CheckExpr(say.Value, scope); break;
                case WaitStmt wait: CheckExpr(wait.Seconds, scope); break;
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
                case StopStmt:
                    if (loops == 0) throw new OogaError(s.Line, s.Col, "stop only work inside repeat or count.");
                    break;
                case SkipStmt:
                    if (loops == 0) throw new OogaError(s.Line, s.Col, "skip only work inside repeat or count.");
                    break;
                case GiveStmt g:
                    if (!inAction) throw new OogaError(s.Line, s.Col, "give only work inside \"me can\" action.");
                    if (g.Value != null) CheckExpr(g.Value, scope);
                    break;
            }
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
            case LogicExpr l: CheckExpr(l.Left, scope); CheckExpr(l.Right, scope); break;
            case NotExpr not: CheckExpr(not.Inner, scope); break;
            case AskExpr ask: if (ask.Prompt != null) CheckExpr(ask.Prompt, scope); break;
            case RandomExpr r: CheckExpr(r.Low, scope); CheckExpr(r.High, scope); break;
            case CallExpr call:
                if (!actions.TryGetValue(call.Name, out var can))
                {
                    string hint = Spelling.Suggest(call.Name, actions.Keys);
                    throw new OogaError(call.Line, call.Col,
                        hint != null
                            ? $"me no know how to {call.Name}. you mean {hint}?"
                            : $"me no know how to {call.Name}. teach me first with: me can {call.Name}");
                }
                if (call.Args.Count != can.Params.Count)
                    throw new OogaError(call.Line, call.Col,
                        $"{call.Name} want {Things(can.Params.Count)} but got {Things(call.Args.Count)}. (see line {can.Line})");
                foreach (var a in call.Args) CheckExpr(a, scope);
                break;
        }
    }

    static string Things(int n) => n == 1 ? "1 thing" : $"{n} things";

    void CheckName(string name, Node at, HashSet<string> scope)
    {
        if (scope.Contains(name)) return;
        if (actions.TryGetValue(name, out var can))
            throw new OogaError(at.Line, at.Col,
                $"\"{name}\" is an action (line {can.Line}), not a thing. to use it, write: me {name}");
        string hint = Spelling.Suggest(name, scope);
        throw new OogaError(at.Line, at.Col,
            hint != null
                ? $"no thing called \"{name}\". you mean \"{hint}\"?"
                : $"no thing called \"{name}\". make it first with: me has {name} 0");
    }
}
