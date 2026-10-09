using System.Text.RegularExpressions;
using Ooga.Cli;

namespace Ooga.Tests;

public record CliRun(int Exit, string Out, string Err);

public static class Cli
{
    public static CliRun Run(string[] args, params string[] answers)
    {
        var input = new StringReader(string.Join("\n", answers) + (answers.Length > 0 ? "\n" : ""));
        var output = new StringWriter { NewLine = "\n" };
        var error = new StringWriter { NewLine = "\n" };
        int exit = O.Bounded(_ => CliApp.Run(args, input, output, error, color: false, realWait: false));
        return new CliRun(exit, output.ToString(), error.ToString());
    }

    public static CliRun Example(string name, params string[] answers) =>
        Run(new[] { Path.Combine(O.RepoRoot(), "examples", name) }, answers);
}

// Every example in examples/ runs through the real "ooga" command and gives the output the quickstart promises.
public class ExampleTests
{
    [Fact]
    public void Every_example_is_covered_by_a_test()
    {
        var files = Directory.GetFiles(Path.Combine(O.RepoRoot(), "examples"), "*.ooga").Select(Path.GetFileName).Order();
        Assert.Equal(new[]
        {
            "01_hello.ooga", "02_calculator.ooga", "03_counting.ooga", "04_guess_number.ooga",
            "05_actions.ooga", "06_player.ooga", "07_mistakes.ooga", "08_shop.ooga", "09_fizzbuzz.ooga",
            "10_lists.ooga", "11_boxes.ooga", "12_try_and_files.ooga", "13_csharp.ooga",
            "14_action_values.ooga", "15_use.ooga",
        }, files);
    }

    [Fact]
    public void Hello()
    {
        var run = Cli.Example("01_hello.ooga", "Grok");
        Assert.Equal((0, ""), (run.Exit, run.Err));
        Assert.Equal("ooga booga\nwhat your name? hello Grok, welcome to cave.\n", run.Out);
    }

    [Theory]
    [InlineData("6", "*", "7", "42")]
    [InlineData("7", "/", "2", "3.5")]
    [InlineData("1", "+", "2", "3")]
    [InlineData("1", "-", "2", "-1")]
    [InlineData("1", "/", "0", "no can split by 0, silly")]
    [InlineData("1", "%", "0", "ooga no know that sign")]
    public void Calculator(string a, string sign, string b, string answer)
    {
        var run = Cli.Example("02_calculator.ooga", a, sign, b);
        Assert.Equal(0, run.Exit);
        Assert.Equal($"first number? what to do? (+ - * /) second number? {answer}\n", run.Out);
    }

    [Fact]
    public void Counting()
    {
        var run = Cli.Example("03_counting.ooga");
        Assert.Equal(0, run.Exit);
        Assert.Equal(string.Join("\n",
            "repeat 3 times:", "ooga", "ooga", "ooga",
            "count up:", "1", "2", "3", "4", "5",
            "rocket launch:", "3", "2", "1", "boom!",
            "stop early:", "1", "2", "3",
            "skip number 2:", "1", "3") + "\n", run.Out);
    }

    [Fact]
    public void Guess_number_ends_when_guessed()
    {
        // Guessing 1, 2, 3 ... always finds the secret, and the number of tries equals the secret.
        var run = Cli.Example("04_guess_number.ooga", "1", "2", "3", "4", "5", "6", "7", "8", "9", "10");
        Assert.Equal(0, run.Exit);
        var last = run.Out.TrimEnd('\n').Split('\n').Last();
        var m = Regex.Match(last, @"yes! it was (\d+)\. you took (\d+) tries\.$");
        Assert.True(m.Success, run.Out);
        Assert.Equal(m.Groups[1].Value, m.Groups[2].Value);
    }

    [Fact]
    public void Guess_number_says_too_small_and_too_big()
    {
        var run = Cli.Example("04_guess_number.ooga", "0", "11", "1", "2", "3", "4", "5", "6", "7", "8", "9", "10");
        Assert.Contains("too small", run.Out);
        Assert.Contains("too big", run.Out);
    }

