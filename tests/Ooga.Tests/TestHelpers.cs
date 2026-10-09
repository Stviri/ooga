using Ooga;

namespace Ooga.Tests;

// A pretend outside world: answers come from a list, "say" lines are collected, "wait" does not sleep.
public class FakeHost : IOogaHost
{
    readonly Queue<string> answers;
    public readonly List<string> Said = new();
    public readonly List<string> Prompts = new();
    public readonly List<double> Waits = new();

    public FakeHost(params string[] answers) { this.answers = new Queue<string>(answers); }

    public void Say(string text) => Said.Add(text);

    public string Ask(string prompt)
    {
        Prompts.Add(prompt);
        return answers.Count > 0 ? answers.Dequeue() : null;
    }

    public void Wait(double seconds) => Waits.Add(seconds);

    public string Output => string.Join("\n", Said);
}

public record RunResult(string Output, RunEnd End, FakeHost Host);

public static class O
{
    // No single test program may run longer than this (wall clock) or take more steps than MaxSteps.
    public static readonly TimeSpan TimeLimit = TimeSpan.FromSeconds(10);
    public const long MaxSteps = 2_000_000;

    public static RunResult Run(string source, params string[] answers)
    {
        var host = new FakeHost(answers);
        var end = Bounded(cancel => OogaRunner.Run(source, host,
            new RunOptions { Seed = 1, MaxSteps = MaxSteps, Cancel = cancel }));
        return new RunResult(host.Output, end, host);
    }

    // Shortcut: run and return what was said.
    public static string Out(string source, params string[] answers) => Run(source, answers).Output;

    // Run a program that must fail, and return the ooga error.
    public static OogaError Fails(string source, params string[] answers)
    {
        var host = new FakeHost(answers);
        try
        {
            Bounded(cancel => OogaRunner.Run(source, host,
                new RunOptions { Seed = 1, MaxSteps = MaxSteps, Cancel = cancel }));
        }
        catch (OogaError e)
        {
            return e;
        }
        throw new Xunit.Sdk.XunitException("expected an ooga error, but the program ran fine. output:\n" + host.Output);
    }

    // Runs work with a hard time limit. A hanging program fails the test instead of hanging it.
    public static T Bounded<T>(Func<CancellationToken, T> work)
    {
        using var cts = new CancellationTokenSource();
        var task = Task.Run(() => work(cts.Token));
        if (Task.WaitAny(new Task[] { task }, TimeLimit) < 0)
        {
            cts.Cancel();
            throw new Xunit.Sdk.XunitException($"ooga program did not finish within {TimeLimit.TotalSeconds} seconds.");
        }
        return task.GetAwaiter().GetResult();
    }

    public static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "Ooga.sln")))
            dir = dir.Parent;
        return dir?.FullName ?? throw new InvalidOperationException("could not find the repository root (Ooga.sln)");
    }

    public static string Lines(params string[] lines) => string.Join("\n", lines);
}
