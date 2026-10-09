namespace Ooga;

// Reads tokens and builds the program shape (see Ast.cs).
public class Parser
{
    public static readonly HashSet<string> Keywords = new()
    {
        "me", "is", "has", "can", "die", "say", "if", "else", "repeat", "while", "count", "from", "to",
        "stop", "skip", "give", "wait", "when", "and", "or", "not", "same", "big", "small",
        "yes", "no", "nothing", "ask", "random", "gain", "lose",
        "list", "box", "of", "item", "size", "kind", "each", "in", "try", "oops", "fail", "use",
    };

    // Words that can start a line. Used for "you mean ...?" hints.
    static readonly string[] LineStarters = { "say", "if", "else", "repeat", "count", "each", "stop", "skip", "give", "wait", "try", "fail", "use", "item", "me" };

    readonly List<Token> t;
    int p;

    Parser(List<Token> tokens) { t = tokens; }

    public static OogaProgram Parse(List<Token> tokens)
    {
        var parser = new Parser(tokens);
        var program = new OogaProgram { Line = 1, Col = 1, File = tokens.Count > 0 ? tokens[0].File : null };
        try
        {
            while (parser.Peek().Kind != Tok.End)
                program.Body.Add(parser.ParseStatement(top: true));
        }
        catch (OogaError e)
        {
            e.File ??= program.File;
            throw;
        }
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
        node.File = tk.File;
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
        if (IsWord(tk, "me"))
            throw Err(tk, "ooga confused by me here. to give an action's answer to other action, wrap it in ( ), like: me double (me double 2)");
        if (tk.Kind == Tok.Symbol && "+-*/%".Contains(tk.Value))
            throw Err(tk, $"ooga confused by {tk} here. if this math is for an action, put ( ) around it, like: me hit (5 + 1)");
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

            case "each":
            {
                Next();
                string name = ExpectName("a name for each thing, like: each x in bag");
                ExpectWord("in", $"each need \"in\" next, like: each {name} in bag");
                if (AtLineEnd()) throw Err(Peek(), $"each {name} in what? like: each {name} in bag");
                var source = ParseExpr();
                return At(new EachStmt { Name = name, Source = source, Body = ParseBlock(tk, "each") }, tk);
            }

            case "try":
            {
                Next();
                var body = ParseBlock(tk, "try");
                if (!IsWord(Peek(), "oops"))
                    throw Err(tk, "try need oops right after its lines, to say what to do when something go wrong. like:\ntry\n    ...\noops why\n    say why");
                var oopsTok = Next();
                string name = Peek().Kind == Tok.Word ? ExpectName("a name for the problem, like: oops why") : null;
                return At(new TryStmt { Body = body, OopsName = name, Oops = ParseBlock(oopsTok, "oops") }, tk);
            }

            case "oops":
                throw Err(tk, "oops here, but no try right above it.");

            case "fail":
                Next();
                if (AtLineEnd()) throw Err(Peek(), "fail need a reason, like: fail \"health no can be below 0\"");
                stmt = At(new FailStmt { Message = ParseExpr() }, tk);
                break;

            case "use":
                Next();
                if (!top) throw Err(tk, "use must be at left edge, not inside other block.");
                if (Peek().Kind != Tok.Text) throw Err(Peek(), "use need a file name in quotes, like: use \"tools.ooga\"");
                stmt = At(new UseStmt { Path = Next().Value }, tk);
                break;

            case "item":
                stmt = ParseChange(tk);
                break;

            case "when":
                throw Err(tk, "\"when\" is for game things (like getting hit). it come later with Godot. for now, use \"me can\".");

            default:
            {
                if (Keywords.Contains(tk.Value))
                    throw Err(tk, $"\"{tk.Value}\" no can start a line.");
                stmt = ParseChange(tk);
                break;
            }
        }

