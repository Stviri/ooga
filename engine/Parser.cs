namespace Ooga;

// Reads tokens and builds the program shape (see Ast.cs).
public class Parser
{
    public static readonly HashSet<string> Keywords = new()
    {
        "me", "is", "has", "can", "die", "say", "if", "else", "repeat", "while", "count", "from", "to",
        "stop", "skip", "give", "wait", "when", "and", "or", "not", "same", "big", "small",
        "yes", "no", "nothing", "ask", "random", "gain", "lose",
    };

    readonly List<Token> t;
    int p;

    Parser(List<Token> tokens) { t = tokens; }

    public static OogaProgram Parse(List<Token> tokens)
    {
        var parser = new Parser(tokens);
        var program = new OogaProgram { Line = 1, Col = 1 };
        while (parser.Peek().Kind != Tok.End)
            program.Body.Add(parser.ParseStatement(top: true));
        return program;
    }

    // ---------- token helpers ----------

    Token Peek(int ahead = 0) => t[Math.Min(p + ahead, t.Count - 1)];
    Token Next() => t[p++];
    static bool IsWord(Token tk, string w) => tk.Kind == Tok.Word && tk.Value == w;
    static bool IsSym(Token tk, string s) => tk.Kind == Tok.Symbol && tk.Value == s;
    bool AtLineEnd() => Peek().Kind is Tok.Newline or Tok.End or Tok.Dedent;

    static T At<T>(T node, Token tk) where T : Node
    {
        node.Line = tk.Line;
        node.Col = tk.Col;
        return node;
    }

    static OogaError Err(Token tk, string message) => new(tk.Line, tk.Col, message);

    void ExpectWord(string w, string message)
    {
        if (!IsWord(Peek(), w)) throw Err(Peek(), message);
        Next();
    }

    string ExpectName(string what)
    {
        var tk = Peek();
        if (tk.Kind != Tok.Word)
            throw Err(tk, $"ooga want {what} here, but got {tk}.");
        if (Keywords.Contains(tk.Value))
            throw Err(tk, $"\"{tk.Value}\" is special ooga word. no can use it as a name. pick other name.");
        Next();
        return tk.Value;
    }

    void ExpectLineEnd()
    {
        var tk = Peek();
        if (tk.Kind == Tok.Newline) { Next(); return; }
        if (tk.Kind is Tok.End or Tok.Dedent) return;
        throw Err(tk, $"ooga confused by {tk} here. too many things on this line?");
    }

    List<Stmt> ParseBlock(Token owner, string ownerText)
    {
        ExpectLineEnd();
        if (Peek().Kind != Tok.Indent)
            throw Err(owner, $"\"{ownerText}\" need lines under it, pushed in 4 spaces.");
        Next();
        var body = new List<Stmt>();
        while (Peek().Kind is not (Tok.Dedent or Tok.End))
            body.Add(ParseStatement(top: false));
        if (Peek().Kind == Tok.Dedent) Next();
        return body;
    }

    // ---------- lines ----------

