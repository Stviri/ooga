using Ooga;

namespace Ooga.Cli;

// Connects ooga's "say", "ask" and "wait" to a text console.
public class ConsoleHost : IOogaHost
{
    readonly TextReader input;
    readonly TextWriter output;
    readonly bool realWait;

    public ConsoleHost(TextReader input, TextWriter output, bool realWait = true)
    {
        this.input = input;
        this.output = output;
        this.realWait = realWait;
    }

    public void Say(string text) => output.WriteLine(text);

    public string Ask(string prompt)
    {
        if (prompt != null) output.Write(prompt + " ");
        output.Flush();
        return input.ReadLine();
    }

    public void Wait(double seconds)
    {
        output.Flush();
        if (realWait) Thread.Sleep(TimeSpan.FromSeconds(seconds));
    }
}
