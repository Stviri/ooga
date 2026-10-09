using Ooga;

namespace Ooga.Tests;

// The tokenizer and parser build a real program shape. These tests look at that shape directly.
public class LexerTests
{
    [Fact]
    public void Splits_line_into_words_numbers_texts_and_symbols()
    {
        var tokens = Lexer.Run("me has hp 2.5 + \"hi \\\"you\\\"\"");
        Assert.Equal(new[] { Tok.Word, Tok.Word, Tok.Word, Tok.Number, Tok.Symbol, Tok.Text, Tok.Newline, Tok.End },
            tokens.Select(t => t.Kind));
        Assert.Equal(2.5, tokens[3].Number);
        Assert.Equal("hi \"you\"", tokens[5].Value);
    }

    [Fact]
    public void Remembers_line_and_column_of_every_token()
    {
        var tokens = Lexer.Run("say 1\n\n    # note\nsay  \"x\"");
        var x = tokens.Single(t => t.Kind == Tok.Text);
        Assert.Equal((4, 6), (x.Line, x.Col));
    }

    [Fact]
    public void Pushed_in_lines_make_indent_and_dedent()
    {
        var kinds = Lexer.Run("if yes\n    say 1\n        \nsay 2").Select(t => t.Kind).ToList();
        Assert.Equal(new[]
        {
            Tok.Word, Tok.Word, Tok.Newline,
            Tok.Indent, Tok.Word, Tok.Number, Tok.Newline,
            Tok.Dedent, Tok.Word, Tok.Number, Tok.Newline, Tok.End,
        }, kinds);
    }

    [Fact]
    public void Notes_are_ignored_even_after_code()
    {
        Assert.Equal("1", O.Out("say 1 # this is a note\n# whole line note"));
    }

    [Fact]
    public void Windows_line_endings_work()
    {
        Assert.Equal("1\n2", O.Out("if yes\r\n    say 1\r\nsay 2\r\n"));
    }

    [Fact]
    public void Hash_inside_text_is_not_a_note()
    {
        Assert.Equal("a # b", O.Out("say \"a # b\""));
    }
}

public class ParserTests
{
    static OogaProgram Parse(string src) => Parser.Parse(Lexer.Run(src));

    [Fact]
    public void Times_binds_tighter_than_plus()
    {
        var say = (SayStmt)Parse("say 1 + 2 * 3").Body[0];
        var plus = Assert.IsType<MathExpr>(say.Value);
        Assert.Equal("+", plus.Op);
        Assert.Equal("*", Assert.IsType<MathExpr>(plus.Right).Op);
    }

    [Fact]
    public void Check_is_built_from_whole_math_on_both_sides()
    {
        var say = (SayStmt)Parse("say 1 + 1 is big or same 2 * 1").Body[0];
        var check = Assert.IsType<CompareExpr>(say.Value);
        Assert.Equal(CompareOp.BigOrSame, check.Op);
        Assert.IsType<MathExpr>(check.Left);
        Assert.IsType<MathExpr>(check.Right);
    }

    [Fact]
    public void And_binds_tighter_than_or()
    {
        var say = (SayStmt)Parse("say yes or no and no").Body[0];
        var or = Assert.IsType<LogicExpr>(say.Value);
        Assert.Equal("or", or.Op);
        Assert.Equal("and", Assert.IsType<LogicExpr>(or.Right).Op);
    }

    [Fact]
    public void If_else_if_else_is_one_statement_with_branches()
    {
        var program = Parse(O.Lines(
            "if 1 is 2",
            "    say 1",
            "else if 1 is 1",
            "    say 2",
            "else",
            "    say 3",
            "say 4"));
        Assert.Equal(2, program.Body.Count);
        var ifs = Assert.IsType<IfStmt>(program.Body[0]);
        Assert.Equal(2, ifs.Branches.Count);
        Assert.Single(ifs.Else);
    }

    [Fact]
    public void Action_has_name_params_and_body()
    {
        var can = Assert.IsType<CanStmt>(Parse("me can add a b\n    give a + b").Body[0]);
        Assert.Equal("add", can.Name);
        Assert.Equal(new[] { "a", "b" }, can.Params);
        Assert.IsType<GiveStmt>(Assert.Single(can.Body));
    }

    [Fact]
    public void Action_call_takes_one_value_per_thing()
    {
        var say = (SayStmt)Parse("say me add 1 (2 + 3) + 4").Body[0];
        var plus = Assert.IsType<MathExpr>(say.Value);
        var call = Assert.IsType<CallExpr>(plus.Left);
        Assert.Equal(2, call.Args.Count);
    }

    [Fact]
    public void Statements_remember_where_they_start()
    {
        var program = Parse("say 1\nif yes\n    me has x 5");
        var has = ((IfStmt)program.Body[1]).Branches[0].Body[0];
        Assert.Equal((3, 5), (has.Line, has.Col));
    }

    [Fact]
    public void Keywords_inside_names_are_fine()
    {
        // "sayer", "ifs", "isbig" contain ooga words but are ordinary names.
        Assert.Equal("6", O.Out("me has sayer 1\nme has ifs 2\nme has isbig 3\nsay sayer + ifs + isbig"));
    }
}