    Stmt ParseStatement(bool top)
    {
        var tk = Peek();
        if (tk.Kind == Tok.Indent)
            throw Err(tk, "this line pushed in, but nothing above want lines under it. move it left.");
        if (tk.Kind != Tok.Word)
            throw Err(tk, $"line must start with a word (like say, if, me), not {tk}.");

        Stmt stmt;
        switch (tk.Value)
        {
            case "me":
                return ParseMe(top);

            case "say":
                Next();
                if (AtLineEnd()) throw Err(Peek(), "say what? put something after say, like: say \"hello\"");
                stmt = At(new SayStmt { Value = ParseExpr() }, tk);
                break;

            case "if":
                return ParseIf();

            case "else":
                throw Err(tk, "else here, but no if right above it.");

            case "repeat":
            {
                Next();
                if (IsWord(Peek(), "while"))
                {
                    Next();
                    var cond = ParseExpr();
                    return At(new RepeatWhileStmt { Cond = cond, Body = ParseBlock(tk, "repeat while") }, tk);
                }
                if (AtLineEnd()) throw Err(Peek(), "repeat how many times? like: repeat 3");
                var times = ParseExpr();
                return At(new RepeatStmt { Times = times, Body = ParseBlock(tk, "repeat") }, tk);
            }

            case "count":
            {
                Next();
                string name = ExpectName("a name to count with, like: count i from 1 to 10");
                ExpectWord("from", $"count need \"from\" next, like: count {name} from 1 to 10");
                var from = ParseExpr();
                ExpectWord("to", $"count need \"to\" next, like: count {name} from 1 to 10");
                var to = ParseExpr();
                return At(new CountStmt { Name = name, From = from, To = to, Body = ParseBlock(tk, "count") }, tk);
            }

            case "stop":
                Next();
                stmt = At(new StopStmt(), tk);
                break;

            case "skip":
                Next();
                stmt = At(new SkipStmt(), tk);
                break;

            case "give":
                Next();
                stmt = At(new GiveStmt { Value = AtLineEnd() ? null : ParseExpr() }, tk);
                break;

            case "wait":
                Next();
                if (AtLineEnd()) throw Err(Peek(), "wait how long? like: wait 1  (that is 1 second)");
                stmt = At(new WaitStmt { Seconds = ParseExpr() }, tk);
                break;

            case "when":
                throw Err(tk, "\"when\" is for game things (like getting hit). it come later with Godot. for now, use \"me can\".");

            default:
            {
                if (Keywords.Contains(tk.Value))
                    throw Err(tk, $"\"{tk.Value}\" no can start a line.");
                Next();
                string name = tk.Value;
                var verb = Peek();
                if (IsWord(verb, "is"))
                {
                    Next();
                    var after = Peek();
                    if (IsWord(after, "same") || IsWord(after, "big") || IsWord(after, "small") || IsWord(after, "not"))
                        throw Err(after, $"this look like a check. checks go after if, like: if {name} is {after.Value} 5");
                    if (AtLineEnd()) throw Err(after, $"{name} is what? like: {name} is 5");
                    stmt = At(new SetStmt { Name = name, Value = ParseExpr() }, tk);
                }
                else if (IsWord(verb, "gain") || IsWord(verb, "lose"))
                {
                    Next();
                    if (AtLineEnd()) throw Err(Peek(), $"{name} {verb.Value} how much? like: {name} {verb.Value} 1");
                    stmt = At(new ChangeStmt { Name = name, Gain = verb.Value == "gain", Amount = ParseExpr() }, tk);
                }
                else
                {
                    throw Err(tk, $"what to do with {name}? try \"{name} is 5\", \"{name} gain 1\", or \"say {name}\".");
                }
                break;
            }
        }

        ExpectLineEnd();
        return stmt;
    }

    Stmt ParseMe(bool top)
    {
        var me = Next();
        var w = Peek();
        if (w.Kind != Tok.Word)
            throw Err(w, "me what? try: me has, me can, me die, or me <action>.");

        Stmt stmt;
        switch (w.Value)
        {
            case "is":
                Next();
                stmt = At(new KindStmt { Kind = ExpectName("what kind of thing me is, like: me is player") }, me);
                break;

            case "has":
            {
                Next();
                string name = ExpectName("a name for the thing, like: me has health 100");
                if (AtLineEnd())
                    throw Err(Peek(), $"me has {name}... but how much? give start value, like: me has {name} 0");
                stmt = At(new HasStmt { Name = name, Value = ParseExpr() }, me);
                break;
            }

            case "can":
            {
                if (!top)
                    throw Err(me, "\"me can\" must be at left edge, not inside other block.");
                Next();
                string name = ExpectName("an action name, like: me can jump");
                var can = At(new CanStmt { Name = name }, me);
                while (Peek().Kind == Tok.Word)
                    can.Params.Add(ExpectName("a name for the thing action get"));
                can.Body = ParseBlock(me, $"me can {name}");
                return can;
            }

            case "die":
                Next();
                stmt = At(new DieStmt(), me);
                break;

            default:
                stmt = At(new CallStmt { Call = ParseCallAfterMe(me) }, me);
                break;
        }

        ExpectLineEnd();
        return stmt;
    }

    Stmt ParseIf()
    {
        var ifTok = Next();
        var stmt = At(new IfStmt(), ifTok);
        var cond = ParseExpr();
        stmt.Branches.Add((cond, ParseBlock(ifTok, "if")));

        while (IsWord(Peek(), "else"))
        {
            var elseTok = Next();
            if (IsWord(Peek(), "if"))
            {
                Next();
                var c = ParseExpr();
                stmt.Branches.Add((c, ParseBlock(elseTok, "else if")));
            }
            else
            {
                stmt.Else = ParseBlock(elseTok, "else");
                break;
            }
        }
        return stmt;
    }

    // ---------- values ----------

    Expr ParseExpr() => ParseOr();

    Expr ParseOr()
    {
        var left = ParseAnd();
        while (IsWord(Peek(), "or"))
        {
            var op = Next();
            left = At(new LogicExpr { Op = "or", Left = left, Right = ParseAnd() }, op);
        }
        return left;
    }

