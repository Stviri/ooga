using Ooga;

namespace Ooga.Tests;

public class ValueTests
{
    [Theory]
    [InlineData("say 5", "5")]
    [InlineData("say 3.5", "3.5")]
    [InlineData("say \"ooga\"", "ooga")]
    [InlineData("say yes", "yes")]
    [InlineData("say no", "no")]
    [InlineData("say nothing", "nothing")]
    [InlineData("say \"a\\nb\"", "a\nb")]
    [InlineData("say \"back\\\\slash\"", "back\\slash")]
    [InlineData("say 0.1 + 0.2", "0.3")]
    [InlineData("say 0 - 0", "0")]
    [InlineData("say 1000000 * 1000000", "1000000000000")]
    public void Shows_values_in_ooga_words(string src, string expected)
    {
        Assert.Equal(expected, O.Out(src));
    }

    [Fact]
    public void Things_can_be_made_changed_gained_and_lost()
    {
        Assert.Equal("100\n50\n60\n45", O.Out(O.Lines(
            "me has health 100",
            "say health",
            "health is 50",
            "say health",
            "health gain 10",
            "say health",
            "health lose 15",
            "say health")));
    }

    [Fact]
    public void A_thing_can_change_kind()
    {
        Assert.Equal("5\nfive\nyes", O.Out("me has x 5\nsay x\nx is \"five\"\nsay x\nx is yes\nsay x"));
    }

    [Fact]
    public void Text_gain_sticks_text_on_the_end()
    {
        Assert.Equal("ooga!1", O.Out("me has name \"ooga\"\nname gain \"!\"\nname gain 1\nsay name"));
    }

    [Theory]
    [InlineData("say \"hp: \" + 10", "hp: 10")]
    [InlineData("say \"a\" + 1 + 2", "a12")]
    [InlineData("say 1 + 2 + \"a\"", "3a")]
    [InlineData("say \"alive: \" + yes", "alive: yes")]
    [InlineData("say \"got \" + nothing", "got nothing")]
    public void Plus_with_text_joins(string src, string expected)
    {
        Assert.Equal(expected, O.Out(src));
    }

    [Fact]
    public void Me_is_only_labels_the_file()
    {
        Assert.Equal("hi", O.Out("me is player\nsay \"hi\""));
    }
}

public class MathAndPrecedenceTests
{
    [Theory]
    [InlineData("say 7 + 2", "9")]
    [InlineData("say 7 - 2", "5")]
    [InlineData("say 7 * 2", "14")]
    [InlineData("say 7 / 2", "3.5")]
    [InlineData("say 10 / 3", "3.33333333333333")]
    [InlineData("say 1 + 2 * 3", "7")]
    [InlineData("say (1 + 2) * 3", "9")]
    [InlineData("say 10 - 4 - 3", "3")]
    [InlineData("say 8 / 4 / 2", "1")]
    [InlineData("say 2 * 3 + 4 * 5", "26")]
    [InlineData("say 20 / (2 + 3) * 2", "8")]
    [InlineData("say -2 * 3", "-6")]
    [InlineData("say -(2 + 3)", "-5")]
    [InlineData("say 2 - -3", "5")]
    [InlineData("say ((((1))))", "1")]
    public void Arithmetic_follows_school_rules(string src, string expected)
    {
        Assert.Equal(expected, O.Out(src));
    }

    [Theory]
    [InlineData("say 2 + 3 is 5", "yes")]                 // math happens before the check
    [InlineData("say 1 is 1 and 2 is 3", "no")]
    [InlineData("say yes or yes and no", "yes")]          // and before or
    [InlineData("say (yes or yes) and no", "no")]
    [InlineData("say not yes and no", "no")]              // not sticks to the nearest check
    [InlineData("say not (yes and no)", "yes")]
    [InlineData("say not 1 is 2", "yes")]
    [InlineData("say not not yes", "yes")]
    public void Checks_combine_in_the_right_order(string src, string expected)
    {
        Assert.Equal(expected, O.Out(src));
    }

    [Fact]
    public void And_or_stop_early_when_answer_is_known()
    {
        // The right side would be a mistake (text is big number), but it is never looked at.
        Assert.Equal("no\nyes", O.Out("say no and \"a\" is big 1\nsay yes or \"a\" is big 1"));
    }
}

