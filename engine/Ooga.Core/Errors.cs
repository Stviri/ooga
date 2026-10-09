namespace Ooga;

// A problem in the ooga script. Line and column point at the spot to show the user.
public class OogaError : Exception
{
    public int Line { get; }
    public int Col { get; }

    // Which script file the problem is in (null = the main file), and that file's text (for showing the line).
    public string File { get; set; }
    public string SourceText { get; set; }

    // "try" / "oops" can catch this problem. Safety stops (like "ooga tired") can not be caught.
    public bool CanCatch { get; init; } = true;

    public OogaError(int line, int col, string message) : base(message)
    {
        Line = line;
        Col = col;
    }

    public OogaError(Node at, string message) : this(at.Line, at.Col, message) { File = at.File; }

    // Report using the file name and text remembered in the error.
    public ErrorReport Report() => Report(File ?? "script", SourceText ?? "");

    // The error as the user sees it:
    //
    //   ooga booga! problem in hello.ooga line 7, column 5:
    //       say heath
    //           ^
    //   no thing called "heath". you mean "health"?
    public ErrorReport Report(string fileName, string source)
    {
        string[] lines = source.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
        string code = null, pointer = null;
        if (Line >= 1 && Line <= lines.Length)
        {
            code = lines[Line - 1];
            pointer = new string(' ', Math.Max(0, Math.Min(Col - 1, code.Length))) + "^";
        }
        return new ErrorReport(
            $"ooga booga! problem in {fileName} line {Line}, column {Col}:",
            code == null ? null : "    " + code,
            pointer == null ? null : "    " + pointer,
            Message);
    }
}

// The four parts of an error message, kept apart so a console can colour them.
// Code and Pointer are null when the line can not be shown.
public record ErrorReport(string Header, string Code, string Pointer, string Message)
{
    public override string ToString() =>
        string.Join("\n", new[] { Header, Code, Pointer, Message }.Where(s => s != null));
}

// Thrown by "me die" to end the program quietly.
public class DieSignal : Exception { }

// Finds the closest known word to a typo, for "you mean ...?" hints.
public static class Spelling
{
    public static string Suggest(string word, IEnumerable<string> candidates, int maxDistance = 2)
    {
        string best = null;
        int bestDist = maxDistance + 1;
        foreach (var c in candidates)
        {
            if (c == word) continue;
            int d = Distance(word.ToLowerInvariant(), c.ToLowerInvariant());
            // Very short words only count as typos when they differ by case alone, or by one letter.
            if (Math.Min(word.Length, c.Length) <= 3 && d > 1) continue;
            if (d < bestDist) { bestDist = d; best = c; }
        }
        return best;
    }

    // How many single-letter changes turn a into b. Swapping two letters next to each other counts as one.
    public static int Distance(string a, string b)
    {
        var d = new int[a.Length + 1, b.Length + 1];
        for (int i = 0; i <= a.Length; i++) d[i, 0] = i;
        for (int j = 0; j <= b.Length; j++) d[0, j] = j;
        for (int i = 1; i <= a.Length; i++)
            for (int j = 1; j <= b.Length; j++)
            {
                d[i, j] = Math.Min(Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                    d[i - 1, j - 1] + (a[i - 1] == b[j - 1] ? 0 : 1));
                if (i > 1 && j > 1 && a[i - 1] == b[j - 2] && a[i - 2] == b[j - 1])
                    d[i, j] = Math.Min(d[i, j], d[i - 2, j - 2] + 1);
            }
        return d[a.Length, b.Length];
    }
}