    [Fact]
    public void Guess_number_handles_words_instead_of_numbers()
    {
        var run = Cli.Example("04_guess_number.ooga", "grok", "1", "2", "3", "4", "5", "6", "7", "8", "9", "10");
        Assert.Equal(0, run.Exit);
        Assert.Contains("your guess? that not a number. try again.\n", run.Out);
        var m = System.Text.RegularExpressions.Regex.Match(run.Out, @"yes! it was (\d+)\. you took (\d+) tries\.");
        Assert.Equal(m.Groups[1].Value, m.Groups[2].Value);   // the word did not count as a try
    }

    [Fact]
    public void Lists()
    {
        var run = Cli.Example("10_lists.ooga");
        Assert.Equal((0, ""), (run.Exit, run.Err));
        Assert.Equal(string.Join("\n",
            "scores: list 40 75 12", "how many: 3", "first: 40", "now: list 40 75 99", "total: 214", "best: 99",
            "sorted: list 40 75 99", "someone got 99!", "fixed: list 41 75 99", "o", "o", "g", "a") + "\n", run.Out);
    }

    [Fact]
    public void Boxes()
    {
        var run = Cli.Example("11_boxes.ooga");
        Assert.Equal((0, ""), (run.Exit, run.Err));
        Assert.Equal(string.Join("\n",
            "box name \"grok\" health 100 club no", "grok get hit. health now 70", "grok pick up club", "has club? yes",
            "grok get hit. health now 10", "zug get hit. health now -10", "zug is down!",
            "list (box name \"grok\" health 10 club yes) (box name \"zug\" health (-10) club no)") + "\n", run.Out);
    }

    [Fact]
    public void Try_and_files()
    {
        File.Delete("cave_save.txt");
        try
        {
            var first = Cli.Example("12_try_and_files.ooga");
            Assert.Equal((0, ""), (first.Exit, first.Err));
            Assert.StartsWith("no save yet (read_file no find file: cave_save.txt)\nold best: 0\n", first.Out);
            Assert.Contains("new best!", first.Out);
            Assert.EndsWith("caught: age no can be below 0, but got -5\n", first.Out);

            string saved = File.ReadAllText("cave_save.txt");
            var second = Cli.Example("12_try_and_files.ooga");
            Assert.StartsWith($"old best: {saved}\n", second.Out);
        }
        finally
        {
            File.Delete("cave_save.txt");
        }
    }

    [Fact]
    public void CSharp()
    {
        var run = Cli.Example("13_csharp.ooga");
        Assert.Equal((0, ""), (run.Exit, run.Err));
        Assert.Equal(string.Join("\n", "12", "3.14", "ooga booga", "7", "CAVEMAN", "list \"rock\" \"stick\" \"fire\"", "list 20 30") + "\n", run.Out);
    }

    [Fact]
    public void Action_values()
    {
        var run = Cli.Example("14_action_values.ooga");
        Assert.Equal((0, ""), (run.Exit, run.Err));
        Assert.Equal(string.Join("\n", "list 1 2 3 4 5 6", "list 2 4 6 8 10 12", "list 2 4 6", "list \"ox\" \"cat\" \"mammoth\"", "42") + "\n", run.Out);
    }

    [Fact]
    public void Use_another_file()
    {
        var run = Cli.Example("15_use.ooga");
        Assert.Equal((0, "WELCOME TO BIG CAVE!\n*****\n"), (run.Exit, run.Out));
    }

    [Fact]
    public void Actions()
    {
        var run = Cli.Example("05_actions.ooga");
        Assert.Equal((0, "ooga, grok!\nooga, zug!\n42\n5\n11\n"), (run.Exit, run.Out));
    }

    [Fact]
    public void Player()
    {
        var run = Cli.Example("06_player.ooga");
        Assert.Equal(0, run.Exit);
        Assert.Equal("ouch! health now 70\nouch! health now 40\nouch! health now 10\nouch! health now -20\nplayer die\n", run.Out);
    }

    [Fact]
    public void Mistakes_example_shows_a_helpful_error()
    {
        var run = Cli.Example("07_mistakes.ooga");
        Assert.Equal(1, run.Exit);
        Assert.Equal("", run.Out);
        Assert.EndsWith("""
            07_mistakes.ooga line 7, column 5:
                say heath
                    ^
            no thing called "heath". you mean "health"?

            """.Replace("\r\n", "\n"), run.Err);
    }

