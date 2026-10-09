using Ooga;

namespace Ooga.Tests;

public class TryTests
{
    [Fact]
    public void Oops_runs_when_something_goes_wrong()
    {
        Assert.Equal("before\ncaught: no can split by 0.\nafter", O.Out("""
            try
                say "before"
                say 1 / 0
                say "never"
            oops why
                say "caught: " + why
            say "after"
            """));
    }

    [Fact]
    public void Oops_is_skipped_when_all_is_fine()
    {
        Assert.Equal("fine\nafter", O.Out("try\n    say \"fine\"\noops\n    say \"never\"\nsay \"after\""));
    }

    [Fact]
    public void Fail_makes_your_own_problem()
    {
        Assert.Equal("health no can be -5", O.Out("""
            me can set_health h
                if h is small 0
                    fail "health no can be " + h
                give h
            try
                me set_health (-5)
            oops why
                say why
            """));
    }

    [Fact]
    public void Uncaught_fail_stops_the_program_at_the_fail_line()
    {
        var e = O.Fails("say 1\nfail \"ooga stop here\"\nsay 2");
        Assert.Equal((2, 1, "ooga stop here"), (e.Line, e.Col, e.Message));
    }

    [Fact]
    public void Problem_deep_inside_actions_is_caught_and_things_are_back_to_normal()
    {
        Assert.Equal("caught\nlocal x is gone: 5", O.Out("""
            me has x 5
            me can deep n
                me has x n
                if n is 0
                    fail "bottom"
                give me deep (n - 1)
            try
                say me deep 10
            oops
                say "caught"
            say "local x is gone: " + x
            """));
    }

    [Fact]
    public void Try_inside_loop_and_action_keeps_stop_skip_give_working()
    {
        Assert.Equal("1\n3\nfound 4", O.Out("""
            me can find_four
                each n in list 1 2 3 4 5
                    try
                        if n is 2
                            skip
                        if n is 4
                            give "found " + n
                        say n
                    oops
                        say "no"
            say me find_four
            """));
    }

    [Fact]
    public void Problem_in_oops_goes_further_out()
    {
        Assert.Equal("outer: second", O.Out("""
            try
                try
                    fail "first"
                oops
                    fail "second"
            oops why
                say "outer: " + why
            """));
    }

    [Fact]
    public void Ooga_tired_can_not_be_caught()
    {
        var host = new FakeHost();
        var e = Assert.Throws<OogaError>(() => O.Bounded(_ => OogaRunner.Run(
            "try\n    repeat while yes\n        skip\noops\n    say \"caught\"", host, new RunOptions { MaxSteps = 1000 })));
        Assert.Contains("ooga tired", e.Message);
        Assert.Empty(host.Said);
    }

    [Fact]
    public void Me_die_is_not_a_problem_to_catch()
    {
        var run = O.Run("try\n    me die\noops\n    say \"caught\"\nsay \"after\"");
        Assert.Equal((RunEnd.Died, ""), (run.End, run.Output));
    }
}

public class UseTests
{
    static FakeHost WithFiles(params (string Name, string Text)[] files)
    {
        var host = new FakeHost();
        foreach (var (name, text) in files) host.Files[name] = text;
        return host;
    }

    [Fact]
    public void Use_brings_in_actions_and_things()
    {
        var host = WithFiles(("tools.ooga", "me has greeting \"ooga\"\nme can shout w\n    give (me upper w) + \"!\""));
        Assert.Equal("ooga\nHI!", O.Run("use \"tools\"\nsay greeting\nsay me shout \"hi\"", host).Output);
    }

    [Fact]
    public void Each_file_is_used_only_once()
    {
        var host = WithFiles(("a.ooga", "use \"b\"\nsay \"a\""), ("b.ooga", "use \"a\"\nsay \"b\""));
        Assert.Equal("b\na", O.Run("use \"a\"\nuse \"b\"\nuse \"a.ooga\"", host).Output);
    }

    [Fact]
    public void Mistake_in_used_file_names_that_file()
    {
        var host = WithFiles(("tools.ooga", "me can broken\n    say nope"));
        var e = O.Fails("use \"tools\"\nsay 1", host);
        Assert.Equal(("tools.ooga", 2, 9), (e.File, e.Line, e.Col));
        Assert.Contains("ooga booga! problem in tools.ooga line 2, column 9:\n        say nope", e.Report().ToString());
    }

