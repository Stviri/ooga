namespace Ooga;

// Talk mode: run ooga a piece at a time, keeping things and actions between pieces.
//
//   var talk = new OogaSession(host, options);
//   talk.Run("me has x 5");
//   talk.Run("say x + 1");     // says 6
public class OogaSession
{
    readonly IOogaHost host;
    readonly RunOptions options;
    readonly Interpreter interpreter;
    readonly Dictionary<string, OogaAction> builtIns;
    int pieces;
    readonly HashSet<string> used = new();
    readonly Dictionary<string, string> sources = new();

    // The answer of the last piece, when it was only one action use (like "me double 5"). Talk mode says it.
    public object LastAnswer { get; private set; }

    public OogaSession(IOogaHost host, RunOptions options = null)
    {
        this.host = host;
        this.options = options ?? new RunOptions { FileName = "talk" };
        interpreter = new Interpreter(host, this.options);
        builtIns = OogaRunner.KnownActions(this.options);
    }

    // Runs one piece. Throws OogaError on mistakes; the things made before the mistake stay.
    // A piece that is just a value (like "1 + 2" or "x") is said, so you can look at things quickly.
    public RunEnd Run(string piece)
    {
        pieces++;
        string name = $"{options.FileName} piece {pieces}";
        OogaProgram program;
        try
        {
            program = Compile(piece, name);
        }
        catch (OogaError first) when (!piece.Contains('\n') && !piece.TrimStart().StartsWith("say "))
        {
            try
            {
                program = Compile("say " + piece.Trim(), name);
            }
            catch (OogaError)
            {
                throw first;
            }
        }

        RunEnd end = RunEnd.Finished;
        Exception failure = null;
        LastAnswer = null;
        var thread = new Thread(() =>
        {
            try { end = interpreter.RunMore(program); }
            catch (Exception e) { failure = e; }
        }, 256 * 1024 * 1024);
        thread.Start();
        thread.Join();
        if (failure is OogaError oe)
        {
            oe.File ??= name;
            oe.SourceText ??= oe.File == name ? piece : sources.GetValueOrDefault(oe.File);
        }
        if (failure != null)
            System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();
        LastAnswer = interpreter.LastAnswer;
        if (LastAnswer != null) host.Say(Values.Show(LastAnswer));
        return end;
    }

    OogaProgram Compile(string piece, string name)
    {
        try
        {
            var program = Parser.Parse(Lexer.Run(piece, name));
            // "use" works the same as in a file. Each file is used once per talk.
            program.Body = OogaRunner.Link(program.Body, host as IOogaFiles, used, sources, 0);
            Checker.CheckMore(program, builtIns, interpreter.ThingNames, interpreter.Actions);
            return program;
        }
        catch (OogaError e)
        {
            e.File ??= name;
            e.SourceText ??= e.File == name ? piece : sources.GetValueOrDefault(e.File);
            throw;
        }
    }
}