    Expr ParseAnd()
    {
        var left = ParseNot();
        while (IsWord(Peek(), "and"))
        {
            var op = Next();
            left = At(new LogicExpr { Op = "and", Left = left, Right = ParseNot() }, op);
        }
        return left;
    }

    Expr ParseNot()
    {
        if (IsWord(Peek(), "not"))
        {
            var tk = Next();
            return At(new NotExpr { Inner = ParseNot() }, tk);
        }
        return ParseCompare();
    }

    Expr ParseCompare()
    {
        var left = ParseSum();
        if (!IsWord(Peek(), "is")) return left;

        var isTok = Next();
        bool not = false;
        if (IsWord(Peek(), "not")) { Next(); not = true; }

        var op = CompareOp.Same;
        if (IsWord(Peek(), "same"))
        {
            Next();
        }
        else if (IsWord(Peek(), "big") || IsWord(Peek(), "small"))
        {
            bool big = Next().Value == "big";
            if (IsWord(Peek(), "or") && IsWord(Peek(1), "same"))
            {
                Next();
                Next();
                op = big ? CompareOp.BigOrSame : CompareOp.SmallOrSame;
            }
            else
            {
                op = big ? CompareOp.Big : CompareOp.Small;
            }
        }

        return At(new CompareExpr { Op = op, Not = not, Left = left, Right = ParseSum() }, isTok);
    }

    Expr ParseSum()
    {
        var left = ParseTerm();
        while (IsSym(Peek(), "+") || IsSym(Peek(), "-"))
        {
            var op = Next();
            left = At(new MathExpr { Op = op.Value, Left = left, Right = ParseTerm() }, op);
        }
        return left;
    }

    Expr ParseTerm()
    {
        var left = ParseUnary();
        while (IsSym(Peek(), "*") || IsSym(Peek(), "/"))
        {
            var op = Next();
            left = At(new MathExpr { Op = op.Value, Left = left, Right = ParseUnary() }, op);
        }
        return left;
    }

    Expr ParseUnary()
    {
        if (IsSym(Peek(), "-"))
        {
            var tk = Next();
            return At(new NegateExpr { Inner = ParseUnary() }, tk);
        }
        return ParsePrimary();
    }

    bool StartsArg(Token tk) =>
        tk.Kind is Tok.Number or Tok.Text
        || IsSym(tk, "(")
        || (tk.Kind == Tok.Word && (!Keywords.Contains(tk.Value) || tk.Value is "yes" or "no" or "nothing"));

    CallExpr ParseCallAfterMe(Token me)
    {
        string name = ExpectName("an action name after me");
        var call = At(new CallExpr { Name = name }, me);
        while (StartsArg(Peek()))
            call.Args.Add(ParsePrimary());
        return call;
    }

    Expr ParsePrimary()
    {
        var tk = Peek();
        switch (tk.Kind)
        {
            case Tok.Number:
                Next();
                return At(new LiteralExpr { Value = tk.Number }, tk);

            case Tok.Text:
                Next();
                return At(new LiteralExpr { Value = tk.Value }, tk);

            case Tok.Symbol when tk.Value == "(":
            {
                Next();
                var inner = ParseExpr();
                if (!IsSym(Peek(), ")"))
                    throw Err(tk, "this ( open but never close. put a ) at the end.");
                Next();
                return inner;
            }

            case Tok.Word:
                switch (tk.Value)
                {
                    case "yes": Next(); return At(new LiteralExpr { Value = true }, tk);
                    case "no": Next(); return At(new LiteralExpr { Value = false }, tk);
                    case "nothing": Next(); return At(new LiteralExpr { Value = null }, tk);
                    case "ask":
                        Next();
                        return At(new AskExpr { Prompt = StartsArg(Peek()) ? ParsePrimary() : null }, tk);
                    case "random":
                    {
                        Next();
                        var low = ParseSum();
                        ExpectWord("to", "random need \"to\", like: random 1 to 6");
                        return At(new RandomExpr { Low = low, High = ParseSum() }, tk);
                    }
                    case "me":
                        Next();
                        return ParseCallAfterMe(tk);
                }
                if (Keywords.Contains(tk.Value))
                    throw Err(tk, $"ooga expect a value here, but got \"{tk.Value}\".");
                Next();
                return At(new NameExpr { Name = tk.Value }, tk);

            case Tok.Newline:
            case Tok.End:
            case Tok.Dedent:
                throw Err(tk, "line end too early. ooga expect a value here.");

            default:
                throw Err(tk, $"ooga expect a value here, but got {tk}.");
        }
    }
}