    [Fact]
    public void Running_problem_in_used_file_names_that_file()
    {
        var host = WithFiles(("math.ooga", "me can split_it n\n    give n / 0"));
        var e = O.Fails("use \"math\"\nsay me split_it 4", host);
        Assert.Equal(("math.ooga", 2, 12), (e.File, e.Line, e.Col));
    }

    [Fact]
    public void Same_action_in_two_files_is_a_mistake()
    {
        var host = WithFiles(("a.ooga", "me can jump\n    say 1"));
        var e = O.Fails("use \"a\"\nme can jump\n    say 2", host);
        Assert.Contains("me already know how to jump (a.ooga line 1)", e.Message);
        Assert.Equal("main.ooga", e.File);
    }

    [Fact]
    public void Missing_file_to_use()
    {
        var e = O.Fails("say 1\nuse \"nope\"");
        Assert.Equal((2, 1), (e.Line, e.Col));
        Assert.Contains("ooga no find file to use: \"nope\"", e.Message);
    }

    [Fact]
    public void Use_must_be_at_the_left_edge()
    {
        Assert.Contains("use must be at left edge", O.Fails("if yes\n    use \"x\"").Message);
    }

    [Fact]
    public void Use_needs_a_host_with_files()
    {
        var e = Assert.Throws<OogaError>(() => OogaRunner.Run("use \"x\"", new NoFilesHost()));
        Assert.Contains("use need files", e.Message);
    }
}

public class CSharpBridgeTests
{
    [Theory]
    [InlineData("me csharp \"System.Math\" \"Sqrt\" 16", "4")]
    [InlineData("me csharp \"System.Math\" \"Max\" 3 9", "9")]
    [InlineData("me csharp \"Math\" \"Abs\" (-3)", "3")]
    [InlineData("me round_to (me csharp \"System.Math\" \"PI\") 4", "3.1416")]
    [InlineData("me csharp \"System.String\" \"Join\" \"-\" (list \"a\" \"b\")", "a-b")]
    [InlineData("me csharp \"System.String\" \"Concat\" \"a\" \"b\"", "ab")]
    [InlineData("me csharp \"System.Int32\" \"Parse\" \"42\"", "42")]
    [InlineData("me csharp \"System.Char\" \"IsDigit\" \"7\"", "yes")]
    [InlineData("me csharp \"System.IO.Path\" \"GetExtension\" \"cave.ooga\"", ".ooga")]
    [InlineData("me csharp \"System.Environment\" \"NewLine\" is nothing", "no")]
    [InlineData("me csharp_get \"hello\" \"Length\"", "5")]
    [InlineData("me csharp_call \"hello\" \"ToUpper\"", "HELLO")]
    [InlineData("me csharp_call \"a,b\" \"Split\" \",\"", "list \"a\" \"b\"")]
    [InlineData("me csharp_call \"hello\" \"Substring\" 1 3", "ell")]
    [InlineData("me csharp \"System.DayOfWeek\" \"Monday\"", "Monday")]
    public void Call_into_csharp(string call, string expected)
    {
        Assert.Equal(expected, O.Out("say " + call));
    }

    [Fact]
    public void Make_a_csharp_thing_and_use_it()
    {
        Assert.Equal("ooga 42 yes\ncsharp", O.Out("""
            me has sb me csharp_new "System.Text.StringBuilder"
            me csharp_call sb "Append" "ooga "
            me csharp_call sb "Append" 42
            me csharp_call sb "Append" " yes"
            say sb
            say kind of sb
            """));
    }

    [Fact]
    public void Csharp_list_stays_a_csharp_thing()
    {
        Assert.Equal("2\nb", O.Out("""
            me has names me csharp_new "System.Collections.Generic.List`1[System.String]"
            me csharp_call names "Add" "a"
            me csharp_call names "Add" "b"
            say me csharp_get names "Count"
            say me csharp_call names "get_Item" 1
            """));
    }

    [Fact]
    public void Set_a_property()
    {
        Assert.Equal("ooga", O.Out("""
            me has sb me csharp_new "System.Text.StringBuilder" "oogabooga"
            me csharp_set sb "Length" 4
            say sb
            """));
    }

    [Fact]
    public void Csharp_dictionary_made_stays_csharp_but_returned_one_becomes_a_box()
    {
        Assert.Equal("yes\ncsharp\nbox", O.Out("""
            me has d me csharp_new "System.Collections.Generic.Dictionary`2[System.String,System.Int32]"
            me csharp_call d "Add" "a" 1
            say me csharp_call d "ContainsKey" "a"
            say kind of d
            say kind of me csharp "System.Environment" "GetEnvironmentVariables"
            """));
    }

