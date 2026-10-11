using Ooga;

namespace Ooga.Tests;

// Windows and buttons, with a pretend window system: the "person" is a little script that types and clicks.
public class WindowTests
{
    const string Greeter = """
        me has win me window "Greeter"
        me label win "your name?"
        me has name_box me text_box win
        me has answer me label win ""
        me can greet
            text of answer is "ooga, " + text of name_box + "!"
        me button win "Greet" (action greet)
        me open_window win
        say text of answer
        """;

    [Fact]
    public void Type_click_and_read_the_answer()
    {
        var host = new FakeHost();
        host.WhenShown.Enqueue(w => { w.Type("Grok"); w.Click("Greet"); });
        var run = O.Run(Greeter, host);
        Assert.Equal("ooga, Grok!", run.Output);
        var win = Assert.Single(host.Windows);
        Assert.Equal("Greeter", win.Text);
        Assert.Equal(new[] { "label", "text_box", "label", "button" }, win.Children.Select(c => c.Kind));
        Assert.True(win.Closed);
    }

    [Fact]
    public void Each_click_runs_the_action_again()
    {
        var host = new FakeHost();
        host.WhenShown.Enqueue(w => { w.Click("+1"); w.Click("+1"); w.Click("+1"); });
        var run = O.Run("""
            me has clicks 0
            me has win me window "Counter"
            me has shown me label win "0"
            me can add_one
                clicks gain 1
                text of shown is clicks
            me button win "+1" (action add_one)
            me open_window win
            say clicks
            """, host);
        Assert.Equal("3", run.Output);
        Assert.Equal("3", host.Windows[0].Find("label").Text);
    }

    [Fact]
    public void Check_box_list_box_and_window_parts()
    {
        var host = new FakeHost();
        host.WhenShown.Enqueue(w =>
        {
            w.Find("check_box").Checked = true;
            w.Find("list_box").PickedIndex = 1;
        });
        var run = O.Run("""
            me has win me window "Shop"
            me has gift me check_box win "wrap it?"
            me has pick me list_box win (list "club" "rock")
            say picked of pick
            me open_window win
            say checked of gift
            say picked of pick
            items of pick is list "a" "b" "c"
            say items of pick
            picked of pick is "c"
            say picked of pick
            width of win is 600
            say width of win
            say text of win
            say kind of pick
            """, host);
        Assert.Equal("nothing\nyes\nrock\na, b, c\nc\n600\nShop\nlist_box", run.Output);
    }

    [Fact]
    public void Popup_and_pickers()
    {
        var host = new FakeHost();
        host.PickAnswers.Enqueue("C:\\cave\\notes.txt");
        var run = O.Run("me popup \"hi!\"\nsay me pick_file\nsay me pick_folder", host);
        Assert.Equal(new[] { "hi!" }, host.Popups);
        Assert.Equal("C:\\cave\\notes.txt\nnothing", run.Output);
    }

    [Fact]
    public void Problem_in_a_button_action_is_reported_at_its_line()
    {
        var host = new FakeHost();
        host.WhenShown.Enqueue(w => w.Click("Go"));
        var e = O.Fails("me has win me window \"x\"\nme can go\n    say 1 / 0\nme button win \"Go\" (action go)\nme open_window win", host);
        Assert.Equal((3, 11, "no can split by 0."), (e.Line, e.Col, e.Message));
    }

    [Fact]
    public void Me_die_in_a_button_action_ends_the_program()
    {
        var host = new FakeHost();
        host.WhenShown.Enqueue(w => w.Click("Quit"));
        var run = O.Run("me has win me window \"x\"\nme can quit\n    me die\nme button win \"Quit\" (action quit)\nme open_window win\nsay \"never\"", host);
        Assert.Equal((RunEnd.Died, ""), (run.End, run.Output));
    }

    [Theory]
    [InlineData("me has win me window \"x\"\nme has l me label win \"a\"\nsay checked of l", 3, 5, "label no have part checked. a label has: text")]
    [InlineData("me has win me window \"x\"\nme has b me check_box win \"a\"\nchecked of b is 5", 3, 1, "checked of check_box must be yes or no, not number 5.")]
    [InlineData("me has win me window \"x\"\nwidth of win is 10", 2, 1, "width of window must be a whole number from 50 to 10000")]
    [InlineData("me has win me window \"x\"\nme has p me list_box win (list \"a\")\npicked of p is \"z\"", 3, 1, "list_box no have text \"z\" to pick.")]
    [InlineData("me label 5 \"a\"", 1, 1, "label need a window (from: me window \"title\"), but got number 5.")]
    [InlineData("me has win me window \"x\"\nme can hit n\n    say n\nme button win \"Hit\" (action hit)", 4, 1, "action hit want 1 thing, but a button gives it nothing when clicked")]
    [InlineData("me has win me window \"x\"\nme button win \"Hit\" 5", 2, 1, "button need an action to run when clicked, like: (action greet)")]
    public void Window_mistakes(string source, int line, int col, string message)
    {
        var e = O.Fails(source);
        Assert.Contains(message, e.Message);
        Assert.Equal((line, col), (e.Line, e.Col));
    }

    [Fact]
    public void A_host_without_windows_says_so()
    {
        var e = Assert.Throws<OogaError>(() => OogaRunner.Run("me has w me window \"x\"", new NoFilesHost()));
        Assert.Contains("window need windows, but ooga is running somewhere with no windows", e.Message);
    }

    [Fact]
    public void Checks_never_open_real_windows()
    {
        // ooga --test runs scripts with a console host that has no windows, so a check can never get stuck on one.
        var run = Cli.Run(new[] { Path.Combine(O.RepoRoot(), "examples", "extra", "19_window.ooga") });
        Assert.Equal(Ooga.Cli.CliApp.ScriptProblem, run.Exit);
        Assert.Contains("need windows, but ooga is running somewhere with no windows", run.Err);
    }
}
