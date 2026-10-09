using Ooga;

namespace Ooga.Cli;

// Connects ooga's "say", "ask" and "wait" to a text console, and gives it the computer's files.
public class ConsoleHost : IOogaHost, IOogaFiles
{
    readonly TextReader input;
    readonly TextWriter output;
    readonly bool realWait;
    readonly string workDir;
    readonly string nameRoot;
    readonly Dictionary<string, string> realPaths = new();   // shown name of a used file -> where it really is

    // workDir: folder that file paths start from (null = the folder ooga was started in).
    // nameRoot: folder that names of used files are shown from, with / between folders (null = normal names).
    public ConsoleHost(TextReader input, TextWriter output, bool realWait = true, string workDir = null, string nameRoot = null)
    {
        this.input = input;
        this.output = output;
        this.realWait = realWait;
        this.workDir = workDir;
        this.nameRoot = nameRoot;
    }

    string Full(string path) => workDir == null ? path : Path.Combine(workDir, path);

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

    public string ReadFile(string path) => File.ReadAllText(Full(path));

    public void WriteFile(string path, string text, bool add)
    {
        if (add) File.AppendAllText(Full(path), text);
        else File.WriteAllText(Full(path), text);
    }

    public bool FileExists(string path) => File.Exists(Full(path));

    // 'use "tools.ooga"' looks next to the script that says it. ".ooga" may be left off.
    public LoadedScript LoadScript(string path, string fromFile)
    {
        // fromFile is the name shown in messages; look up where that file really is.
        string from = fromFile != null && realPaths.TryGetValue(fromFile, out var real) ? real : fromFile ?? "";
        string folder = Path.GetDirectoryName(from) ?? "";
        string full = Path.GetFullPath(Path.Combine(folder, path));
        if (!File.Exists(full) && !Path.HasExtension(full) && File.Exists(full + ".ooga")) full += ".ooga";
        if (!File.Exists(full)) return null;
        string shown;
        if (nameRoot != null)
        {
            shown = Path.GetRelativePath(nameRoot, full).Replace('\\', '/');
        }
        else
        {
            shown = Path.GetRelativePath(Directory.GetCurrentDirectory(), full);
            if (shown.StartsWith("..")) shown = full;
        }
        realPaths[shown] = full;
        return new LoadedScript(full, shown, File.ReadAllText(full));
    }
}
