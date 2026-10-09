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
        if (args.Length != 1 || args[0] is "-h" or "--help" or "/?")
        {
            output.WriteLine("ooga run ooga files.");
            output.WriteLine("use like this:  ooga hello.ooga");
            return args.Length == 1 ? Ok : UsageProblem;
        }

        string path = args[0];
        if (!File.Exists(path))
        {
            error.WriteLine($"ooga no find file: {path}");
            foreach (var f in LookAlikeFiles(path))
                error.WriteLine($"you mean:  {f}");
            return UsageProblem;
        }

        string source = File.ReadAllText(path, Encoding.UTF8);
        try
        {
            OogaRunner.Run(source, new ConsoleHost(input, output, realWait));
            output.Flush();
            return Ok;
        }
        catch (OogaError e)
        {
            output.Flush();
            ShowError(error, e.Report(path, source), color);
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
