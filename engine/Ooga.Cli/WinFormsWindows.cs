#if OOGA_WINDOWS
using System.Runtime.ExceptionServices;
using System.Windows.Forms;
using Ooga;

namespace Ooga.Cli;

// The console host, plus real windows (Windows Forms). Only built into the Windows ooga.exe.
public class WindowsConsoleHost : ConsoleHost, IOogaWindows
{
    readonly WinFormsWindows windows = new();

    public WindowsConsoleHost(TextReader input, TextWriter output, bool realWait = true, string workDir = null, string nameRoot = null)
        : base(input, output, realWait, workDir, nameRoot) { }

    public IWindowControl NewWindow(string title) => windows.NewWindow(title);
    public IWindowControl Add(IWindowControl window, string kind, string text, IReadOnlyList<string> items, Action onClick) =>
        windows.Add(window, kind, text, items, onClick);
    public void Show(IWindowControl window) => windows.Show(window);
    public void Close(IWindowControl window) => windows.Close(window);
    public void Popup(string text) => windows.Popup(text);
    public string PickFile() => windows.PickFile();
    public string PickFolder() => windows.PickFolder();
}

// Ooga windows drawn with Windows Forms. Things in a window go top to bottom (a FlowLayoutPanel).
public class WinFormsWindows : IOogaWindows
{
    static bool started;
    Exception pending;   // a problem from a button's action, waiting to come out of Show

    static void Start()
    {
        if (started) return;
        started = true;
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
    }

    sealed class WindowParts
    {
        public Form Form;
        public FlowLayoutPanel Panel;
    }

    sealed class Control : IWindowControl
    {
        public System.Windows.Forms.Control Inner;
        public WindowParts Window;   // only for the window itself

        public string Text { get => Inner.Text; set => Inner.Text = value ?? ""; }

        public bool Checked
        {
            get => Inner is CheckBox box && box.Checked;
            set { if (Inner is CheckBox box) box.Checked = value; }
        }

        public IReadOnlyList<string> Items
        {
            get => Inner is ListBox list ? list.Items.Cast<object>().Select(o => o?.ToString() ?? "").ToList() : Array.Empty<string>();
            set
            {
                if (Inner is not ListBox list) return;
                list.BeginUpdate();
                list.Items.Clear();
                foreach (var item in value ?? Array.Empty<string>()) list.Items.Add(item);
                list.EndUpdate();
            }
        }

        public int PickedIndex
        {
            get => Inner is ListBox list ? list.SelectedIndex : -1;
            set { if (Inner is ListBox list) list.SelectedIndex = value; }
        }

        public int Width { get => Inner.Width; set => Inner.Width = value; }
        public int Height { get => Inner.Height; set => Inner.Height = value; }
    }

    public IWindowControl NewWindow(string title)
    {
        Start();
        var form = new Form
        {
            Text = title,
            Width = 420,
            Height = 320,
            StartPosition = FormStartPosition.CenterScreen,
            Font = new System.Drawing.Font("Segoe UI", 10f),
        };
        try { form.Icon = System.Drawing.Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch (Exception) { }
        var panel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoScroll = true,
            Padding = new Padding(12),
        };
        form.Controls.Add(panel);
        return new Control { Inner = form, Window = new WindowParts { Form = form, Panel = panel } };
    }

    public IWindowControl Add(IWindowControl window, string kind, string text, IReadOnlyList<string> items, Action onClick)
    {
        var parts = ((Control)window).Window;
        int wide = Math.Max(200, parts.Form.ClientSize.Width - 48);
        System.Windows.Forms.Control inner = kind switch
        {
            "label" => new Label { Text = text, AutoSize = true, MaximumSize = new System.Drawing.Size(wide, 0) },
            "button" => new Button { Text = text, AutoSize = true, Padding = new Padding(8, 2, 8, 2) },
            "text_box" => new TextBox { Text = text, Width = wide },
            "check_box" => new CheckBox { Text = text, AutoSize = true },
            "list_box" => new ListBox { Width = wide, Height = 120, IntegralHeight = false },
            _ => throw new ArgumentException("unknown kind " + kind),
        };
        if (inner is ListBox list)
            foreach (var item in items ?? Array.Empty<string>()) list.Items.Add(item);
        if (inner is Button button && onClick != null)
            button.Click += (_, _) => Run(onClick, parts.Form);
        parts.Panel.Controls.Add(inner);
        return new Control { Inner = inner };
    }

    // A button's action runs here. If it has a problem (or says "me die"), the window closes
    // and the problem comes back out of Show, so ooga reports it like any other.
    void Run(Action action, Form form)
    {
        if (pending != null) return;
        try
        {
            action();
        }
        catch (Exception e)
        {
            pending = e;
            foreach (Form open in Application.OpenForms.Cast<Form>().ToList()) open.Close();
        }
    }

    public void Show(IWindowControl window)
    {
        var form = ((Control)window).Window.Form;
        if (form.IsDisposed) throw new InvalidOperationException("this window was closed already. make a new one with: me window \"title\"");
        if (Application.MessageLoop) form.ShowDialog();   // opened from a button in another window
        else Application.Run(form);
        if (pending != null)
        {
            var e = pending;
            pending = null;
            ExceptionDispatchInfo.Capture(e).Throw();
        }
    }

    public void Close(IWindowControl window) => ((Control)window).Window.Form.Close();

    public void Popup(string text)
    {
        Start();
        MessageBox.Show(text, "Ooga");
    }

    public string PickFile()
    {
        Start();
        using var dialog = new OpenFileDialog();
        return dialog.ShowDialog() == DialogResult.OK ? dialog.FileName : null;
    }

    public string PickFolder()
    {
        Start();
        using var dialog = new FolderBrowserDialog();
        return dialog.ShowDialog() == DialogResult.OK ? dialog.SelectedPath : null;
    }
}
#endif
