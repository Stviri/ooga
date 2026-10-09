using Ooga;

namespace Ooga.Tests;

// Programs that never end must not hang the tests (or a future game engine).
public class SafetyTests
{
    [Theory]
    [InlineData("repeat while yes\n    say 1")]
    [InlineData("me has x 0\nrepeat while x is small 1\n    x is 0")]
    [InlineData("repeat while yes\n    skip")]
    [InlineData("repeat while yes\n    if no\n        say 1")]
    public void Endless_loop_stops_at_step_limit(string source)
    {
        var host = new FakeHost();
        var e = Assert.Throws<OogaError>(() =>
            O.Bounded(_ => OogaRunner.Run(source, host, new RunOptions { MaxSteps = 10_000 })));
        Assert.Contains("ooga tired", e.Message);
    }

    [Fact]
    public void Endless_loop_can_be_cancelled_from_outside()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(200));
        Assert.Throws<OperationCanceledException>(() =>
            O.Bounded(_ => OogaRunner.Run("repeat while yes\n    skip", new FakeHost(), new RunOptions { Cancel = cts.Token })));
    }

    [Fact]
    public void Very_deep_actions_do_not_crash_the_engine()
    {
        var host = new FakeHost();
        O.Bounded(_ => OogaRunner.Run(
            "me can sum n\n    if n is 0\n        give 0\n    give 1 + (me sum (n - 1))\nsay me sum 20000",
            host, new RunOptions { MaxDepth = 20_001, MaxSteps = 1_000_000 }));
        Assert.Equal(new[] { "20000" }, host.Said);
    }

    [Fact]
    public void Big_but_finite_loops_finish()
    {
        Assert.Equal("100000", O.Out("me has n 0\nrepeat 100000\n    n gain 1\nsay n"));
    }
}
