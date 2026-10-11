using Ooga;

namespace Ooga.Tests;

// A pretend outside world: answers come from a list, "say" lines are collected, "wait" does not sleep,
// and files live in memory.
public partial class FakeHost : IOogaHost, IOogaFiles, IOogaWindows
{
    public readonly Dictionary<string, string> Files = new();

    readonly Queue<string> answers;
    public readonly List<string> Said = new();
    public readonly List<string> Prompts = new();
    public readonly List<double> Waits = new();

    public FakeHost(params string[] answers) { this.answers = new Queue<string>(answers); }

    public void Say(string text) => Said.Add(text);

    public string Ask(string prompt)
    {
        Prompts.Add(prompt);
        return answers.Count > 0 ? answers.Dequeue() : null;
    }

    public void Wait(double seconds) => Waits.Add(seconds);

    public string Output => string.Join("\n", Said);

    public string ReadFile(string path) =>
        Files.TryGetValue(Clean(path), out var text) ? text : throw new FileNotFoundException("missing", path);

    public void WriteFile(string path, string text, bool add)
    {
        Files[Clean(path)] = add && Files.TryGetValue(Clean(path), out var old) ? old + text : text;
        MakeFolder(Parent(path));
    }

    public bool FileExists(string path) => Files.ContainsKey(Clean(path));

    // Folders in memory: a folder exists when it was made, or when a file is inside it.
    public readonly HashSet<string> Folders = new() { "." };
    static string Clean(string p) => p.Replace('\\', '/').TrimEnd('/');
    static string Parent(string p) { int i = Clean(p).LastIndexOf('/'); return i < 0 ? "." : Clean(p)[..i]; }
    static string Join(string folder, string name) => folder == "." ? name : Clean(folder) + "/" + name;
    static string NameOf(string p) => Clean(p)[(Clean(p).LastIndexOf('/') + 1)..];

    public bool FolderExists(string path) =>
        Folders.Contains(Clean(path)) || Files.Keys.Any(f => Parent(f) == Clean(path) || Parent(f).StartsWith(Clean(path) + "/"));

    public IReadOnlyList<string> FilesIn(string folder)
    {
        if (!FolderExists(folder)) throw new DirectoryNotFoundException(folder);
        return Files.Keys.Where(f => Parent(f) == Clean(folder)).Select(f => Join(folder, NameOf(f))).Order(StringComparer.OrdinalIgnoreCase).ToList();
    }

    public IReadOnlyList<string> FoldersIn(string folder)
    {
        if (!FolderExists(folder)) throw new DirectoryNotFoundException(folder);
        return Folders.Where(f => f != "." && Parent(f) == Clean(folder)).Select(f => Join(folder, NameOf(f))).Order(StringComparer.OrdinalIgnoreCase).ToList();
    }

    public void MakeFolder(string path) { var p = Clean(path); while (p != ".") { Folders.Add(p); p = Parent(p); } }

    // like a real disk, a folder stays when its last file moves out
    public void MoveFile(string from, string to) { CopyFile(from, to); MakeFolder(Parent(from)); Files.Remove(Clean(from)); }

    public void CopyFile(string from, string to)
    {
        if (!Files.TryGetValue(Clean(from), out var text)) throw new FileNotFoundException("missing", from);
        if (Files.ContainsKey(Clean(to))) throw new IOException($"The file '{to}' already exists.");
        if (!FolderExists(Parent(to))) throw new DirectoryNotFoundException(Parent(to));
        Files[Clean(to)] = text;
        MakeFolder(Parent(to));
    }

    public void DeleteFile(string path) { if (!Files.Remove(Clean(path))) throw new FileNotFoundException("missing", path); }

    public long FileSize(string path) =>
        Files.TryGetValue(Clean(path), out var text) ? System.Text.Encoding.UTF8.GetByteCount(text) : throw new FileNotFoundException("missing", path);

    public LoadedScript LoadScript(string path, string fromFile)
    {
        string name = Files.ContainsKey(path) ? path : path + ".ooga";
        return Files.TryGetValue(name, out var text) ? new LoadedScript(name, name, text) : null;
    }
}

// ---------- pretend windows ----------

public class FakeControl : IWindowControl
{
    public string Kind;
    public FakeControl Window;
    public Action OnClick;
    public readonly List<FakeControl> Children = new();
    public bool Closed;
    public string Text { get; set; } = "";
    public bool Checked { get; set; }
    public IReadOnlyList<string> Items { get; set; } = Array.Empty<string>();
    public int PickedIndex { get; set; } = -1;
    public int Width { get; set; } = 420;
    public int Height { get; set; } = 320;

