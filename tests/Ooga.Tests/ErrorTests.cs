using Ooga;

namespace Ooga.Tests;

// Wrong programs must fail with a caveman message pointing at the exact line and column.
public class ErrorTests
{
    public static TheoryData<string, int, int, string> Mistakes => new()
    {
        // ---- typos (with "you mean ...?" hints) ----
        { "me has health 100\nsay heath", 2, 5, "no thing called \"heath\". you mean \"health\"?" },
        { "me has health 100\nsay Health", 2, 5, "you mean \"health\"?" },
        { "say nobody", 1, 5, "no thing called \"nobody\". make it first with: me has nobody 0" },
        { "me can greet who\n    say who\nme greeet \"x\"", 3, 1, "me no know how to greeet. you mean greet?" },
        { "me jump", 1, 1, "me no know how to jump. teach me first with: me can jump" },
        { "sya \"hi\"", 1, 1, "ooga no know \"sya\". you mean say?" },
        { "Say \"hi\"", 1, 1, "you mean say?" },
        { "repaet 3\n    say 1", 1, 1, "you mean repeat?" },
        { "if 5 is bigg 3\n    say 1", 1, 9, "ooga no know \"bigg\". you mean big?" },
        { "if 5 is smal 3\n    say 1", 1, 9, "you mean small?" },
        { "me can greet who\n    say who\nsay greet", 3, 5, "\"greet\" is an action (line 1), not a thing. to use it, write: me greet" },

        // ---- shape of lines ----
        { "health = 5", 1, 8, "ooga no use =" },
        { "say \"hello", 1, 5, "text start here but never end" },
        { "say \"a\\qb\"", 1, 7, "ooga no know \\q" },
        { "say 5 % 2", 1, 7, "ooga no know this sign: %" },
        { "me has 2hit 5", 1, 8, "names no can start with number" },
        { "say (1 + 2", 1, 5, "this ( open but never close" },
        { "say", 1, 4, "say what?" },
        { "me has health", 1, 14, "me has health... but how much?" },
        { "me has say 5", 1, 8, "\"say\" is special ooga word. no can use it as a name" },
        { "me can if\n    say 1", 1, 8, "\"if\" is special ooga word" },
        { "health", 1, 1, "what to do with health?" },
        { "me", 1, 3, "me what?" },
        { "count i 1 to 3\n    say i", 1, 9, "count need \"from\" next" },
        { "count i from 1 3\n    say i", 1, 16, "count need \"to\" next" },
        { "say random 1 6", 1, 14, "random need \"to\"" },
        { "say 1 2", 1, 7, "ooga confused by 2 here. too many things on this line?" },
        { "me can double x\n    give x * 2\nsay me double me double 2", 3, 15, "wrap it in ( )" },
        { "me has x 1\nx is big 5", 2, 6, "this look like a check. checks go after if" },
        { "when me hit\n    say 1", 1, 1, "\"when\" is for game things" },
        { "else\n    say 1", 1, 1, "else here, but no if right above it." },

        // ---- blocks and pushing in ----
        { "if yes\nsay 1", 1, 1, "\"if\" need lines under it, pushed in 4 spaces." },
        { "repeat 3", 1, 1, "\"repeat\" need lines under it" },
        { "me can jump", 1, 1, "\"me can jump\" need lines under it" },
        { "say 1\n    say 2", 2, 1, "this line pushed in, but nothing above want lines under it" },
        { "if yes\n  say 1", 2, 1, "this line pushed in 2 spaces. ooga want exactly 4" },
        { "if yes\n\tsay 1", 2, 1, "found a tab key push here" },
        { "if yes\n    if yes\n        say 1\n  say 2", 4, 1, "this line push-in no match any line above" },
        { "if yes\n    me can jump\n        say 1", 2, 5, "\"me can\" must be at left edge" },

        // ---- rules about where words work ----
        { "stop", 1, 1, "stop only work inside repeat or count." },
        { "skip", 1, 1, "skip only work inside repeat or count." },
        { "give 5", 1, 1, "give only work inside \"me can\" action." },
        { "me can f\n    stop", 2, 5, "stop only work inside repeat or count." },
        { "me can f\n    say 1\nme can f\n    say 2", 3, 1, "me already know how to f (line 1)" },
        { "me can die\n    say 1", 1, 8, "\"die\" is special ooga word" },
        { "me has x 1\nme can show a\n    say a\nme show x + 2", 4, 11, "if this math is for an action, put ( ) around it" },
        { "if yes\n    sya \"hi\"", 2, 5, "you mean say?" },
        { "me can add a a\n    give a", 1, 1, "action add has \"a\" two times" },
        { "me has x 1\nme has x 2", 2, 1, "me already has x (line 1). to change it, write: x is ..." },
        { "me can add a b\n    give a + b\nme add 1", 3, 1, "add want 2 things but got 1 thing. (see line 1)" },
        { "me can one x\n    give x\nsay me one 1 2", 3, 5, "one want 1 thing but got 2 things" },

        // ---- problems found while running ----
        { "say 10 / (5 - 5)", 1, 8, "no can split by 0." },
        { "say \"a\" - 1", 1, 9, "no can do text \"a\" - number 1. math need numbers." },
        { "say yes * 2", 1, 9, "no can do yes * number 2" },
        { "if 5\n    say 1", 1, 4, "need yes or no here, but got number 5" },
        { "repeat while \"x\"\n    say 1", 1, 14, "need yes or no here" },
        { "say \"a\" is big 1", 1, 9, "no can check if text \"a\" is big or small than number 1. need numbers." },
        { "me has x 5\nx gain \"a\"", 2, 1, "x is number 5. no can gain text \"a\"" },
        { "me has x \"a\"\nx lose 1", 2, 1, "no can lose" },
        { "repeat 2.5\n    say 1", 1, 8, "repeat need whole number 0 or more, not 2.5" },
        { "repeat -1\n    say 1", 1, 8, "repeat need whole number 0 or more" },
        { "repeat \"x\"\n    say 1", 1, 8, "repeat need a number, but got text \"x\"" },
        { "count i from \"a\" to 3\n    say i", 1, 14, "count need a number" },
        { "wait -1", 1, 1, "no can wait less than 0 seconds" },
        { "say random 1.5 to 3", 1, 5, "random need whole numbers" },
        { "say -\"a\"", 1, 6, "- need a number, but got text \"a\"" },
        { "me has x 1\ncount i from 1 to 2000\n    x is x * 1000", 3, 12, "number too big for ooga" },
        { "me can forever n\n    give me forever n\nsay me forever 1", 2, 10, "me dizzy. forever called too many times" },
        { "me can down n\n    give me down n - 1\nsay me down 3", 2, 10, "like: me down (n - 1)" },
        { "me has a ask \"?\"", 1, 10, "ooga ask, but no more answers can come" },
    };