    [Theory]
    [InlineData("say me csharp \"System.Nope\" \"X\"", "ooga no find C# type \"System.Nope\"")]
    [InlineData("say me csharp \"System.Math\" \"Nope\" 1", "Math no have static method \"Nope\"")]
    [InlineData("say me csharp \"System.Math\" \"Sqrt\" \"a\"", "Math.Sqrt no take 1 thing like that. it can take: Sqrt(Double)")]
    [InlineData("say me csharp \"System.Int32\" \"Parse\" \"ooga\"", "C# say problem:")]
    [InlineData("say me csharp_get \"hi\" \"Nope\"", "String no have property or field \"Nope\"")]
    [InlineData("say me csharp_call nothing \"ToString\"", "csharp_call need a thing to work on, but got nothing.")]
    [InlineData("me csharp_set \"hi\" \"Length\" 1", "String.Length can only be read, not changed.")]
    public void Csharp_mistakes_are_ooga_problems(string src, string message)
    {
        Assert.Contains(message, O.Fails(src).Message);
    }

    [Fact]
    public void Csharp_problem_can_be_caught()
    {
        Assert.StartsWith("C# say problem:", O.Out("try\n    say me csharp \"System.Int32\" \"Parse\" \"x\"\noops why\n    say why"));
    }

    [Fact]
    public void Csharp_can_be_turned_off()
    {
        var e = O.Fails("say me csharp \"System.Math\" \"Sqrt\" 4", new FakeHost(), new RunOptions { AllowCSharp = false });
        Assert.Contains("me no know how to csharp", e.Message);
    }
}

public class NewErrorTests
{
    public static TheoryData<string, int, int, string> Mistakes => new()
    {
        { "me has bag list 1 2\nsay item 3 of bag", 2, 10, "list has 2 things (1 to 2). no item 3." },
        { "me has bag list\nsay item 1 of bag", 2, 10, "list is empty. no item 1." },
        { "me has bag list 1\nsay item 1.5 of bag", 2, 10, "need a whole number, but got number 1.5" },
        { "me has bag list 1\nitem 0 of bag is 5", 2, 6, "no item 0" },
        { "me has p box health 1\nsay helth of p", 2, 5, "box no have part helth. you mean health?" },
        { "me has p box\nsay health of p", 2, 5, "box is empty. no part health." },
        { "me has n 5\nsay health of n", 2, 5, "only a box has parts. n is number 5, so it no have health." },
        { "me has n 5\nhealth of n is 1", 2, 1, "only a box has parts. n is number 5." },
        { "say size of 5", 1, 5, "size of number 5? only lists, texts and boxes have a size." },
        { "say item 1 of 5", 1, 5, "only a list, a text or a box has items." },
        { "me has t \"abc\"\nitem 1 of t is \"x\"", 2, 1, "no can change one letter of a text" },
        { "say \"abc\" has 1", 1, 11, "text can only have text inside" },
        { "say 5 has 1", 1, 7, "only a list, a box or a text can have things" },
        { "each x in 5\n    say x", 1, 11, "each need a list, a text or a box, but got number 5." },
        { "me has bag list 1\nbag lose 2", 2, 1, "list no have number 2 to lose." },
        { "say size bag", 1, 10, "size need \"of\" next, like: size of bag" },
        { "say item 1 bag", 1, 12, "item need \"of\" next" },
        { "say box health", 1, 15, "box part health need a value, like: box health 0" },
        { "say box a 1 a 2", 1, 13, "box already has part a" },
        { "each x bag\n    say x", 1, 8, "each need \"in\" next" },
        { "try\n    say 1\nsay 2", 1, 1, "try need oops right after its lines" },
        { "oops\n    say 1", 1, 1, "oops here, but no try right above it." },
        { "fail", 1, 5, "fail need a reason" },
        { "me has bag list\nbag has 1", 2, 5, "this look like a check. checks go after if, like: if bag has 5" },
        { "say 5 % 0", 1, 7, "no can split by 0." },
        { "me has list 1", 1, 8, "\"list\" is special ooga word" },
        { "each x in list 1\n    stop\nstop", 3, 1, "stop only work inside repeat, count or each." },
    };

    [Theory]
    [MemberData(nameof(Mistakes))]
    public void New_mistake_is_reported_at_the_right_spot(string source, int line, int col, string message)
    {
        var e = O.Fails(source);
        Assert.Contains(message, e.Message);
        Assert.Equal((line, col), (e.Line, e.Col));
    }
}
