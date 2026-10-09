namespace Ooga;

// Everything ooga needs from the outside world.
//
// The language core never touches the console, files, or a game engine directly.
// The ooga command line gives it a console host. Tests give it a pretend host.
// A Godot (or other engine) adapter will later give it a host of its own.
public interface IOogaHost
{
    // "say": show one line.
    void Say(string text);

    // "ask": show the prompt (null when there is none) and return what the person typed.
    // Return null when no more answers can ever come (for example, the input was closed).
    string Ask(string prompt);

    // "wait": pause for this many seconds (never less than 0).
    void Wait(double seconds);
}

public class RunOptions
{
    // Same seed = same "random" numbers every run. Null = different every run.
    public int? Seed { get; init; }

    // Stop with an error after this many steps (lines run plus loop rounds). 0 = no limit.
    // Tests use it so a never-ending loop fails instead of hanging.
    public long MaxSteps { get; init; }

    // How deep actions may call other actions (or themselves) before ooga gives up.
    public int MaxDepth { get; init; } = 500;

    // Lets the caller stop a running program from outside.
    public CancellationToken Cancel { get; init; }
}

public enum RunEnd { Finished, Died }

// The simple way to use ooga: give it script text and a host, it does the rest.
public static class OogaRunner
{
    // Read and check a script without running it. Throws OogaError on mistakes.
    public static OogaProgram Compile(string source)
    {
        var program = Parser.Parse(Lexer.Run(source));
        Checker.Check(program);
        return program;
    }

    // Read, check and run a script. Throws OogaError on mistakes.
    // Runs on its own thread with a big stack, so deep ooga actions can not crash the engine.
    public static RunEnd Run(string source, IOogaHost host, RunOptions options = null)
    {
        options ??= new RunOptions();
        RunEnd end = RunEnd.Finished;
        Exception failure = null;

        var thread = new Thread(() =>
        {
            try
            {
                end = Interpreter.Run(Compile(source), host, options);
            }
            catch (Exception e)
            {
                failure = e;
            }
        }, 256 * 1024 * 1024);
        thread.Start();
        thread.Join();

        if (failure != null)
            System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();
        return end;
    }
}