    [Theory]
    [MemberData(nameof(Mistakes))]
    public void Mistake_is_reported_at_the_right_spot(string source, int line, int col, string message)
    {
        var e = O.Fails(source);
        Assert.Contains(message, e.Message);
        Assert.Equal((line, col), (e.Line, e.Col));
    }

    [Fact]
    public void Checking_happens_before_running()
    {
        // The mistake is on the last line, so nothing at all is said.
        var host = new FakeHost();
        Assert.Throws<OogaError>(() => OogaRunner.Run("say 1\nsay 2\nsay nope", host));
        Assert.Empty(host.Said);
    }

    [Fact]
    public void Running_problem_keeps_what_was_already_said()
    {
        var host = new FakeHost();
        Assert.Throws<OogaError>(() => OogaRunner.Run("say 1\nsay 1 / 0", host));
        Assert.Equal(new[] { "1" }, host.Said);
    }

    [Fact]
    public void Report_shows_file_line_column_code_and_pointer()
    {
        string source = "me has health 100\nsay heath";
        var e = O.Fails(source);
        var report = e.Report("cave/hello.ooga", source);
        Assert.Equal("""
            ooga booga! problem in cave/hello.ooga line 2, column 5:
                say heath
                    ^
            no thing called "heath". you mean "health"?
            """.Replace("\r\n", "\n"), report.ToString());
    }

    [Fact]
    public void Short_names_do_not_get_silly_hints()
    {
        var e = O.Fails("me has hp 1\nsay x");
        Assert.DoesNotContain("you mean", e.Message);
    }
}
