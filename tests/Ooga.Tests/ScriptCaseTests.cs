using Ooga.Cli;

namespace Ooga.Tests;

// Every script in examples/ and tests/cases/ is a test: it must show exactly what its expected/NAME.out says.
// The same check runs on any computer with:  ooga --test examples tests/cases
public class ScriptCaseTests
{
    public static TheoryData<string> Scripts()
    {
        var data = new TheoryData<string>();
        foreach (var folder in new[] { "examples", Path.Combine("tests", "cases"), Path.Combine("docs", "ooga-vs-csharp") })
            foreach (var script in TestRunner.Scripts(Path.Combine(O.RepoRoot(), folder)))
                data.Add(Path.GetRelativePath(O.RepoRoot(), script).Replace('\\', '/'));
        return data;
    }

    [Theory]
    [MemberData(nameof(Scripts))]
    public void Script_shows_what_it_should(string script)
    {
        var result = TestRunner.RunCase(Path.Combine(O.RepoRoot(), script));
        Assert.True(result.Passed, $"{script}: {result.Problem}\n--- got ---\n{result.Actual}");
    }

    [Fact]
    public void Every_example_has_an_expected_output()
    {
        foreach (var folder in new[] { "examples", Path.Combine("tests", "cases") })
            foreach (var script in TestRunner.Scripts(Path.Combine(O.RepoRoot(), folder)))
                Assert.True(File.Exists(TestRunner.ExpectedFile(script, ".out")), $"missing expected output for {script}");
    }

    [Fact]
    public void There_are_cases_for_each_kind()
    {
        var names = TestRunner.Scripts(Path.Combine(O.RepoRoot(), "tests", "cases")).Select(Path.GetFileName).ToList();
        foreach (var kind in new[] { "nested_", "recursion_", "use_", "fail_" })
            Assert.True(names.Count(n => n.StartsWith(kind)) >= 3, $"want at least 3 {kind} cases");
    }
}

// The checker of checks: it must say WRONG when a script is wrong.
public class TestRunnerTests : IDisposable
{
    readonly string folder = Path.Combine(Path.GetTempPath(), "ooga-runner-test-" + Guid.NewGuid().ToString("N"));

    public TestRunnerTests() => Directory.CreateDirectory(Path.Combine(folder, "expected"));
    public void Dispose() => Directory.Delete(folder, recursive: true);

    string Case(string name, string script, string expected = null, string typed = null)
    {
        string path = Path.Combine(folder, name + ".ooga");
        File.WriteAllText(path, script);
        if (expected != null) File.WriteAllText(Path.Combine(folder, "expected", name + ".out"), expected);
        if (typed != null) File.WriteAllText(Path.Combine(folder, "expected", name + ".in"), typed);
        return path;
    }

    [Fact]
    public void Right_output_passes_even_with_windows_line_endings()
    {
        Assert.True(TestRunner.RunCase(Case("a", "say 1\nsay 2", "1\r\n2\r\n")).Passed);
    }

    [Fact]
    public void Typed_answers_come_from_the_in_file()
    {
        Assert.True(TestRunner.RunCase(Case("a", "me has n ask \"n?\"\nsay n * 2", "n? 42\n", "21\n")).Passed);
    }

    [Fact]
    public void Wrong_output_fails_and_says_where()
    {
        var r = TestRunner.RunCase(Case("a", "say 1\nsay 3", "1\n2\n"));
        Assert.False(r.Passed);
        Assert.Equal("line 2 of output is different.\n         want: 2\n         got:  3", r.Problem);
    }

    [Fact]
    public void Missing_expected_output_fails()
    {
        var r = TestRunner.RunCase(Case("a", "say 1"));
        Assert.False(r.Passed);
        Assert.StartsWith("no expected output", r.Problem);
    }

    [Fact]
    public void Expected_problem_that_does_not_happen_fails()
    {
        var r = TestRunner.RunCase(Case("a", "say 1", "\nooga booga! problem in a.ooga line 1, column 1:\n"));
        Assert.False(r.Passed);
    }

    [Fact]
    public void Problem_text_said_on_purpose_still_needs_a_real_problem()
    {
        var r = TestRunner.RunCase(Case("a", "say \"ooga booga! problem in nowhere\"", "ooga booga! problem in nowhere\n"));
        Assert.False(r.Passed);
        Assert.Equal("ended with code 0, but should end with 1", r.Problem);
    }

    [Fact]
    public void Never_ending_script_fails_instead_of_hanging()
    {
        var r = O.Bounded(_ => TestRunner.RunCase(Case("a", "repeat while yes\n    skip", "")));
        Assert.False(r.Passed);
        Assert.Contains("ooga tired", r.Actual);
    }

    [Fact]
    public void Random_is_the_same_every_run()
    {
        string path = Case("a", "say random 1 to 1000000", "x");
        Assert.Equal(TestRunner.RunCase(path).Actual, TestRunner.RunCase(path).Actual);
    }

    [Fact]
    public void Files_go_to_a_fresh_folder_every_run()
    {
        string path = Case("a", "say me file_exists \"f.txt\"\nme write_file \"f.txt\" 1", "no\n");
        Assert.True(TestRunner.RunCase(path).Passed);
        Assert.True(TestRunner.RunCase(path).Passed);
        Assert.False(File.Exists(Path.Combine(folder, "f.txt")));
    }

    [Fact]
    public void Command_makes_missing_expected_outputs_then_checks_them()
    {
        Case("a", "say \"made\"");
        var output = new StringWriter { NewLine = "\n" };
        Assert.Equal(CliApp.Ok, TestRunner.Main(new[] { folder, "--make-expected" }, output));
        Assert.Equal("made\n", File.ReadAllText(Path.Combine(folder, "expected", "a.out")));

        output = new StringWriter { NewLine = "\n" };
        Assert.Equal(CliApp.Ok, TestRunner.Main(new[] { folder }, output));
        Assert.EndsWith("1 ok, 0 wrong.\nooga happy.\n", output.ToString());
    }

    [Fact]
    public void Command_reports_wrong_scripts_and_ends_with_code_1()
    {
        Case("good", "say 1", "1\n");
        Case("bad", "say 1", "2\n");
        var output = new StringWriter { NewLine = "\n" };
        Assert.Equal(CliApp.ScriptProblem, TestRunner.Main(new[] { folder }, output));
        Assert.Contains("WRONG  ", output.ToString());
        Assert.EndsWith("1 ok, 1 wrong.\n", output.ToString());
    }
}
