using System.Text;
using Ooga;

namespace Ooga.Cli;

// "ooga --test examples": runs every script in a folder and checks what it shows against what it should show.
//
// For a script   examples/10_lists.ooga
// it looks for   examples/expected/10_lists.out    what the script must show (required)
//                examples/expected/10_lists.in     what the person types, one answer per line (only if the script asks)
//                examples/expected/10_lists.args   words after the file name, on one line (only if the script uses them)
//
// Every check runs the same way, so the answer is the same on every computer:
//   - "random" uses seed 1 (same numbers every time)
//   - "wait" does not really wait
//   - files are read and written in a fresh empty folder, which is thrown away after
//   - a script that runs too long (10 seconds, or 10 million steps) fails
// A .out file showing "ooga booga! problem in ..." means the script must stop with that problem (exit code 1).
public static class TestRunner
{
    public const int Seed = 1;
    public static readonly TimeSpan TimeLimit = TimeSpan.FromSeconds(10);
    const long MaxSteps = 10_000_000;

    public record CaseResult(string Name, bool Passed, string Problem, string Expected, string Actual);

    public static int Main(string[] args, TextWriter output)
    {
        bool make = args.Contains("--make-expected");
        var folders = args.Where(a => a != "--make-expected").ToList();
        if (folders.Count == 0) folders.Add("examples");

        int passed = 0, failed = 0, made = 0;
        foreach (var folder in folders)
        {
            if (!Directory.Exists(folder))
            {
                output.WriteLine($"ooga no find folder: {folder}");
                failed++;
                continue;
            }
            foreach (var script in Scripts(folder))
            {
                string name = Path.GetFileName(script);
                if (make && !File.Exists(ExpectedFile(script, ".out")))
                {
                    var made1 = RunCase(script);
                    File.WriteAllText(ExpectedFile(script, ".out"), made1.Actual);
                    output.WriteLine($"made   {Path.Combine(folder, name)}  (look at expected/{Path.GetFileNameWithoutExtension(name)}.out: is it right?)");
                    made++;
                    continue;
                }
                var result = RunCase(script);
                if (result.Passed)
                {
                    output.WriteLine($"ok     {Path.Combine(folder, name)}");
                    passed++;
                }
                else
                {
                    output.WriteLine($"WRONG  {Path.Combine(folder, name)}: {result.Problem}");
                    failed++;
                }
            }
        }

        output.WriteLine();
        output.WriteLine($"{passed} ok, {failed} wrong" + (made > 0 ? $", {made} made" : "") + ".");
        if (failed == 0 && passed + made > 0) output.WriteLine("ooga happy.");
        return failed == 0 ? CliApp.Ok : CliApp.ScriptProblem;
    }

    // The .ooga files directly inside a folder (not in sub-folders: those are helpers for "use").
    public static IEnumerable<string> Scripts(string folder) =>
        Directory.GetFiles(folder, "*.ooga").OrderBy(f => f, StringComparer.Ordinal);

    public static string ExpectedFile(string script, string ending) =>
        Path.Combine(Path.GetDirectoryName(script) ?? ".", "expected", Path.GetFileNameWithoutExtension(script) + ending);

    public static CaseResult RunCase(string script)
    {
        string name = Path.GetFileName(script);
        string outFile = ExpectedFile(script, ".out");
        string inFile = ExpectedFile(script, ".in");
        string argsFile = ExpectedFile(script, ".args");

        string typed = File.Exists(inFile) ? File.ReadAllText(inFile) : "";
        string[] words = File.Exists(argsFile)
            ? File.ReadAllText(argsFile).Split((char[])null, StringSplitOptions.RemoveEmptyEntries)
            : Array.Empty<string>();

        string workDir = Path.Combine(Path.GetTempPath(), "ooga-check-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(workDir);
        var shown = new StringWriter { NewLine = "\n" };
        int exit;
        try
        {
            // What the person sees: normal output and problems, in the order they happen.
            var both = TextWriter.Synchronized(shown);
            var host = new ConsoleHost(new StringReader(typed), both, realWait: false,
                workDir: workDir, nameRoot: Path.GetDirectoryName(Path.GetFullPath(script)));
            using var cancel = new CancellationTokenSource(TimeLimit);
            var options = new RunOptions
            {
                FileName = script, Arguments = words, Seed = Seed, MaxSteps = MaxSteps, Cancel = cancel.Token,
            };
            var task = Task.Run(() => CliApp.RunScript(script, name, host, options, both, both, color: false));
            if (Task.WaitAny(new Task[] { task }, TimeLimit + TimeSpan.FromSeconds(5)) < 0)
                return new CaseResult(name, false, "took too long (never ended?)", null, shown.ToString());
            try
            {
                exit = task.GetAwaiter().GetResult();
            }
            catch (OperationCanceledException)
            {
                return new CaseResult(name, false, $"took more than {TimeLimit.TotalSeconds} seconds", null, shown.ToString());
            }
        }
        finally
        {
            try { Directory.Delete(workDir, recursive: true); } catch (IOException) { }
        }

        string actual = shown.ToString();
        if (!File.Exists(outFile))
            return new CaseResult(name, false, $"no expected output. make {Path.Combine("expected", Path.GetFileName(outFile))}", null, actual);

        string expected = File.ReadAllText(outFile);
        if (exit == CliApp.EngineProblem)
            return new CaseResult(name, false, "ooga engine broke (engine bug)", expected, actual);

        string want = Clean(expected), got = Clean(actual);
        if (want != got)
            return new CaseResult(name, false, FirstDifference(want, got), expected, actual);

        int wantExit = want.Contains("ooga booga! problem in ") ? CliApp.ScriptProblem : CliApp.Ok;
        if (exit != wantExit)
            return new CaseResult(name, false, $"ended with code {exit}, but should end with {wantExit}", expected, actual);

        return new CaseResult(name, true, null, expected, actual);
    }

    // Windows and Linux end lines differently, and editors add or drop a last empty line. Neither counts.
    static string Clean(string text) => text.Replace("\r\n", "\n").TrimEnd('\n', ' ');

    static string FirstDifference(string want, string got)
    {
        var w = want.Split('\n');
        var g = got.Split('\n');
        for (int i = 0; i < Math.Max(w.Length, g.Length); i++)
        {
            string wl = i < w.Length ? w[i] : "(nothing more)";
            string gl = i < g.Length ? g[i] : "(nothing more)";
            if (wl != gl)
                return new StringBuilder()
                    .Append($"line {i + 1} of output is different.")
                    .Append($"\n         want: {wl}")
                    .Append($"\n         got:  {gl}")
                    .ToString();
        }
        return "output is different.";
    }
}
