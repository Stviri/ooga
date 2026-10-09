using System.Text;
using Ooga;

namespace Ooga.Cli;

// The "ooga" command. Kept apart from Program.cs so tests can run it without a real console.
public static class CliApp
{
    public const int Ok = 0;
    public const int ScriptProblem = 1;
    public const int UsageProblem = 2;
    public const int EngineProblem = 3;

    // color: paint the error header red and the pointer yellow (only for a real console).
    public static int Run(string[] args, TextReader input, TextWriter output, TextWriter error,
        bool color = false, bool realWait = true)
    {
        if (args.Length == 0 || args[0] is "-h" or "--help" or "/?")
        {
            output.WriteLine("ooga run ooga files.");
            output.WriteLine("use like this:  ooga hello.ooga");
            output.WriteLine("words after the file name go to the script: me arguments");
            output.WriteLine("same random numbers every time:  ooga --seed 7 hello.ooga");
            output.WriteLine("talk to ooga line by line:  ooga --talk");
            output.WriteLine("check scripts against their expected output:  ooga --test examples");
            return args.Length == 0 ? UsageProblem : Ok;
        }

        if (args[0] is "--talk" or "-i")
            return Talk(input, output, error, color, realWait);

        if (args[0] == "--test")
            return TestRunner.Main(args.Skip(1).ToArray(), output);

        int? seed = null;
        if (args[0] == "--seed")
        {
            if (args.Length < 3 || !int.TryParse(args[1], out int s))
            {
                error.WriteLine("--seed need a whole number and then a file, like:  ooga --seed 7 hello.ooga");
                return UsageProblem;
            }
            seed = s;
            args = args.Skip(2).ToArray();
        }

        string path = args[0];
        if (!File.Exists(path))
        {
            error.WriteLine($"ooga no find file: {path}");
            foreach (var f in LookAlikeFiles(path))
                error.WriteLine($"you mean:  {f}");
            return UsageProblem;
        }

        var host = new ConsoleHost(input, output, realWait);
        return RunScript(path, path, host, new RunOptions { FileName = path, Arguments = args.Skip(1).ToArray(), Seed = seed },
            output, error, color);
    }

    // Runs one script file and prints any problem. Also used by the test runner.
    internal static int RunScript(string path, string shownName, ConsoleHost host, RunOptions options,
        TextWriter output, TextWriter error, bool color)
    {
        string source = File.ReadAllText(path, Encoding.UTF8);
        try
        {
            OogaRunner.Run(source, host, options);
            output.Flush();
            return Ok;
        }
        catch (OogaError e)
        {
            output.Flush();
            if (e.File == path) e.File = shownName;
            ShowError(error, e.Report(), color);
            return ScriptProblem;
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            output.Flush();
            error.WriteLine();
            error.WriteLine("ooga engine broke. this is engine bug, not your fault.");
            error.WriteLine(e.ToString());
            return EngineProblem;
        }
    }

    // Words that start a block: the piece goes on until an empty line.
    static readonly string[] BlockStarters = { "if", "repeat", "count", "each", "try" };

    // Talk mode: type ooga, see what happens, things are kept between pieces.
    static int Talk(TextReader input, TextWriter output, TextWriter error, bool color, bool realWait)
    {
        output.WriteLine("ooga talk. type ooga lines and press Enter.");
        output.WriteLine("lines that start a block (if, repeat, count, each, try, me can) end with an empty line.");
        output.WriteLine("type bye to leave.");
        var host = new ConsoleHost(input, output, realWait);
        var talk = new OogaSession(host, new RunOptions { FileName = "talk" });

        while (true)
        {
            output.Write("ooga> ");
            output.Flush();
            string line = input.ReadLine();
            if (line == null || line.Trim() == "bye") break;
            if (line.Trim() == "") continue;

            var piece = new List<string> { line };
            string first = line.TrimStart().Split(' ')[0];
            if (BlockStarters.Contains(first) || line.TrimStart().StartsWith("me can "))
            {
                while (true)
                {
                    output.Write("....> ");
                    output.Flush();
                    string more = input.ReadLine();
                    if (more == null || more.Trim() == "") break;
                    piece.Add(more);
                }
            }

            try
            {
                if (talk.Run(string.Join("\n", piece)) == RunEnd.Died)
                {
                    output.WriteLine("(me die. talk over.)");
                    break;
                }
            }
            catch (OogaError e)
            {
                output.Flush();
                ShowError(error, e.Report(), color);
                error.Flush();
            }
            catch (Exception e) when (e is not OperationCanceledException)
            {
                error.WriteLine("ooga engine broke. this is engine bug, not your fault.");
                error.WriteLine(e.ToString());
            }
        }
        output.WriteLine("bye!");
        return Ok;
    }

    static void ShowError(TextWriter error, ErrorReport report, bool color)
    {
        error.WriteLine();
        Paint(error, report.Header, color ? ConsoleColor.Red : null);
        if (report.Code != null)
        {
            error.WriteLine(report.Code);
            Paint(error, report.Pointer, color ? ConsoleColor.Yellow : null);
        }
        error.WriteLine(report.Message);
    }

    static void Paint(TextWriter w, string text, ConsoleColor? c)
    {
        if (c != null) Console.ForegroundColor = c.Value;
        w.WriteLine(text);
        if (c != null) Console.ResetColor();
    }

    static IEnumerable<string> LookAlikeFiles(string path)
    {
        try
        {
            string name = Path.GetFileName(path);
            if (!name.EndsWith(".ooga", StringComparison.OrdinalIgnoreCase)) name += ".ooga";
            return Directory.EnumerateFiles(".", name, SearchOption.AllDirectories)
                .Select(f => Path.GetRelativePath(".", f))
                .Take(3)
                .ToList();
        }
        catch (Exception)
        {
            return Array.Empty<string>();
        }
    }
}
