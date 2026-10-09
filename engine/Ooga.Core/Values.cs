using System.Globalization;
using System.Text;

namespace Ooga;

// Ooga values in C#:
//   number      -> double
//   text        -> string
//   yes / no    -> bool
//   nothing     -> null
//   list        -> OogaList
//   box         -> OogaBox
//   C# thing    -> any other .NET object (from the C# bridge)
//
// Lists and boxes are shared, not copied: "me has b bag" gives the same list a second name.

public class OogaList
{
    public readonly List<object> Items;
    public OogaList() { Items = new List<object>(); }
    public OogaList(IEnumerable<object> items) { Items = new List<object>(items); }
    public override string ToString() => Values.Show(this);
}

// A box keeps named parts in the order they were added.
public class OogaBox
{
    readonly Dictionary<object, object> parts = new(new Values.SameComparer());
    readonly List<object> order = new();

    public int Count => order.Count;
    public IReadOnlyList<object> Keys => order;

    public bool Has(object key) => parts.ContainsKey(key);
    public bool TryGet(object key, out object value) => parts.TryGetValue(key, out value);

    public void Set(object key, object value)
    {
        if (!parts.ContainsKey(key)) order.Add(key);
        parts[key] = value;
    }

    public bool Remove(object key)
    {
        if (!parts.Remove(key)) return false;
        order.RemoveAll(k => Values.Same(k, key));
        return true;
    }

    public override string ToString() => Values.Show(this);
}

// An action held as a value: "action double". Run it with "me call f 5".
public class ActionValue
{
    public readonly string Name;
    public ActionValue(string name) { Name = name; }
    public override bool Equals(object o) => o is ActionValue a && a.Name == Name;
    public override int GetHashCode() => Name.GetHashCode();
    public override string ToString() => "action " + Name;
}

public static class Values
{
    // ---------- showing ----------

    // How "say" shows a value.
    public static string Show(object v) => Show(v, 0, quoteText: false);

    static string Show(object v, int depth, bool quoteText)
    {
        if (depth > 20) return "...";
        switch (v)
        {
            case null: return "nothing";
            case bool b: return b ? "yes" : "no";
            case double d: return FormatNumber(d);
            case string s: return quoteText ? Quote(s) : s;
            case ActionValue a: return a.ToString();
            case OogaList list:
            {
                var sb = new StringBuilder("list");
                foreach (var item in list.Items)
                    sb.Append(' ').Append(Inner(item, depth));
                return sb.ToString();
            }
            case OogaBox box:
            {
                var sb = new StringBuilder("box");
                foreach (var key in box.Keys)
                {
                    box.TryGet(key, out var value);
                    sb.Append(' ').Append(key is string k && IsPlainName(k) ? k : Inner(key, depth));
                    sb.Append(' ').Append(Inner(value, depth));
                }
                return sb.ToString();
            }
            default:
                return CSharpBridge.ShowThing(v);
        }
    }

    // A value inside a list or box: text in quotes, lists and boxes in ( ), so it reads like ooga code.
    static string Inner(object v, int depth)
    {
        string shown = Show(v, depth + 1, quoteText: true);
        bool wrap = (v is OogaList l && l.Items.Count > 0) || (v is OogaBox b && b.Count > 0)
                    || (v is double d && d < 0) || v is ActionValue;
        return wrap ? "(" + shown + ")" : shown;
    }

    public static string Quote(string s) =>
        "\"" + s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n") + "\"";

    static bool IsPlainName(string s) =>
        s.Length > 0 && (char.IsLetter(s[0]) || s[0] == '_') && s.All(c => char.IsLetterOrDigit(c) || c == '_')
        && !Parser.Keywords.Contains(s);

    public static string FormatNumber(double d)
    {
        if (d == 0) return "0";
        return d.ToString("G15", CultureInfo.InvariantCulture);
    }

    // How error messages name a value: "number 5", "text \"hi\"", "list of 3 things".
    public static string Describe(object v) => v switch
    {
        null => "nothing",
        bool b => b ? "yes" : "no",
        double d => "number " + FormatNumber(d),
        string s => "text " + Quote(s.Length > 40 ? s.Substring(0, 40) + "..." : s),
        OogaList l => l.Items.Count == 1 ? "list of 1 thing" : $"list of {l.Items.Count} things",
        ActionValue a => a.ToString(),
        OogaBox b => b.Count == 0 ? "empty box" : "box with " + string.Join(", ", b.Keys.Take(5).Select(k => Show(k))),
        _ => "C# thing " + v.GetType().FullName,
    };

    // What "kind of" answers.
    public static string KindName(object v) => v switch
    {
        null => "nothing",
        bool => "yes or no",
        double => "number",
        string => "text",
        OogaList => "list",
        OogaBox => "box",
        ActionValue => "action",
        _ => "csharp",
    };

    // ---------- sameness ----------

    // "a is b": numbers, texts, yes/no compare by value; lists and boxes compare by what is inside.
    public static bool Same(object a, object b) => Same(a, b, 0);

    static bool Same(object a, object b, int depth)
    {
        if (ReferenceEquals(a, b)) return true;
        if (a == null || b == null) return false;
        if (depth > 50) return false;
        switch (a)
        {
            case OogaList la when b is OogaList lb:
                if (la.Items.Count != lb.Items.Count) return false;
                for (int i = 0; i < la.Items.Count; i++)
                    if (!Same(la.Items[i], lb.Items[i], depth + 1)) return false;
                return true;
            case OogaBox ba when b is OogaBox bb:
                if (ba.Count != bb.Count) return false;
                foreach (var key in ba.Keys)
                {
                    if (!bb.TryGet(key, out var vb)) return false;
                    ba.TryGet(key, out var va);
                    if (!Same(va, vb, depth + 1)) return false;
                }
                return true;
            default:
                return a.Equals(b);
        }
    }

    public class SameComparer : IEqualityComparer<object>
    {
        public new bool Equals(object a, object b) => Same(a, b);
        public int GetHashCode(object o) => o switch
        {
            null => 0,
            OogaList l => l.Items.Count,
            OogaBox b => b.Count + 7,
            _ => o.GetHashCode(),
        };
    }

    // Makes a plain copy: a new list or box with the same things inside.
    public static object Copy(object v) => v switch
    {
        OogaList l => new OogaList(l.Items),
        OogaBox b => CopyBox(b),
        _ => v,
    };

    static OogaBox CopyBox(OogaBox b)
    {
        var copy = new OogaBox();
        foreach (var key in b.Keys)
        {
            b.TryGet(key, out var value);
            copy.Set(key, value);
        }
        return copy;
    }
}
