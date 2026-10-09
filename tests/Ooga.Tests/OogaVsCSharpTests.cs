using Ooga.Cli;

namespace Ooga.Tests;

// OOGA_VS_CSHARP.md promises that each Ooga program and its C# twin do exactly the same thing.
// These tests run the C# twin and compare what it shows with the same expected file the Ooga side is checked
// against (ScriptCaseTests checks the Ooga side), and check that the doc shows exactly the code in the files.
[CollectionDefinition("console", DisableParallelization = true)]
public class ConsoleCollection { }

[Collection("console")]
public class OogaVsCSharpTests
{
    static string Folder => Path.Combine(O.RepoRoot(), "docs", "ooga-vs-csharp");

    // pair name -> the C# class whose Main is the twin
    public static TheoryData<string, Type> Pairs => new()
    {
        { "01_say", typeof(Say) },
        { "02_things", typeof(Things) },
        { "03_checks", typeof(Checks) },
        { "04_loops", typeof(Loops) },
        { "05_actions", typeof(Actions) },
        { "06_lists", typeof(Lists) },
        { "07_boxes", typeof(Boxes) },
        { "08_problems", typeof(Problems) },
        { "09_ask", typeof(Asking) },
        { "10_fizzbuzz", typeof(FizzBuzz) },
    };

    static string Clean(string text) => text.Replace("\r\n", "\n").TrimEnd('\n', ' ');

    [Theory]
    [MemberData(nameof(Pairs))]
    public void CSharp_twin_shows_exactly_what_the_ooga_program_shows(string pair, Type twin)
    {
        string expected = File.ReadAllText(Path.Combine(Folder, "expected", pair + ".out"));
        string inFile = Path.Combine(Folder, "expected", pair + ".in");
        string typed = File.Exists(inFile) ? File.ReadAllText(inFile) : "";

        var oldOut = Console.Out;
        var oldIn = Console.In;
        var shown = new StringWriter { NewLine = "\n" };
        try
        {
            Console.SetOut(shown);
            Console.SetIn(new StringReader(typed));
            twin.GetMethod("Main")!.Invoke(null, null);
        }
        finally
        {
            Console.SetOut(oldOut);
            Console.SetIn(oldIn);
        }
        Assert.Equal(Clean(expected), Clean(shown.ToString()));

        // ...and the Ooga side, against the same file.
        var ooga = TestRunner.RunCase(Path.Combine(Folder, pair + ".ooga"));
        Assert.True(ooga.Passed, $"{pair}.ooga: {ooga.Problem}");
    }

    [Fact]
    public void Every_pair_has_both_languages_and_an_expected_output()
    {
        var oogaFiles = Directory.GetFiles(Folder, "*.ooga").Select(Path.GetFileNameWithoutExtension).Order().ToList();
        var csFiles = Directory.GetFiles(Folder, "*.cs").Select(Path.GetFileNameWithoutExtension).Order().ToList();
        Assert.Equal(oogaFiles, csFiles);
        Assert.Equal(oogaFiles, Pairs.Select(row => (string)row[0]).Order().ToList());
        foreach (var name in oogaFiles)
            Assert.True(File.Exists(Path.Combine(Folder, "expected", name + ".out")), name);
    }

    [Fact]
    public void The_doc_shows_exactly_the_tested_code_and_output()
    {
        string doc = File.ReadAllText(Path.Combine(O.RepoRoot(), "OOGA_VS_CSHARP.md")).Replace("\r\n", "\n");
        foreach (var file in Directory.GetFiles(Folder, "*.ooga").Concat(Directory.GetFiles(Folder, "*.cs"))
                     .Concat(Directory.GetFiles(Path.Combine(Folder, "expected"), "*.out")))
        {
            string code = Clean(File.ReadAllText(file));
            Assert.True(doc.Contains("\n" + code + "\n```"), $"OOGA_VS_CSHARP.md does not show {Path.GetFileName(file)} exactly");
        }
    }
}
