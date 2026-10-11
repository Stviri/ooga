namespace Ooga;

// Windows with buttons, in ooga words:
//
//   me has win me window "Greeter"
//   me has name_box me text_box win
//   me has answer me label win ""
//   me can greet
//       text of answer is "ooga, " + text of name_box + "!"
//   me button win "Greet" (action greet)
//   me open_window win
//
// The core only knows the words. A host that can show windows implements IOogaWindows:
// the ooga command uses Windows Forms on Windows; tests use a pretend window system;
// a game engine could draw them its own way. Things go top to bottom in the window, no coordinates needed.
public interface IOogaWindows
{
    IWindowControl NewWindow(string title);

    // kind: label, button, text_box, check_box, list_box. onClick is only for buttons.
    IWindowControl Add(IWindowControl window, string kind, string text, IReadOnlyList<string> items, Action onClick);

    // Shows the window and waits until it is closed. Problems from button actions come back out of here.
    void Show(IWindowControl window);
    void Close(IWindowControl window);

    void Popup(string text);
    string PickFile();      // null when the person cancels
    string PickFolder();    // null when the person cancels
}

// One thing in a window (or the window itself), as the host draws it.
public interface IWindowControl
{
    string Text { get; set; }
    bool Checked { get; set; }
    IReadOnlyList<string> Items { get; set; }
    int PickedIndex { get; set; }   // -1 = nothing picked
    int Width { get; set; }
    int Height { get; set; }
}

// What ooga holds for a window or a thing in it. Its parts: "text of button", "checked of box", ...
public class OogaWidget : IOogaParts
{
    public string Kind { get; }
    public IWindowControl Control { get; }
    public OogaWidget Window { get; }   // the window this thing is in (itself for a window)

    public OogaWidget(string kind, IWindowControl control, OogaWidget window)
    {
        Kind = kind;
        Control = control;
        Window = window ?? this;
    }

    public IEnumerable<string> PartNames => Kind switch
    {
        "window" => new[] { "text", "width", "height" },
        "check_box" => new[] { "text", "checked" },
        "list_box" => new[] { "items", "picked" },
        _ => new[] { "text" },
    };

    string NoPart(string name) =>
        $"{Kind} no have part {name}. a {Kind} has: {string.Join(", ", PartNames)}";

    public string TryGetPart(string name, out object value)
    {
        value = null;
        if (!PartNames.Contains(name)) return NoPart(name);
        switch (name)
        {
            case "text": value = Control.Text ?? ""; break;
            case "checked": value = Control.Checked; break;
            case "width": value = (double)Control.Width; break;
            case "height": value = (double)Control.Height; break;
            case "items": value = new OogaList(Control.Items ?? Array.Empty<string>()); break;
            case "picked":
                var items = Control.Items ?? Array.Empty<string>();
                int i = Control.PickedIndex;
                value = i >= 0 && i < items.Count ? items[i] : null;
                break;
        }
        return null;
    }

    public string TrySetPart(string name, object value)
    {
        if (!PartNames.Contains(name)) return NoPart(name);
        switch (name)
        {
            case "text":
                Control.Text = Values.Show(value);
                return null;
            case "checked":
                if (value is not bool b) return $"checked of {Kind} must be yes or no, not {Values.Describe(value)}.";
                Control.Checked = b;
                return null;
            case "width":
            case "height":
                if (value is not double d || d != Math.Floor(d) || d < 50 || d > 10000)
                    return $"{name} of window must be a whole number from 50 to 10000, not {Values.Describe(value)}.";
                if (name == "width") Control.Width = (int)d; else Control.Height = (int)d;
                return null;
            case "items":
                if (value is not OogaList list) return $"items of list_box must be a list, not {Values.Describe(value)}.";
                Control.Items = list.Items.Select(Values.Show).ToList();
                return null;
            case "picked":
                var items = Control.Items ?? Array.Empty<string>();
                if (value == null) { Control.PickedIndex = -1; return null; }
                int at = items.ToList().IndexOf(Values.Show(value));
                if (at < 0) return $"list_box no have {Values.Describe(value)} to pick.";
                Control.PickedIndex = at;
                return null;
        }
        return NoPart(name);
    }

    public override string ToString() => Kind == "list_box" ? "list_box" : $"{Kind} \"{Control.Text}\"";
}

public static class WindowLibrary
{
    public static IEnumerable<OogaAction> Actions()
    {
        yield return new("window", 1, c => new OogaWidget("window", Windows(c).NewWindow(c.Text(0)), null));
        yield return new("label", 2, c => Add(c, "label", Values.Show(c[1]), null, null));
        yield return new("button", 3, c => Add(c, "button", c.Text(1), null, ClickAction(c, 2)));
        yield return new("text_box", 1, 2, c => Add(c, "text_box", c.Count > 1 ? Values.Show(c[1]) : "", null, null));
        yield return new("check_box", 2, c => Add(c, "check_box", c.Text(1), null, null));
        yield return new("list_box", 2, c => Add(c, "list_box", "", c.List(1).Items.Select(Values.Show).ToList(), null));
        yield return new("open_window", 1, c => { Windows(c).Show(WindowOf(c, 0).Control); return null; });
        yield return new("close_window", 1, c => { Windows(c).Close(WindowOf(c, 0).Control); return null; });
        yield return new("popup", 1, c => { Windows(c).Popup(Values.Show(c[0])); return null; });
        yield return new("pick_file", 0, c => Windows(c).PickFile());
        yield return new("pick_folder", 0, c => Windows(c).PickFolder());
    }

    static IOogaWindows Windows(ActionCall c) =>
        c.Host as IOogaWindows
        ?? throw c.Problem($"{c.Name} need windows, but ooga is running somewhere with no windows. " +
                           "windows need Windows, and the ooga.exe made for it.");

    static OogaWidget WindowOf(ActionCall c, int i) =>
        c[i] is OogaWidget { Kind: "window" } w
            ? w
            : throw c.Problem($"{c.Name} need a window (from: me window \"title\"), but got {Values.Describe(c[i])}.");

    static OogaWidget Add(ActionCall c, string kind, string text, IReadOnlyList<string> items, Action onClick)
    {
        var window = WindowOf(c, 0);
        return new OogaWidget(kind, Windows(c).Add(window.Control, kind, text, items, onClick), window);
    }

    // The action a button runs. It is checked now, so a mistake shows when the button is made, not when it is clicked.
    static Action ClickAction(ActionCall c, int i)
    {
        if (c[i] is not ActionValue action)
            throw c.Problem($"button need an action to run when clicked, like: (action greet), but got {Values.Describe(c[i])}.");
        var (least, _) = c.Wants(action);
        if (least > 0)
            throw c.Problem($"action {action.Name} want {Library.CountText(least)}, but a button gives it nothing when clicked. " +
                            $"teach it with no things, like: me can {action.Name}");
        return () => c.Invoke(action, Array.Empty<object>());
    }
}
