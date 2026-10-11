using Ooga;

namespace Ooga.Tests;

// Whatever goes wrong inside C# must become an ooga problem that try / oops can catch,
// never "ooga engine broke". (Found when a web download failed inside an async C# method.)
public class BridgeErrorTests
{
    [Fact]
    public void Failing_async_csharp_method_is_a_catchable_problem()
    {
        Assert.Equal("caught: C# say problem: no internet", O.Out("""
            me has why_not me csharp_new "System.Exception" "no internet"
            try
                me csharp "System.Threading.Tasks.Task" "FromException" why_not
            oops why
                say "caught: " + why
            """));
    }

    [Fact]
    public void Failing_async_csharp_method_without_try_is_a_normal_ooga_error()
    {
        var e = O.Fails("me has x me csharp_new \"System.Exception\" \"no internet\"\nsay me csharp \"System.Threading.Tasks.Task\" \"FromException\" x");
        Assert.Equal((2, 5), (e.Line, e.Col));
        Assert.Equal("C# say problem: no internet", e.Message);
    }

    [Fact]
    public void Problem_while_reading_a_csharp_sequence_is_catchable()
    {
        // Enumerable.Repeat with a negative count fails when called; Range past int.MaxValue fails too.
        Assert.StartsWith("caught: C# say problem:", O.Out("""
            try
                say me csharp "System.Linq.Enumerable" "Range" 1 (-5)
            oops why
                say "caught: " + why
            """));
    }

    [Fact]
    public void Missing_file_through_csharp_is_catchable()
    {
        Assert.StartsWith("caught: C# say problem:", O.Out("""
            try
                say me csharp "System.IO.File" "ReadAllText" "no_such_file_anywhere.txt"
            oops why
                say "caught: " + why
            """));
    }

    [Fact]
    public void Ooga_problem_inside_an_action_called_by_csharp_is_still_the_ooga_problem()
    {
        var e = O.Fails("me can bad n\n    give n / 0\nsay me csharp \"System.Linq.Enumerable\" \"Where\" (list 1 2) (action bad)");
        Assert.Equal("no can split by 0.", e.Message);
        Assert.Equal((2, 12), (e.Line, e.Col));
    }
}