public class ComparisonTests
{
    [Theory]
    [InlineData("5 is 5", "yes")]
    [InlineData("5 is same 5", "yes")]
    [InlineData("5 is 6", "no")]
    [InlineData("5 is not 6", "yes")]
    [InlineData("5 is not 5", "no")]
    [InlineData("5 is big 3", "yes")]
    [InlineData("3 is big 5", "no")]
    [InlineData("3 is small 5", "yes")]
    [InlineData("5 is small 5", "no")]
    [InlineData("5 is big or same 5", "yes")]
    [InlineData("4 is big or same 5", "no")]
    [InlineData("5 is small or same 5", "yes")]
    [InlineData("6 is small or same 5", "no")]
    [InlineData("2 is not big 3", "yes")]
    [InlineData("\"ooga\" is \"ooga\"", "yes")]
    [InlineData("\"ooga\" is \"Ooga\"", "no")]
    [InlineData("1 is \"1\"", "no")]
    [InlineData("yes is yes", "yes")]
    [InlineData("yes is no", "no")]
    [InlineData("nothing is nothing", "yes")]
    [InlineData("0 is nothing", "no")]
    [InlineData("-1 is small 0", "yes")]
    public void Checks_answer_yes_or_no(string check, string expected)
    {
        Assert.Equal(expected, O.Out("say " + check));
    }
}

public class ConditionTests
{
    const string Ladder = """
        if hp is small or same 0
            say "dead"
        else if hp is small 30
            say "hurt"
        else
            say "fine"
        say "after"
        """;

    [Theory]
    [InlineData(0, "dead")]
    [InlineData(-5, "dead")]
    [InlineData(10, "hurt")]
    [InlineData(30, "fine")]
    [InlineData(100, "fine")]
    public void Only_first_true_branch_runs(int hp, string expected)
    {
        Assert.Equal(expected + "\nafter", O.Out($"me has hp {hp}\n" + Ladder));
    }

    [Fact]
    public void If_without_else_can_do_nothing()
    {
        Assert.Equal("end", O.Out("if no\n    say \"never\"\nsay \"end\""));
    }

    [Theory]
    [InlineData(5, 10, "two")]
    [InlineData(5, 1, "one")]
    [InlineData(5, 7, "three")]
    [InlineData(1, 10, "four")]
    public void Nested_conditions_pick_the_right_path(int a, int b, string expected)
    {
        string src = $"""
            me has a {a}
            me has b {b}
            if a is big 3
                if b is small 5
                    say "one"
                else if b is same 10
                    say "two"
                else
                    say "three"
            else
                say "four"
            say "done"
            """;
        Assert.Equal(expected + "\ndone", O.Out(src));
    }

    [Theory]
    [InlineData(1, 2, 3, "deep\nend")]
    [InlineData(1, 2, 4, "inner else\nend")]
    [InlineData(1, 5, 3, "end")]
    [InlineData(9, 2, 3, "outer else\nend")]
    public void Else_belongs_to_the_if_it_lines_up_with(int a, int b, int c, string expected)
    {
        string src = $"""
            me has a {a}
            me has b {b}
            me has c {c}
            if a is 1
                if b is 2
                    if c is 3
                        say "deep"
                    else
                        say "inner else"
            else
                say "outer else"
            say "end"
            """;
        Assert.Equal(expected, O.Out(src));
    }
}

public class LoopTests
{
    [Fact]
    public void Repeat_runs_the_block_that_many_times()
    {
        Assert.Equal("ooga\nooga\nooga", O.Out("repeat 3\n    say \"ooga\""));
    }

    [Fact]
    public void Repeat_zero_runs_nothing()
    {
        Assert.Equal("end", O.Out("repeat 0\n    say \"x\"\nsay \"end\""));
    }

    [Fact]
    public void Repeat_count_can_be_math()
    {
        Assert.Equal("4", O.Out("me has n 0\nme has times 2\nrepeat times * 2\n    n gain 1\nsay n"));
    }

    [Fact]
    public void Repeat_while_checks_before_each_round()
    {
        Assert.Equal("30\n20\n10\n0", O.Out("me has hp 30\nrepeat while hp is big 0\n    say hp\n    hp lose 10\nsay hp"));
    }

    [Fact]
    public void Count_goes_up_and_down()
    {
        Assert.Equal("1 2 3 | 3 2 1 | 5", O.Out("""
            me has line ""
            count i from 1 to 3
                line gain i + " "
            line gain "| "
            count i from 3 to 1
                line gain i + " "
            line gain "| "
            count i from 5 to 5
                line gain i
            say line
            """));
    }

