using System.Globalization;
using System.Text.RegularExpressions;

namespace Ooga;

// An action ooga already knows, used like any other action: "me round 3.7".
// The standard ones live in Library. A program that runs ooga (or a future game engine adapter)
// can add its own through RunOptions.Actions.
public class OogaAction
{
    public string Name { get; }
    public int MinThings { get; }
    public int MaxThings { get; }          // -1 = any number
    public Func<ActionCall, object> Run { get; }

    public OogaAction(string name, int things, Func<ActionCall, object> run) : this(name, things, things, run) { }

    public OogaAction(string name, int minThings, int maxThings, Func<ActionCall, object> run)
    {
        Name = name;
        MinThings = minThings;
        MaxThings = maxThings;
        Run = run;
    }

    public string ThingsText()
    {
        static string N(int n) => n == 1 ? "1 thing" : $"{n} things";
        if (MaxThings == MinThings) return N(MinThings);
        if (MaxThings < 0) return $"{N(MinThings)} or more";
        return $"{MinThings} to {MaxThings} things";
    }
}

// What an action gets when it runs: the things handed to it, plus helpers to check them.
public class ActionCall
{
    public string Name { get; init; }
    public object[] Things { get; init; }
    public Node At { get; init; }
    public IOogaHost Host { get; init; }
    public Random Random { get; init; }
    public RunOptions Options { get; init; }
    public DateTime Started { get; init; }

    public object this[int i] => Things[i];
    public int Count => Things.Length;

    public OogaError Problem(string message) => new(At, message);

    string Which(int i) => Things.Length == 1 ? "" : $" (thing {i + 1})";

    public double Number(int i) =>
        Things[i] is double d ? d : throw Problem($"{Name} need a number{Which(i)}, but got {Values.Describe(Things[i])}.");

    public int Whole(int i)
    {
        double d = Number(i);
        if (d != Math.Floor(d) || d < int.MinValue || d > int.MaxValue)
            throw Problem($"{Name} need a whole number{Which(i)}, but got {Values.FormatNumber(d)}.");
        return (int)d;
    }

    public string Text(int i) =>
        Things[i] is string s ? s : throw Problem($"{Name} need a text{Which(i)}, but got {Values.Describe(Things[i])}.");

    public OogaList List(int i) =>
        Things[i] is OogaList l ? l : throw Problem($"{Name} need a list{Which(i)}, but got {Values.Describe(Things[i])}.");

    public IOogaFiles Files() =>
        Host as IOogaFiles ?? throw Problem($"{Name} need files, but ooga is running somewhere with no files.");
}

public static class Library
{
    public static readonly IReadOnlyDictionary<string, OogaAction> Standard = Make().ToDictionary(a => a.Name);

    static readonly Regex PlainNumber = new(@"^-?[0-9]+(\.[0-9]+)?$");

