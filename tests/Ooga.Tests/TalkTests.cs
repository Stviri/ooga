using Ooga;
using Ooga.Cli;

namespace Ooga.Tests;

public class SessionTests
{
    [Fact]
    public void Things_and_actions_stay_between_pieces()
    {
        var host = new FakeHost();
        var talk = new OogaSession(host);
        talk.Run("me has x 5");
        talk.Run("me can double n\n    give n * 2");
        talk.Run("say me double x");
        talk.Run("x gain 1");
        talk.Run("say x");
        Assert.Equal(new[] { "10", "6" }, host.Said);
    }

    [Fact]
    public void A_value_alone_is_said()
    {
        var host = new FakeHost();
        var talk = new OogaSession(host);
        talk.Run("me has bag list 1 2");
        talk.Run("bag");
        talk.Run("1 + 2 * 3");
        talk.Run("size of bag");
        Assert.Equal(new[] { "list 1 2", "7", "2" }, host.Said);
    }

    [Fact]
    public void Lone_action_use_shows_its_answer()
    {
        var host = new FakeHost();
        var talk = new OogaSession(host);
        talk.Run("me can double n\n    give n * 2\nme can quiet\n    give");
        talk.Run("me double 4");
        talk.Run("me quiet");
        Assert.Equal(new[] { "8" }, host.Said);
        Assert.Null(talk.LastAnswer);
    }

    [Fact]
    public void Mistake_keeps_what_was_there_before()
    {
        var host = new FakeHost();
        var talk = new OogaSession(host);
        talk.Run("me has x 1");
        var e = Assert.Throws<OogaError>(() => talk.Run("say heath"));
        Assert.Equal(("talk piece 2", 1, 5), (e.File, e.Line, e.Col));
        talk.Run("say x");
        Assert.Equal(new[] { "1" }, host.Said);
    }

    [Fact]
    public void Action_can_be_taught_again_in_talk()
    {
        var host = new FakeHost();
        var talk = new OogaSession(host);
        talk.Run("me can f\n    give 1");
        talk.Run("me can f\n    give 2");
        talk.Run("say me f");
        Assert.Equal(new[] { "2" }, host.Said);
    }

    [Fact]
    public void Use_works_in_talk()
    {
        var host = new FakeHost();
        host.Files["tools.ooga"] = "me can hi\n    give \"hi\"";
        var talk = new OogaSession(host);
        talk.Run("me has x 1");
        talk.Run("use \"tools\"\nsay me hi + x");
        Assert.Equal(new[] { "hi1" }, host.Said);
    }
}

public class TalkCliTests
{
    [Fact]
    public void Talk_mode_runs_lines_and_blocks()
    {
        var run = Cli.Run(new[] { "--talk" },
            "me has x 5", "x + 1", "me can double n", "    give n * 2", "", "me double x", "say heath", "bye");
        Assert.Equal(CliApp.Ok, run.Exit);
        Assert.Contains("ooga> 6\n", run.Out);
        Assert.Contains("ooga> 10\n", run.Out);
        Assert.Contains("no thing called \"heath\"", run.Err);
        Assert.EndsWith("bye!\n", run.Out);
    }

    [Fact]
    public void Talk_mode_ends_when_typing_ends()
    {
        var run = Cli.Run(new[] { "-i" }, "say 1");
        Assert.Equal(CliApp.Ok, run.Exit);
        Assert.Contains("ooga> 1\n", run.Out);
    }

    [Fact]
    public void Me_die_ends_talk()
    {
        var run = Cli.Run(new[] { "--talk" }, "me die", "say 2");
        Assert.Contains("(me die. talk over.)", run.Out);
        Assert.DoesNotContain("ooga> 2", run.Out);
    }
}
