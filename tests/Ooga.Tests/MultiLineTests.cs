using Ooga;
using Ooga.Cli;

namespace Ooga.Tests;

// "list" or "box" at the end of a line, with its items or parts pushed in on the lines below.
public class MultiLineTests
{
    [Fact]
    public void List_items_on_their_own_lines()
    {
        Assert.Equal("sword, bow, dagger\n3", O.Out("""
            me has weapons list
                "sword"
                "bow"
                "dagger"
            say weapons
            say size of weapons
            """));
    }

    [Fact]
    public void Empty_lines_and_notes_between_items_are_fine()
    {
        Assert.Equal("a, b", O.Out("me has w list\n    \"a\"\n\n    # note\n\n    \"b\"\nsay w"));
    }

    [Fact]
    public void Items_can_be_any_value_even_math()
    {
        Assert.Equal("6, yes, nothing, -2, sword!, (1, 2)", O.Out("me has x 3\nme has w list\n    x * 2\n    yes\n    nothing\n    -2\n    \"sword\" + \"!\"\n    list 1 2\nsay w"));
    }

    [Fact]
    public void Box_part_values_can_be_math()
    {
        Assert.Equal("health: 60, armor: -5", O.Out("me has hero box\n    health 50 + 10\n    armor -5\nsay hero"));
    }

    [Fact]
    public void Box_parts_on_their_own_lines()
    {
        Assert.Equal("name: grok, health: 100\n100", O.Out("""
            me has hero box
                name "grok"
                health 100
            say hero
            say health of hero
            """));
    }

    [Fact]
    public void List_inside_a_box_and_boxes_inside_a_list()
    {
        Assert.Equal("name: grok, bag: (rock, stick)\n(name: grok), (name: zug), (name: ugg)", O.Out("""
            me has hero box
                name "grok"
                bag list
                    "rock"
                    "stick"
            say hero
            me has team list
                box
                    name "grok"
                box
                    name "zug"
                (box name "ugg")
            say team
            """));
    }

    [Fact]
    public void Works_with_is_and_say_and_inside_blocks()
    {
        Assert.Equal("1, 2\n3, 4\ndone", O.Out("""
            me has w list
            w is list
                1
                2
            say w
            if yes
                say list
                    3
                    4
            say "done"
            """));
    }

    [Fact]
    public void Block_at_the_very_end_of_the_file()
    {
        var run = O.Run("me has w list\n    \"a\"\n    \"b\"");
        Assert.Equal(RunEnd.Finished, run.End);
    }

    [Fact]
    public void List_with_nothing_under_it_is_still_empty()
    {
        Assert.Equal("(empty)\n(empty)", O.Out("me has w list\nme has b box\nsay w\nsay b"));
    }

    [Fact]
    public void Lines_after_the_block_line_numbers_stay_right()
    {
        var e = O.Fails("me has w list\n    1\n    2\nsay nope");
        Assert.Equal((4, 5), (e.Line, e.Col));
    }

    [Theory]
    [InlineData("me has w list\n    say \"x\"", 2, 5, "lines under list must be things for the list, like: \"sword\" or 5")]
    [InlineData("me has h box\n    \"grok\"", 2, 5, "lines under box must be parts, a name and a value, like: health 100")]
    [InlineData("me has h box\n    health", 2, 11, "box part health need a value, like: health 0")]
    [InlineData("me has h box\n    name \"a\"\n    name \"b\"", 3, 5, "box already has part name. each part only once.")]
    [InlineData("me has w list\n    \"a\"\n        \"b\"", 3, 1, "this line is pushed in more than the line above it")]
    [InlineData("me has w list\n  \"a\"", 2, 1, "this line pushed in 2 spaces. ooga want exactly 4")]
    [InlineData("me has h box\n    bag list\n        say 1", 3, 9, "lines under list must be things for the list")]
    [InlineData("me has n list\n    1 2 3", 2, 7, "one thing per line under list. put 2 on its own line.")]
    [InlineData("me has h box\n    name \"a\" health 1", 2, 14, "one part per line under box. put health on its own line.")]
    public void Mistakes_in_lines_under_list_or_box(string source, int line, int col, string message)
    {
        var e = O.Fails(source);
        Assert.Contains(message, e.Message);
        Assert.Equal((line, col), (e.Line, e.Col));
    }

    [Fact]
    public void Talk_mode_reads_lines_under_list()
    {
        var run = Cli.Run(new[] { "--talk" }, "me has w list", "    \"sword\"", "    \"bow\"", "", "say w", "bye");
        Assert.Contains("ooga> sword, bow\n", run.Out);
    }
}
