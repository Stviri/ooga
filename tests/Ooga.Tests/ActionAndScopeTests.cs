using Ooga;

namespace Ooga.Tests;

public class ActionTests
{
    [Fact]
    public void Action_with_things_and_answer()
    {
        Assert.Equal("ooga, grok!\n42\n5\n11\n10", O.Out("""
            me can greet who
                say "ooga, " + who + "!"
            me can double x
                give x * 2
            me can add a b
                give a + b
            me greet "grok"
            say me double 21
            say me add 2 3
            say (me double 5) + 1
            say me double (2 + 3)
            """));
    }

    [Fact]
    public void Each_thing_given_to_an_action_is_one_value()
    {
        // "me double 5 + 1" means (me double 5) + 1, and "me add 1 2 * 3" means (me add 1 2) * 3.
        Assert.Equal("11\n9", O.Out("""
            me can double x
                give x * 2
            me can add a b
                give a + b
            say me double 5 + 1
            say me add 1 2 * 3
            """));
    }

    [Fact]
    public void Action_with_no_things()
    {
        Assert.Equal("5", O.Out("me can five\n    give 5\nsay me five"));
    }

    [Fact]
    public void Give_leaves_the_action_at_once_even_inside_loops()
    {
        Assert.Equal("4\nnothing", O.Out("""
            me can first_big limit
                count i from 1 to 100
                    repeat 3
                        if i * i is big limit
                            give i
                give nothing
            say me first_big 10
            say me first_big 100000
            """));
    }

    [Fact]
    public void Action_without_give_answers_nothing()
    {
        Assert.Equal("hi\nnothing\nnothing", O.Out("""
            me can hello
                say "hi"
            me can empty
                give
            say me hello
            say me empty
            """));
    }

    [Fact]
    public void Actions_can_be_taught_after_they_are_used()
    {
        Assert.Equal("6", O.Out("say me triple 2\nme can triple x\n    give x * 3"));
    }

    [Fact]
    public void Actions_can_use_other_actions_and_themselves()
    {
        Assert.Equal("120\n55\n1", O.Out("""
            me can factorial n
                if n is small or same 1
                    give 1
                give n * (me factorial (n - 1))
            me can fib n
                if n is small 2
                    give n
                give (me fib (n - 1)) + (me fib (n - 2))
            me can is_even n
                if n is 0
                    give yes
                give me is_odd (n - 1)
            me can is_odd n
                if n is 0
                    give no
                give me is_even (n - 1)
            say me factorial 5
            say me fib 10
            if me is_even 10
                say 1
            """));
    }

    [Fact]
    public void Things_given_are_worked_out_before_the_action_starts()
    {
        Assert.Equal("3\n1", O.Out("""
            me has n 1
            me can show a
                n is 100
                say a
            me show (n + 2)
            n is 1
            say n
            """));
    }

    [Fact]
    public void Answer_can_feed_another_action()
    {
        Assert.Equal("8", O.Out("me can double x\n    give x * 2\nsay me double (me double 2)"));
    }

    [Fact]
    public void Deep_but_finite_actions_are_fine()
    {
        Assert.Equal("400", O.Out("me can sum n\n    if n is 0\n        give 0\n    give 1 + (me sum (n - 1))\nsay me sum 400"));
    }
}

public class ScopeTests
{
    [Fact]
    public void Things_made_inside_an_action_stay_inside()
    {
        var e = O.Fails("""
            me can f
                me has secret 1
                give secret
            say me f
            say secret
            """);
        Assert.Equal(5, e.Line);
        Assert.Contains("no thing called \"secret\"", e.Message);
    }

    [Fact]
    public void Action_things_given_stay_inside()
    {
        var e = O.Fails("me can f x\n    give x\nsay me f 1\nsay x");
        Assert.Equal((4, 5), (e.Line, e.Col));
    }

    [Fact]
    public void Action_can_read_and_change_file_things()
    {
        Assert.Equal("ouch 70\n70", O.Out("""
            me has health 100
            me can hit amount
                health lose amount
                say "ouch " + health
            me hit 30
            say health
            """));
    }

    [Fact]
    public void Action_thing_with_same_name_hides_file_thing()
    {
        Assert.Equal("5\n6\n1", O.Out("""
            me has x 1
            me can show x
                say x
                x is 6
                say x
            me show 5
            say x
            """));
    }

    [Fact]
    public void Me_has_inside_action_makes_a_new_inside_thing()
    {
        Assert.Equal("inside 9\noutside 1", O.Out("""
            me has x 1
            me can f
                me has x 9
                say "inside " + x
            me f
            say "outside " + x
            """));
    }

    [Fact]
    public void Count_inside_action_uses_inside_thing()
    {
        Assert.Equal("100", O.Out("me has i 100\nme can f\n    count i from 1 to 2\n        say i\nme f\nsay i").Split('\n').Last());
    }

    [Fact]
    public void Each_action_call_has_its_own_things()
    {
        Assert.Equal("1\n2\n3", O.Out("""
            me can down n
                if n is small 1
                    give 0
                me has mine n
                me has rest me down (n - 1)
                say mine
            me down 3
            """));
    }

    [Fact]
    public void Things_made_in_blocks_at_top_belong_to_whole_file()
    {
        Assert.Equal("7", O.Out("if yes\n    me has found 7\nsay found"));
    }

    [Fact]
    public void Action_sees_file_thing_made_later_if_it_exists_when_called()
    {
        Assert.Equal("7", O.Out("me can show\n    say score\nme has score 7\nme show"));
    }

    [Fact]
    public void Action_called_before_file_thing_is_made_fails_clearly()
    {
        var e = O.Fails("me can show\n    say score\nme show\nme has score 7");
        Assert.Equal((2, 9), (e.Line, e.Col));
        Assert.Contains("no have value yet", e.Message);
    }
}
