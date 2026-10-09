using System.Text;
using Ooga;

Console.OutputEncoding = Encoding.UTF8;
Console.InputEncoding = Encoding.UTF8;

if (args.Length != 1)
{
    Console.WriteLine("ooga run ooga files.");
    Console.WriteLine("use like this:  ooga hello.ooga");
    return 1;
}

string path = args[0];
if (!File.Exists(path))
{
    Console.WriteLine($"ooga no find file: {path}");
    try
    {
        string name = Path.GetFileName(path);
        if (!name.EndsWith(".ooga", StringComparison.OrdinalIgnoreCase)) name += ".ooga";
        var found = Directory.EnumerateFiles(".", name, SearchOption.AllDirectories)
            .Select(f => Path.GetRelativePath(".", f))
            .Take(3)
            .ToList();
        foreach (var f in found)
            Console.WriteLine($"you mean:  .\\ooga {f}");
    }
    catch (Exception)
    {
    }
    return 1;
}

string source = File.ReadAllText(path, Encoding.UTF8);

try
{
    var program = Parser.Parse(Lexer.Run(source));
    Checker.Check(program);
    Interpreter.Run(program);
}
catch (DieSignal)
{
}
catch (OogaError e)
{
    Console.Out.Flush();
    ShowError(Path.GetFileName(path), source, e);
    return 1;
}

return 0;

static void ShowError(string file, string source, OogaError e)
{
    string[] lines = source.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');

    Console.WriteLine();
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($"ooga booga! problem in {file} line {e.Line}:");
    Console.ResetColor();

    if (e.Line >= 1 && e.Line <= lines.Length)
    {
        string code = lines[e.Line - 1];
        Console.WriteLine("    " + code);
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("    " + new string(' ', Math.Max(0, Math.Min(e.Col - 1, code.Length))) + "^");
        Console.ResetColor();
    }

    Console.WriteLine(e.Message);
}
