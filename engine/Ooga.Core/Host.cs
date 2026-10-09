namespace Ooga;

// Everything ooga needs from the outside world.
//
// The language core never touches the console, files, or a game engine directly.
// The ooga command line gives it a console host. Tests give it a pretend host.
// A Godot (or other engine) adapter will later give it a host of its own.
public interface IOogaHost
{
    // "say": show one line.
    void Say(string text);

    // "ask": show the prompt (null when there is none) and return what the person typed.
    // Return null when no more answers can ever come (for example, the input was closed).
    string Ask(string prompt);

    // "wait": pause for this many seconds (never less than 0).
    void Wait(double seconds);
}

// A host that also has files. Needed for "use" and for read_file / write_file / add_to_file / file_exists.
// Hosts without files (a game, a web page) simply do not implement this.
public interface IOogaFiles
{
    string ReadFile(string path);
    void WriteFile(string path, string text, bool add);
    bool FileExists(string path);

    // Finds the script named in 'use "path"', written inside the script fromFile.
    // Key must be the same for the same file (so it is only used once). Name is shown in error messages.
    LoadedScript LoadScript(string path, string fromFile);
}

public record LoadedScript(string Key, string Name, string Text);

public class RunOptions
{
    // Same seed = same "random" numbers every run. Null = different every run.
    public int? Seed { get; init; }

    // Stop with an error after this many steps (lines run plus loop rounds). 0 = no limit.
    // Tests use it so a never-ending loop fails instead of hanging.
    public long MaxSteps { get; init; }

    // How deep actions may call other actions (or themselves) before ooga gives up.
    public int MaxDepth { get; init; } = 500;

    // Lets the caller stop a running program from outside.
    public CancellationToken Cancel { get; init; }

    // The name shown in errors for the main script, and used to find files named in "use".
    public string FileName { get; init; } = "script";

    // What "me arguments" gives: extra words typed after the file name.
    public IReadOnlyList<string> Arguments { get; init; }

    // Extra actions the program running ooga provides (for example a game engine adapter).
    public IEnumerable<OogaAction> Actions { get; init; }

    // Allow the csharp, csharp_new, csharp_call, csharp_get and csharp_set actions.
    public bool AllowCSharp { get; init; } = true;
}

public enum RunEnd { Finished, Died }

// The simple way to use ooga: give it script text and a host, it does the rest.
public static class OogaRunner
{
    // Read and check a script without running it. Throws OogaError on mistakes.
    public static OogaProgram Compile(string source, IOogaHost host = null, RunOptions options = null)
    {
        options ??= new RunOptions();
        var sources = new Dictionary<string, string> { [options.FileName] = source };
        try
        {
            return CompileInto(source, host, options, sources);
        }
        catch (OogaError e)
        {
            Attach(e, options, sources);
            throw;
        }
    }

    // Read, check and run a script. Throws OogaError on mistakes.
    // Runs on its own thread with a big stack, so deep ooga actions can not crash the engine.
    public static RunEnd Run(string source, IOogaHost host, RunOptions options = null)
    {
        options ??= new RunOptions();
        var sources = new Dictionary<string, string> { [options.FileName] = source };
        RunEnd end = RunEnd.Finished;
        Exception failure = null;

        var thread = new Thread(() =>
        {
            try
            {
                var program = CompileInto(source, host, options, sources);
                end = Interpreter.Run(program, host, options);
            }
            catch (Exception e)
            {
                failure = e;
            }
        }, 256 * 1024 * 1024);
        thread.Start();
        thread.Join();

        if (failure is OogaError oe) Attach(oe, options, sources);
        if (failure != null)
            System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();
        return end;
    }

    // All actions ooga knows before reading the script: the standard ones plus the host's.
    public static Dictionary<string, OogaAction> KnownActions(RunOptions options)
    {
        var all = new Dictionary<string, OogaAction>();
        foreach (var a in Library.Standard.Values)
            if (options.AllowCSharp || !a.Name.StartsWith("csharp", StringComparison.Ordinal))
                all[a.Name] = a;
        foreach (var a in options.Actions ?? Enumerable.Empty<OogaAction>())
            all[a.Name] = a;
        return all;
    }

    static OogaProgram CompileInto(string source, IOogaHost host, RunOptions options, Dictionary<string, string> sources)
    {
        var program = Parser.Parse(Lexer.Run(source, options.FileName));
        var used = new HashSet<string> { "main:" + options.FileName };
        program.Body = Link(program.Body, host as IOogaFiles, used, sources, 0);
        Checker.Check(program, KnownActions(options));
        return program;
    }

    // Replaces each 'use "file"' line with the lines of that file (each file only once).
    internal static List<Stmt> Link(List<Stmt> body, IOogaFiles files, HashSet<string> used, Dictionary<string, string> sources, int depth)
    {
        var result = new List<Stmt>();
        foreach (var s in body)
        {
            if (s is not UseStmt u)
            {
                result.Add(s);
                continue;
            }
            if (files == null)
                throw new OogaError(u, "use need files, but ooga is running somewhere with no files.");
            if (depth > 50)
                throw new OogaError(u, "too many files using other files. ooga dizzy.");

            LoadedScript loaded;
            try
            {
                loaded = files.LoadScript(u.Path, u.File);
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
            {
                throw new OogaError(u, $"ooga no can use \"{u.Path}\": {e.Message}");
            }
            if (loaded == null)
                throw new OogaError(u, $"ooga no find file to use: \"{u.Path}\"");
            if (!used.Add(loaded.Key)) continue;

            sources[loaded.Name] = loaded.Text;
            var module = Parser.Parse(Lexer.Run(loaded.Text, loaded.Name));
            result.AddRange(Link(module.Body, files, used, sources, depth + 1));
        }
        return result;
    }

    static void Attach(OogaError e, RunOptions options, Dictionary<string, string> sources)
    {
        e.File ??= options.FileName;
        if (e.SourceText == null && sources.TryGetValue(e.File, out var text)) e.SourceText = text;
    }
}