    static IEnumerable<OogaAction> Make()
    {
        // ---------- numbers ----------
        yield return new("round", 1, c => Round(c, Math.Round(c.Number(0), MidpointRounding.AwayFromZero)));
        yield return new("round_to", 2, c =>
        {
            int places = c.Whole(1);
            if (places < 0 || places > 15) throw c.Problem("round_to can keep 0 to 15 places after the dot.");
            return Math.Round(c.Number(0), places, MidpointRounding.AwayFromZero);
        });
        yield return new("round_down", 1, c => Math.Floor(c.Number(0)));
        yield return new("round_up", 1, c => Math.Ceiling(c.Number(0)));
        yield return new("positive", 1, c => Math.Abs(c.Number(0)));
        yield return new("power", 2, c => Finite(c, Math.Pow(c.Number(0), c.Number(1))));
        yield return new("root", 1, c =>
        {
            double x = c.Number(0);
            if (x < 0) throw c.Problem("no can take root of number below 0.");
            return Math.Sqrt(x);
        });
        yield return new("biggest", 1, 2, c => BigSmall(c, big: true));
        yield return new("smallest", 1, 2, c => BigSmall(c, big: false));
        yield return new("numbers", 2, c =>
        {
            int from = c.Whole(0), to = c.Whole(1);
            if (Math.Abs((long)to - from) > 10_000_000) throw c.Problem("numbers can make at most 10000000 numbers.");
            int step = from <= to ? 1 : -1;
            var list = new OogaList();
            for (long n = from; step > 0 ? n <= to : n >= to; n += step) list.Items.Add((double)n);
            return list;
        });

        // ---------- texts ----------
        yield return new("upper", 1, c => c.Text(0).ToUpperInvariant());
        yield return new("lower", 1, c => c.Text(0).ToLowerInvariant());
        yield return new("trim", 1, c => c.Text(0).Trim());
        yield return new("split", 2, c =>
        {
            string text = c.Text(0), sep = c.Text(1);
            IEnumerable<string> parts = sep == "" ? text.Select(ch => ch.ToString()) : text.Split(sep);
            return new OogaList(parts);
        });
        yield return new("join", 2, c => string.Join(c.Text(1), c.List(0).Items.Select(Values.Show)));
        yield return new("replace", 3, c =>
        {
            string from = c.Text(1);
            if (from == "") throw c.Problem("replace need something to look for, not empty text.");
            return c.Text(0).Replace(from, c.Text(2));
        });
        yield return new("starts_with", 2, c => c.Text(0).StartsWith(c.Text(1), StringComparison.Ordinal));
        yield return new("ends_with", 2, c => c.Text(0).EndsWith(c.Text(1), StringComparison.Ordinal));
        yield return new("number", 1, c => c[0] switch
        {
            double d => d,
            string s when PlainNumber.IsMatch(s.Trim()) => double.Parse(s.Trim(), CultureInfo.InvariantCulture),
            _ => null,
        });
        yield return new("text", 1, c => Values.Show(c[0]));

        // ---------- texts and lists ----------
        yield return new("piece", 3, c =>
        {
            int from = c.Whole(1), to = c.Whole(2);
            int size = c[0] switch
            {
                string s => s.Length,
                OogaList l => l.Items.Count,
                _ => throw c.Problem($"piece need a text or a list, but got {Values.Describe(c[0])}."),
            };
            if (from < 1 || to > size || from > to + 1)
                throw c.Problem($"piece {from} to {to} no fit. it has {CountText(size)} (1 to {size}).");
            int len = to - from + 1;
            return c[0] is string text ? text.Substring(from - 1, len) : new OogaList(((OogaList)c[0]).Items.GetRange(from - 1, len));
        });
        yield return new("find", 2, c => c[0] switch
        {
            string s => (double)(s.IndexOf(c.Text(1), StringComparison.Ordinal) + 1),
            OogaList l => (double)(l.Items.FindIndex(x => Values.Same(x, c[1])) + 1),
            _ => throw c.Problem($"find need a text or a list, but got {Values.Describe(c[0])}."),
        });
        yield return new("reverse", 1, c => c[0] switch
        {
            string s => new string(s.Reverse().ToArray()),
            OogaList l => new OogaList(Enumerable.Reverse(l.Items)),
            _ => throw c.Problem($"reverse need a text or a list, but got {Values.Describe(c[0])}."),
        });

        // ---------- lists and boxes ----------
        yield return new("sort", 1, c =>
        {
            var items = c.List(0).Items;
            if (items.All(x => x is double)) return new OogaList(items.OrderBy(x => (double)x));
            if (items.All(x => x is string)) return new OogaList(items.OrderBy(x => (string)x, StringComparer.Ordinal));
            throw c.Problem("sort need a list of only numbers or only texts.");
        });
        yield return new("take", 2, c =>
        {
            if (c[0] is OogaBox box)
            {
                if (!box.TryGet(c[1], out var value))
                    throw c.Problem($"box no have part {Values.Show(c[1])}.");
                box.Remove(c[1]);
                return value;
            }
            var list = c.List(0);
            int at = Position(c, list, c.Whole(1));
            var item = list.Items[at];
            list.Items.RemoveAt(at);
            return item;
        });
        yield return new("put_at", 3, c =>
        {
            var list = c.List(0);
            int at = c.Whole(1);
            if (at < 1 || at > list.Items.Count + 1)
                throw c.Problem($"put_at {at} no fit. list has {CountText(list.Items.Count)}, so use 1 to {list.Items.Count + 1}.");
            list.Items.Insert(at - 1, c[2]);
            return null;
        });
        yield return new("keys", 1, c => c[0] is OogaBox box
            ? new OogaList(box.Keys)
            : throw c.Problem($"keys need a box, but got {Values.Describe(c[0])}."));
        yield return new("copy", 1, c => Values.Copy(c[0]));
        yield return new("pick", 1, c =>
        {
            var list = c.List(0);
            if (list.Items.Count == 0) throw c.Problem("no can pick from empty list.");
            return list.Items[c.Random.Next(list.Items.Count)];
        });
        yield return new("shuffle", 1, c =>
        {
            var items = new List<object>(c.List(0).Items);
            for (int i = items.Count - 1; i > 0; i--)
            {
                int j = c.Random.Next(i + 1);
                (items[i], items[j]) = (items[j], items[i]);
            }
            return new OogaList(items);
        });

        // ---------- time and outside world ----------
        yield return new("time", 0, c => (DateTime.UtcNow - c.Started).TotalSeconds);
        yield return new("date", 0, c => DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));
        yield return new("arguments", 0, c => new OogaList(c.Options.Arguments ?? Array.Empty<string>()));
        yield return new("read_file", 1, c => Guard(c, () => c.Files().ReadFile(c.Text(0))));
        yield return new("write_file", 2, c => Guard(c, () => { c.Files().WriteFile(c.Text(0), Values.Show(c[1]), add: false); return null; }));
        yield return new("add_to_file", 2, c => Guard(c, () => { c.Files().WriteFile(c.Text(0), Values.Show(c[1]), add: true); return null; }));
        yield return new("file_exists", 1, c => Guard(c, () => c.Files().FileExists(c.Text(0))));