    // what a person would do in the window
    public FakeControl Find(string kind, int nth = 0) => Children.Where(c => c.Kind == kind).ElementAt(nth);
    public void Click(string buttonText) => Children.Single(c => c.Kind == "button" && c.Text == buttonText).OnClick();
    public void Type(string text, int nth = 0) => Find("text_box", nth).Text = text;
}

public partial class FakeHost
{
    public readonly List<FakeControl> Windows = new();
    public readonly List<string> Popups = new();
    public readonly Queue<string> PickAnswers = new();

    // What the "person" does each time a window is opened, in order. No script = they close it at once.
    public readonly Queue<Action<FakeControl>> WhenShown = new();

    public IWindowControl NewWindow(string title)
    {
        var w = new FakeControl { Kind = "window", Text = title };
        w.Window = w;
        Windows.Add(w);
        return w;
    }

    public IWindowControl Add(IWindowControl window, string kind, string text, IReadOnlyList<string> items, Action onClick)
    {
        var w = (FakeControl)window;
        var c = new FakeControl { Kind = kind, Text = text, Items = items ?? Array.Empty<string>(), OnClick = onClick, Window = w };
        w.Children.Add(c);
        return c;
    }

    public void Show(IWindowControl window)
    {
        var w = (FakeControl)window;
        if (WhenShown.Count > 0) WhenShown.Dequeue()(w);
        w.Closed = true;
    }

    public void Close(IWindowControl window) => ((FakeControl)window).Closed = true;
    public void Popup(string text) => Popups.Add(text);
    public string PickFile() => PickAnswers.Count > 0 ? PickAnswers.Dequeue() : null;
    public string PickFolder() => PickAnswers.Count > 0 ? PickAnswers.Dequeue() : null;
}

// A host with no files at all, like a game engine might be.
public class NoFilesHost : IOogaHost
{
    public readonly List<string> Said = new();
    public void Say(string text) => Said.Add(text);
    public string Ask(string prompt) => null;
    public void Wait(double seconds) { }
}

public record RunResult(string Output, RunEnd End, FakeHost Host);

public static class O
{
    // No single test program may run longer than this (wall clock) or take more steps than MaxSteps.
    public static readonly TimeSpan TimeLimit = TimeSpan.FromSeconds(10);
    public const long MaxSteps = 2_000_000;

    public static RunResult Run(string source, params string[] answers) => Run(source, new FakeHost(answers));

    public static RunResult Run(string source, FakeHost host, RunOptions extra = null)
    {
        var end = Bounded(cancel => OogaRunner.Run(source, host, new RunOptions
        {
            Seed = 1, MaxSteps = MaxSteps, Cancel = cancel, FileName = extra?.FileName ?? "main.ooga",
            Arguments = extra?.Arguments, Actions = extra?.Actions, AllowCSharp = extra?.AllowCSharp ?? true,
        }));
        return new RunResult(host.Output, end, host);
    }

    // Shortcut: run and return what was said.
    public static string Out(string source, params string[] answers) => Run(source, answers).Output;

    // Run a program that must fail, and return the ooga error.
    public static OogaError Fails(string source, params string[] answers) => Fails(source, new FakeHost(answers));

    public static OogaError Fails(string source, FakeHost host, RunOptions extra = null)
    {
        try
        {
            Run(source, host, extra);
        }
        catch (OogaError e)
        {
            return e;
        }
        throw new Xunit.Sdk.XunitException("expected an ooga error, but the program ran fine. output:\n" + host.Output);
    }

    // Runs work with a hard time limit. A hanging program fails the test instead of hanging it.
    public static T Bounded<T>(Func<CancellationToken, T> work)
    {
        using var cts = new CancellationTokenSource();
        var task = Task.Run(() => work(cts.Token));
        if (Task.WaitAny(new Task[] { task }, TimeLimit) < 0)
        {
            cts.Cancel();
            throw new Xunit.Sdk.XunitException($"ooga program did not finish within {TimeLimit.TotalSeconds} seconds.");
        }
        return task.GetAwaiter().GetResult();
    }

    public static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "Ooga.sln")))
            dir = dir.Parent;
        return dir?.FullName ?? throw new InvalidOperationException("could not find the repository root (Ooga.sln)");
    }

    public static string Lines(params string[] lines) => string.Join("\n", lines);
}
