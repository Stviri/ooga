using Ooga;

namespace Ooga.Tests;

// The actions ooga already knows: "me round 2.5", "me split ...", ...
public class LibraryTests
{
    [Theory]
    // numbers
    [InlineData("me round 2.5", "3")]
    [InlineData("me round (-2.5)", "-3")]
    [InlineData("me round 2.4", "2")]
    [InlineData("me round_to 3.14159 2", "3.14")]
    [InlineData("me round_down 2.9", "2")]
    [InlineData("me round_up 2.1", "3")]
    [InlineData("me positive (-4)", "4")]
    [InlineData("me power 2 10", "1024")]
    [InlineData("me root 81", "9")]
    [InlineData("me biggest 3 7", "7")]
    [InlineData("me smallest 3 7", "3")]
    [InlineData("me biggest (list 4 9 2)", "9")]
    [InlineData("me smallest (list 4 9 2)", "2")]
    [InlineData("me numbers 1 4", "list 1 2 3 4")]
    [InlineData("me numbers 3 1", "list 3 2 1")]
    // texts
    [InlineData("me upper \"ooga\"", "OOGA")]
    [InlineData("me lower \"OOGA\"", "ooga")]
    [InlineData("me trim \"  hi  \"", "hi")]
    [InlineData("me split \"a,b,c\" \",\"", "list \"a\" \"b\" \"c\"")]
    [InlineData("me split \"hey\" \"\"", "list \"h\" \"e\" \"y\"")]
    [InlineData("me join (list 1 \"b\" yes) \", \"", "1, b, yes")]
    [InlineData("me replace \"ooga booga\" \"oo\" \"u\"", "uga buga")]
    [InlineData("me starts_with \"caveman\" \"cave\"", "yes")]
    [InlineData("me ends_with \"caveman\" \"cave\"", "no")]
    [InlineData("me number \"42\"", "42")]
    [InlineData("me number \" -2.5 \"", "-2.5")]
    [InlineData("me number \"grok\"", "nothing")]
    [InlineData("me number 7", "7")]
    [InlineData("me text 7", "7")]
    [InlineData("(me text 7) + 1", "71")]
    [InlineData("me text (list 1 \"a\")", "list 1 \"a\"")]
    // texts and lists
    [InlineData("me piece \"hello\" 2 4", "ell")]
    [InlineData("me piece (list 1 2 3 4) 2 3", "list 2 3")]
    [InlineData("me piece \"hi\" 2 1", "")]
    [InlineData("me find \"hello\" \"l\"", "3")]
    [InlineData("me find \"hello\" \"z\"", "0")]
    [InlineData("me find (list 5 6 7) 7", "3")]
    [InlineData("me reverse \"abc\"", "cba")]
    [InlineData("me reverse (list 1 2 3)", "list 3 2 1")]
    // lists
    [InlineData("me sort (list 3 1 2)", "list 1 2 3")]
    [InlineData("me sort (list \"b\" \"a\")", "list \"a\" \"b\"")]
    [InlineData("me copy (list 1 2)", "list 1 2")]
    [InlineData("me pick (list 7)", "7")]
    [InlineData("size of me shuffle (list 1 2 3)", "3")]
    public void Built_in_action_gives(string call, string expected)
    {
        Assert.Equal(expected, O.Out("say " + call));
    }

    [Fact]
    public void Take_and_put_at_change_the_list()
    {
        Assert.Equal("2\nlist 1 3\nlist 0 1 9 3", O.Out("""
            me has bag list 1 2 3
            say me take bag 2
            say bag
            me put_at bag 2 9
            me put_at bag 1 0
            say bag
            """));
    }

    [Fact]
    public void Shuffle_keeps_every_item()
    {
        Assert.Equal("list 1 2 3 4 5", O.Out("say me sort (me shuffle (me numbers 1 5))"));
    }

    [Fact]
    public void Time_and_date()
    {
        Assert.Equal("yes\nyes", O.Out("say me time is big or same 0\nsay size of me date is 19"));
    }

    [Fact]
    public void Arguments_come_from_the_runner()
    {
        var run = O.Run("say me arguments", new FakeHost(), new RunOptions { Arguments = new[] { "one", "2" } });
        Assert.Equal("list \"one\" \"2\"", run.Output);
        Assert.Equal("list", O.Out("say me arguments"));
    }

    [Fact]
    public void Files_can_be_written_added_to_and_read()
    {
        var host = new FakeHost();
        var run = O.Run("""
            me write_file "notes.txt" "one"
            me add_to_file "notes.txt" "\ntwo"
            say me read_file "notes.txt"
            say me file_exists "notes.txt"
            say me file_exists "other.txt"
            """, host);
        Assert.Equal("one\ntwo\nyes\nno", run.Output);
        Assert.Equal("one\ntwo", host.Files["notes.txt"]);
    }

    [Fact]
    public void Missing_file_is_a_problem_try_can_catch()
    {
        Assert.Equal("read_file no find file: nope.txt", O.Out("try\n    say me read_file \"nope.txt\"\noops why\n    say why"));
    }

    [Fact]
    public void Files_need_a_host_with_files()
    {
        var host = new NoFilesHost();
        var e = Assert.Throws<OogaError>(() => OogaRunner.Run("say me read_file \"x\"", host));
        Assert.Contains("read_file need files, but ooga is running somewhere with no files", e.Message);
    }

    [Fact]
    public void Built_in_names_can_not_be_taught_again()
    {
        var e = O.Fails("me can round x\n    give x");
        Assert.Contains("\"round\" is built in", e.Message);
    }

    [Theory]
    [InlineData("say me round \"a\"", "round need a number, but got text \"a\".")]
    [InlineData("say me power 2", "power want 2 things but got 1 thing.")]
    [InlineData("say me biggest 1 2 3", "biggest want 1 to 2 things but got 3 things.")]
    [InlineData("say me root (-1)", "no can take root of number below 0.")]
    [InlineData("say me take (list 1) 5", "list has 1 thing (1 to 1). no thing at 5.")]
    [InlineData("say me pick (list)", "no can pick from empty list.")]
    [InlineData("say me sort (list 1 \"a\")", "sort need a list of only numbers or only texts.")]
    [InlineData("say me piece \"abc\" 2 9", "piece 2 to 9 no fit. it has 3 things (1 to 3).")]
    [InlineData("say me split 5 \",\"", "split need a text (thing 1), but got number 5.")]
    [InlineData("say me rond 2.5", "me no know how to rond. you mean round?")]
    [InlineData("say me power 10 1000", "number too big for ooga")]
    [InlineData("say me numbers 1 2.5", "numbers need a whole number (thing 2), but got 2.5.")]
    public void Built_in_mistakes(string src, string message)
    {
        Assert.Contains(message, O.Fails(src).Message);
    }

    [Fact]
    public void Host_can_add_its_own_actions()
    {
        var jumps = new List<double>();
        var actions = new[]
        {
            new OogaAction("jump", 1, c => { jumps.Add(c.Number(0)); return "boing"; }),
            new OogaAction("roar", 0, 2, c => "ROAR x" + c.Count),
        };
        var run = O.Run("say me jump 3\nsay me roar\nsay me roar 1 2", new FakeHost(), new RunOptions { Actions = actions });
        Assert.Equal("boing\nROAR x0\nROAR x2", run.Output);
        Assert.Equal(new[] { 3.0 }, jumps);

        var e = O.Fails("me jump", new FakeHost(), new RunOptions { Actions = actions });
        Assert.Contains("jump want 1 thing but got 0 things.", e.Message);
    }
}
