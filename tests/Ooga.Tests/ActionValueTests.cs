using Ooga;

namespace Ooga.Tests;

// "action double" holds an action as a value, so it can be handed to other actions.
public class ActionValueTests
{
    const string Helpers = """
        me can double x
            give x * 2
        me can is_big x
            give x is big 2
        me can add a b
            give a + b
        """;

    [Fact]
    public void Hold_and_call_an_action()
    {
        Assert.Equal("10\naction double\naction\n7", O.Out(Helpers + "\n" + """
            me has f action double
            say me call f 5
            say f
            say kind of f
            say me call (action add) 3 4
            """));
    }

    [Fact]
    public void Built_in_actions_are_values_too()
    {
        Assert.Equal("OOGA", O.Out("me has f action upper\nsay me call f \"ooga\""));
    }

    [Fact]
    public void Keep_change_each_and_sort_by()
    {
        Assert.Equal("list 3 4\nlist 2 4 6 8\nlist \"a\" \"ccc\" \"bb\"\nlist \"a\" \"bb\" \"ccc\"", O.Out(Helpers + "\n" + """
            me can length t
                give size of t
            me has nums list 1 2 3 4
            say me keep nums (action is_big)
            say me change_each nums (action double)
            me has words list "a" "ccc" "bb"
            say words
            say me sort_by words (action length)
            """));
    }

    [Fact]
    public void Lists_of_actions()
    {
        Assert.Equal("6\n9", O.Out(Helpers + "\n" + """
            me can triple x
                give x * 3
            each f in list (action double) (action triple)
                say me call f 3
            """));
    }

    [Fact]
    public void Actions_compare_by_name()
    {
        Assert.Equal("yes\nno", O.Out(Helpers + "\nsay (action double) is (action double)\nsay (action double) is (action add)"));
    }

    [Theory]
    [InlineData("say action dubble", "me no know action dubble. you mean double?")]
    [InlineData("say me call 5", "need an action here (like: action double), but got number 5.")]
    [InlineData("say me call (action double)", "action double want 1 thing but got 0 things.")]
    [InlineData("say me call (action add) 1 2 3", "action add want 2 things but got 3 things.")]
    [InlineData("say me keep (list 1) (action double)", "keep need an action that gives yes or no.")]
    [InlineData("say action", "action which? like: action double")]
    public void Action_value_mistakes(string line, string message)
    {
        Assert.Contains(message, O.Fails(Helpers + "\n" + line).Message);
    }
}

public class ActionToCSharpTests
{
    const string Helpers = """
        me can is_even n
            give n % 2 is 0
        me can backwards a b
            give b - a
        """;

    [Fact]
    public void Ooga_action_becomes_a_csharp_delegate()
    {
        Assert.Equal("2\nlist 1 3 5\nlist 5 3 1\n6", O.Out(Helpers + "\n" + """
            me has nums me csharp_new "System.Collections.Generic.List`1[System.Int32]"
            each n in list 1 2 3 4 5
                me csharp_call nums "Add" n
            say me csharp_call nums "RemoveAll" (action is_even)
            say me csharp_call nums "ToArray"
            me csharp_call nums "Sort" (action backwards)
            say me csharp_call nums "ToArray"
            say me csharp "System.Array" "Find" (list 5 6 7) (action is_even)
            """));
    }

    [Theory]
    [InlineData("me csharp \"System.Linq.Enumerable\" \"Distinct\" (list 1 1 2 3 3)", "list 1 2 3")]
    [InlineData("me csharp \"System.Linq.Enumerable\" \"Sum\" (list 1 2 3.5)", "6.5")]
    [InlineData("me csharp \"System.Linq.Enumerable\" \"Where\" (list 1 2 3 4) (action is_even)", "list 2 4")]
    [InlineData("me csharp \"System.Linq.Enumerable\" \"Range\" 1 3", "list 1 2 3")]
    [InlineData("me csharp \"System.Linq.Enumerable\" \"Max\" (list \"b\" \"c\" \"a\")", "c")]
    public void Generic_csharp_methods_and_linq(string call, string expected)
    {
        Assert.Equal(expected, O.Out(Helpers + "\nsay " + call));
    }

    [Fact]
    public void Wrong_answer_from_action_given_to_csharp_is_a_problem()
    {
        var e = O.Fails(Helpers + "\nsay me csharp \"System.Array\" \"Find\" (list 1 2) (action backwards)");
        Assert.Contains("action backwards want 2 things but got 1 thing.", e.Message);
    }

    [Fact]
    public void Me_die_inside_action_called_by_csharp_still_ends_the_program()
    {
        var run = O.Run("me can stop_here x\n    say \"bye\"\n    me die\nsay me csharp \"System.Array\" \"Find\" (list 1) (action stop_here)\nsay \"never\"");
        Assert.Equal((RunEnd.Died, "bye"), (run.End, run.Output));
    }
}
