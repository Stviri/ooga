using Ooga;

namespace Ooga.Cli;

// Connects ooga's "say", "ask" and "wait" to a text console, and gives it the computer's files.
public class ConsoleHost : IOogaHost, IOogaFiles
{
    readonly TextReader input;
    readonly TextWriter output;
    readonly bool realWait;

    public ConsoleHost(TextReader input, TextWriter output, bool realWait = true)
    {
        this.input = input;
        this.output = output;
        this.realWait = realWait;
    }

    public void Say(string text) => output.WriteLine(text);

    public string Ask(string prompt)
    {
        if (prompt != null) output.Write(prompt + " ");
        output.Flush();
        return input.ReadLine();
    }

    public void Wait(double seconds)
    {
        output.Flush();
        if (realWait) Thread.Sleep(TimeSpan.FromSeconds(seconds));
    }

    // ---------- files (paths are from the folder the ooga command was started in) ----------

    public string ReadFile(string path) => File.ReadAllText(path);

    public void WriteFile(string path, string text, bool add)
    {
        if (add) File.AppendAllText(path, text);
        else File.WriteAllText(path, text);
    }

    public bool FileExists(string path) => File.Exists(path);

    // 'use "tools.ooga"' looks next to the script that says it. ".ooga" may be left off.
    public LoadedScript LoadScript(string path, string fromFile)
    {
        string folder = Path.GetDirectoryName(fromFile ?? "") ?? "";
        string full = Path.GetFullPath(Path.Combine(folder, path));
        if (!File.Exists(full) && !Path.HasExtension(full) && File.Exists(full + ".ooga")) full += ".ooga";
        if (!File.Exists(full)) return null;
        string shown = Path.GetRelativePath(Directory.GetCurrentDirectory(), full);
        if (shown.StartsWith("..")) shown = full;
        return new LoadedScript(full, shown, File.ReadAllText(full));
    }
}
