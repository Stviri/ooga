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

    // The host for running scripts on this computer: with real windows when this is the Windows ooga.exe.
    public static ConsoleHost ForThisComputer(TextReader input, TextWriter output, bool realWait = true)
    {
#if OOGA_WINDOWS
        return new WindowsConsoleHost(input, output, realWait);
#else
        return new ConsoleHost(input, output, realWait);
#endif
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

    public bool FolderExists(string path) => Directory.Exists(Full(path));

    public IReadOnlyList<string> FilesIn(string folder) =>
        Directory.GetFiles(Full(folder)).Select(f => Path.Combine(folder, Path.GetFileName(f))).Order(StringComparer.OrdinalIgnoreCase).ToList();

    public IReadOnlyList<string> FoldersIn(string folder) =>
        Directory.GetDirectories(Full(folder)).Select(f => Path.Combine(folder, Path.GetFileName(f))).Order(StringComparer.OrdinalIgnoreCase).ToList();

    public void MakeFolder(string path) => Directory.CreateDirectory(Full(path));

    public void MoveFile(string from, string to) => File.Move(Full(from), Full(to));

    public void CopyFile(string from, string to) => File.Copy(Full(from), Full(to));

    public void DeleteFile(string path)
    {
        if (!File.Exists(Full(path))) throw new FileNotFoundException("missing", path);
        File.Delete(Full(path));
    }

    public long FileSize(string path) => new FileInfo(Full(path)).Length;

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