    [Fact]
    public void Shop()
    {
        var run = Cli.Example("08_shop.ooga", "2", "3", "0");
        Assert.Equal(0, run.Exit);
        Assert.Equal(string.Join("\n",
            "welcome to cave shop. you have 20 shells.",
            "1 = club (5 shells), 2 = rock (2 shells), 0 = leave",
            "buy what? you buy rock. 18 shells left.",
            "buy what? ooga no sell that.",
            "buy what? bye! you leave with 18 shells and 1 things.") + "\n", run.Out);
    }

    [Fact]
    public void Shop_says_when_shells_are_too_few()
    {
        var run = Cli.Example("08_shop.ooga", "2", "1", "1", "1", "1", "0");
        Assert.Equal(0, run.Exit);
        Assert.Contains("buy what? you buy club. 3 shells left.\nbuy what? you no have enough shells.\n", run.Out);
        Assert.EndsWith("bye! you leave with 3 shells and 4 things.\n", run.Out);
    }

    [Fact]
    public void Shop_closes_when_shells_run_out()
    {
        var run = Cli.Example("08_shop.ooga", "1", "1", "1", "1");
        Assert.Equal(0, run.Exit);
        Assert.EndsWith("you buy club. 0 shells left.\nno more shells. shop close.\n", run.Out);
    }

    [Fact]
    public void FizzBuzz()
    {
        var run = Cli.Example("09_fizzbuzz.ooga");
        Assert.Equal(0, run.Exit);
        Assert.Equal("1\n2\nfizz\n4\nbuzz\nfizz\n7\n8\nfizz\nbuzz\n11\nfizz\n13\n14\nfizzbuzz\n", run.Out);
    }

    [Fact]
    public void Playground_runs()
    {
        var run = Cli.Run(new[] { Path.Combine(O.RepoRoot(), "my_scripts", "playground.ooga") });
        Assert.Equal((0, "ooga ready\n"), (run.Exit, run.Out));
    }
}

public class CliTests
{
    [Fact]
    public void No_file_shows_how_to_use()
    {
        var run = Cli.Run(Array.Empty<string>());
        Assert.Equal(CliApp.UsageProblem, run.Exit);
        Assert.Contains("use like this:  ooga hello.ooga", run.Out);
    }

    [Fact]
    public void Help_shows_how_to_use()
    {
        var run = Cli.Run(new[] { "--help" });
        Assert.Equal(CliApp.Ok, run.Exit);
        Assert.Contains("use like this", run.Out);
    }

    [Fact]
    public void Missing_file_is_reported()
    {
        var run = Cli.Run(new[] { "no_such_file.ooga" });
        Assert.Equal(CliApp.UsageProblem, run.Exit);
        Assert.Contains("ooga no find file: no_such_file.ooga", run.Err);
    }

    [Fact]
    public void Errors_go_to_error_stream_with_file_line_and_column()
    {
        string path = TempScript("say \"before\"\nsay 1 / 0\n");
        var run = Cli.Run(new[] { path });
        Assert.Equal(CliApp.ScriptProblem, run.Exit);
        Assert.Equal("before\n", run.Out);
        Assert.Contains($"ooga booga! problem in {path} line 2, column 7:", run.Err);
        Assert.Contains("no can split by 0.", run.Err);
    }

    [Fact]
    public void Me_die_is_a_normal_ending()
    {
        var run = Cli.Run(new[] { TempScript("say 1\nme die\nsay 2") });
        Assert.Equal((CliApp.Ok, "1\n"), (run.Exit, run.Out));
    }

    [Fact]
    public void Byte_order_mark_and_windows_line_endings_are_fine()
    {
        string path = Path.Combine(Path.GetTempPath(), $"ooga_test_{Guid.NewGuid():N}.ooga");
        File.WriteAllText(path, "﻿if yes\r\n    say \"ok\"\r\n", new System.Text.UTF8Encoding(true));
        var run = Cli.Run(new[] { path });
        Assert.Equal((CliApp.Ok, "ok\n"), (run.Exit, run.Out));
    }

    [Fact]
    public void Ask_after_input_ends_fails_instead_of_looping_forever()
    {
        var run = Cli.Run(new[] { TempScript("repeat while yes\n    me has x ask \"more?\"") }, "a", "b");
        Assert.Equal(CliApp.ScriptProblem, run.Exit);
        Assert.Contains("no more answers can come", run.Err);
    }

    static string TempScript(string source)
    {
        string path = Path.Combine(Path.GetTempPath(), $"ooga_test_{Guid.NewGuid():N}.ooga");
        File.WriteAllText(path, source);
        return path;
    }
}