        // ---------- the C# bridge ----------
        foreach (var a in CSharpBridge.Actions()) yield return a;
    }

    static object Round(ActionCall c, double d) => d == 0 ? 0.0 : d;

    static object Finite(ActionCall c, double d) =>
        double.IsFinite(d) ? d : throw c.Problem("number too big for ooga (or no answer).");

    static object BigSmall(ActionCall c, bool big)
    {
        IEnumerable<object> items = c.Count == 2 ? c.Things : c.List(0).Items;
        var list = items.ToList();
        if (list.Count == 0) throw c.Problem($"{c.Name} of empty list? no can.");
        if (list.Any(x => x is not double))
            throw c.Problem($"{c.Name} need numbers.");
        return big ? list.Max(x => (double)x) : list.Min(x => (double)x);
    }

    // Turns a 1-based ooga position into a list index, with a caveman error when it is outside.
    public static int Position(ActionCall c, OogaList list, int pos)
    {
        if (pos < 1 || pos > list.Items.Count)
            throw c.Problem(list.Items.Count == 0
                ? $"list is empty. no thing at {pos}."
                : $"list has {CountText(list.Items.Count)} (1 to {list.Items.Count}). no thing at {pos}.");
        return pos - 1;
    }

    public static string CountText(int n) => n == 1 ? "1 thing" : $"{n} things";

    // File problems (missing file, no permission) become ooga problems that "try" can catch.
    static object Guard(ActionCall c, Func<object> work)
    {
        try
        {
            return work();
        }
        catch (Exception e) when (e is FileNotFoundException or DirectoryNotFoundException)
        {
            throw c.Problem($"{c.Name} no find file: {Values.Show(c[0])}");
        }
        catch (UnauthorizedAccessException)
        {
            throw c.Problem($"{c.Name} not allowed to touch file: {Values.Show(c[0])}");
        }
        catch (Exception e) when (e is IOException or ArgumentException or NotSupportedException)
        {
            throw c.Problem($"{c.Name} no work: {e.Message}");
        }
    }
}