    [Fact]
    public void Count_bounds_can_be_math_and_are_worked_out_once()
    {
        Assert.Equal("1\n2\n3", O.Out("me has top 3\ncount i from top - 2 to top\n    say i\n    top gain 10"));
    }

    [Fact]
    public void Stop_leaves_only_the_innermost_loop()
    {
        Assert.Equal("1-1\n2-1\n3-1", O.Out("""
            count i from 1 to 3
                count j from 1 to 3
                    if j is 2
                        stop
                    say i + "-" + j
            """));
    }

    [Fact]
    public void Skip_jumps_to_next_round()
    {
        Assert.Equal("1\n3", O.Out("count i from 1 to 3\n    if i is 2\n        skip\n    say i"));
        Assert.Equal("1\n2\n4", O.Out("""
            me has n 0
            repeat while n is small 4
                n gain 1
                if n is 3
                    skip
                say n
            """));
        Assert.Equal("a\na", O.Out("repeat 2\n    say \"a\"\n    skip\n    say \"b\""));
    }

    [Fact]
    public void Stop_works_in_repeat_while()
    {
        Assert.Equal("1\n2", O.Out("""
            me has n 0
            repeat while yes
                n gain 1
                if n is big 2
                    stop
                say n
            """));
    }

    [Fact]
    public void Count_thing_keeps_last_value_after_loop()
    {
        Assert.Equal("3", O.Out("count i from 1 to 3\n    say \"\"\nsay i").Split('\n').Last());
    }
}

public class ConsoleWordTests
{
    [Fact]
    public void Ask_shows_prompt_and_turns_plain_numbers_into_numbers()
    {
        var run = O.Run("me has a ask \"first?\"\nme has b ask\nsay a + b", "5", " 7 ");
        Assert.Equal("12", run.Output);
        Assert.Equal(new[] { "first?", null }, run.Host.Prompts);
    }

    [Theory]
    [InlineData("-3", "number")]
    [InlineData("2.5", "number")]
    [InlineData("grok", "text")]
    [InlineData("1e5", "text")]
    [InlineData("NaN", "text")]
    [InlineData("Infinity", "text")]
    [InlineData("1,5", "text")]
    [InlineData("", "text")]
    public void Only_plain_numbers_count_as_numbers(string typed, string kind)
    {
        // A number plus 0 stays a number; text plus 0 becomes text ending in 0.
        string said = O.Out("me has x ask\nsay x + 0", typed);
        Assert.Equal(kind == "number" ? typed : typed + "0", said);
    }

    [Fact]
    public void Ask_can_be_used_inside_a_loop()
    {
        Assert.Equal("a\nb", O.Out("repeat 2\n    me has word ask \"word?\"\n    say word", "a", "b"));
    }

    [Fact]
    public void Wait_asks_the_host_to_pause()
    {
        var run = O.Run("say 1\nwait 2\nwait 0.5\nsay 2");
        Assert.Equal(new[] { 2.0, 0.5 }, run.Host.Waits);
        Assert.Equal("1\n2", run.Output);
    }

    [Fact]
    public void Random_gives_whole_numbers_in_range()
    {
        var seen = O.Out("count i from 1 to 300\n    say random 1 to 6").Split('\n').Select(int.Parse).ToHashSet();
        Assert.Equal(new[] { 1, 2, 3, 4, 5, 6 }, seen.OrderBy(n => n));
        Assert.Equal("3", O.Out("say random 3 to 3"));
        Assert.Equal("ok", O.Out("me has r random 6 to 1\nif r is big or same 1 and r is small or same 6\n    say \"ok\""));
    }

    [Fact]
    public void Same_seed_same_random_numbers()
    {
        string src = "repeat 5\n    say random 1 to 1000";
        Assert.Equal(O.Out(src), O.Out(src));
    }

    [Fact]
    public void Me_die_ends_right_now()
    {
        var run = O.Run("""
            me can hit
                say "ouch"
                me die
            repeat 3
                me hit
            say "never"
            """);
        Assert.Equal("ouch", run.Output);
        Assert.Equal(RunEnd.Died, run.End);
    }

    [Fact]
    public void Normal_end_is_finished()
    {
        Assert.Equal(RunEnd.Finished, O.Run("say 1").End);
    }
}