        ExpectLineEnd();
        return stmt;
    }

    // health is 5 / health gain 1 / health of player lose 2 / item 2 of bag is "x"
    Stmt ParseChange(Token tk)
    {
        var target = ParsePrimary();
        string name = target switch
        {
            NameExpr n => n.Name,
            PartExpr pe => pe.Part + " of ...",
            _ => "item ... of ...",
        };
        var verb = Peek();
        if (IsWord(verb, "is"))
        {
            Next();
            var after = Peek();
            if (IsWord(after, "same") || IsWord(after, "big") || IsWord(after, "small") || IsWord(after, "not"))
                throw Err(after, $"this look like a check. checks go after if, like: if {name} is {after.Value} 5");
            if (AtLineEnd()) throw Err(after, $"{name} is what? like: {name} is 5");
            return At(new SetStmt { Target = target, Value = ParseExpr() }, tk);
        }
        if (IsWord(verb, "gain") || IsWord(verb, "lose"))
        {
            Next();
            if (AtLineEnd()) throw Err(Peek(), $"{name} {verb.Value} how much? like: {name} {verb.Value} 1");
            return At(new ChangeStmt { Target = target, Gain = verb.Value == "gain", Amount = ParseExpr() }, tk);
        }
        if (IsWord(verb, "has"))
            throw Err(verb, $"this look like a check. checks go after if, like: if {name} has 5");
        if (target is NameExpr)
        {
            string hint = Spelling.Suggest(tk.Value, LineStarters);
            if (hint != null)
                throw Err(tk, $"ooga no know \"{tk.Value}\". you mean {hint}?");
        }
        if (target is NameExpr)
            throw Err(tk, $"what to do with {name}? try \"{name} is 5\", \"{name} gain 1\", or \"say {name}\".");
        throw Err(verb.Kind is Tok.Newline or Tok.End or Tok.Dedent ? tk : verb,
            $"what to do with {name}? try \"is\", \"gain\" or \"lose\" after it.");
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
        if (IsWord(Peek(), "has"))
        {
            var hasTok = Next();
            if (AtLineEnd()) throw Err(Peek(), "has what? like: if bag has 5");
            return At(new HasExpr { Holder = left, Item = ParseSum() }, hasTok);
        }
        if (!IsWord(Peek(), "is")) return left;

        var isTok = Next();
        bool not = false;
        if (IsWord(Peek(), "not")) { Next(); not = true; }

        // "x is bigg 5": a misspelled check word followed by a value.
        var w = Peek();
        if (w.Kind == Tok.Word && !Keywords.Contains(w.Value) && StartsArg(Peek(1)))
        {
            string hint = Spelling.Suggest(w.Value, new[] { "big", "small", "same" });
            if (hint != null)
                throw Err(w, $"ooga no know \"{w.Value}\". you mean {hint}?");
        }

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
        while (IsSym(Peek(), "*") || IsSym(Peek(), "/") || IsSym(Peek(), "%"))
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
        || (tk.Kind == Tok.Word && (!Keywords.Contains(tk.Value) || tk.Value is "yes" or "no" or "nothing" or "size" or "kind" or "item"));

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
                    case "list":
                    {
                        Next();
                        var list = At(new ListExpr(), tk);
                        while (StartsArg(Peek()))
                            list.Items.Add(ParsePrimary());
                        return list;
                    }
                    case "box":
                    {
                        Next();
                        var box = At(new BoxExpr(), tk);
                        while (Peek().Kind == Tok.Word && !Keywords.Contains(Peek().Value))
                        {
                            var partTok = Next();
                            if (box.Parts.Any(p => p.Name == partTok.Value))
                                throw Err(partTok, $"box already has part {partTok.Value}. each part only once.");
                            if (!StartsArg(Peek()))
                                throw Err(Peek(), $"box part {partTok.Value} need a value, like: box {partTok.Value} 0");
                            box.Parts.Add((partTok.Value, ParsePrimary()));
                        }
                        return box;
                    }
                    case "size":
                    case "kind":
                    {
                        Next();
                        ExpectWord("of", $"{tk.Value} need \"of\" next, like: {tk.Value} of bag");
                        var inner = ParsePrimary();
                        return tk.Value == "size" ? At(new SizeExpr { Inner = inner }, tk) : At(new KindExpr { Inner = inner }, tk);
                    }
                    case "item":
                    {
                        Next();
                        if (!StartsArg(Peek())) throw Err(Peek(), "item which? like: item 1 of bag");
                        // "item k of bag": a plain name right after item is the position, never "k of bag".
                        // (to use a part as the position, wrap it: item (pos of p) of bag)
                        Expr index;
                        if (Peek().Kind == Tok.Word && !Keywords.Contains(Peek().Value) && IsWord(Peek(1), "of"))
                        {
                            var nameTok = Next();
                            index = At(new NameExpr { Name = nameTok.Value }, nameTok);
                        }
                        else
                        {
                            index = ParsePrimary();
                        }
                        ExpectWord("of", "item need \"of\" next, like: item 1 of bag");
                        return At(new ItemExpr { Index = index, Holder = ParsePrimary() }, tk);
                    }
                }
                if (Keywords.Contains(tk.Value))
                    throw Err(tk, $"ooga expect a value here, but got \"{tk.Value}\".");
                Next();
                if (IsWord(Peek(), "of"))
                {
                    Next();
                    return At(new PartExpr { Part = tk.Value, Holder = ParsePrimary() }, tk);
                }
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
