using Ooga;

namespace Ooga.Tests;

public class ListTests
{
    [Fact]
    public void Make_show_and_measure_lists()
    {
        Assert.Equal("list\nlist 1 2 3\n3\n0\nlist \"a\" (list 1 2) yes nothing (-1)", O.Out("""
            me has empty list
            me has bag list 1 2 3
            say empty
            say bag
            say size of bag
            say size of empty
            say list "a" (list 1 2) yes nothing (-1)
            """));
    }

    [Fact]
    public void Items_are_counted_from_1()
    {
        Assert.Equal("10\n30\n30", O.Out("me has bag list 10 20 30\nsay item 1 of bag\nsay item 3 of bag\nsay item (size of bag) of bag"));
    }

    [Fact]
    public void Change_an_item()
    {
        Assert.Equal("list 1 99 3\nlist 1 100 3", O.Out("me has bag list 1 2 3\nitem 2 of bag is 99\nsay bag\nitem 2 of bag gain 1\nsay bag"));
    }

    [Fact]
    public void Gain_adds_to_the_end_and_lose_takes_the_first_match()
    {
        Assert.Equal("list 1 2 1 3\nlist 2 1 3", O.Out("me has bag list 1 2\nbag gain 1\nbag gain 3\nsay bag\nbag lose 1\nsay bag"));
    }

    [Fact]
    public void Has_checks_what_is_inside()
    {
        Assert.Equal("yes\nno\nyes\nyes\nno", O.Out("""
            me has bag list 1 "two" (list 3)
            say bag has 1
            say bag has 2
            say bag has "two"
            say bag has (list 3)
            say not bag has 1
            """));
    }

    [Fact]
    public void Each_walks_through_a_list()
    {
        Assert.Equal("6", O.Out("me has total 0\neach n in list 1 2 3\n    total gain n\nsay total"));
    }

    [Fact]
    public void Each_with_stop_and_skip()
    {
        Assert.Equal("1\n3", O.Out("each n in list 1 2 3 4 5\n    if n is 2\n        skip\n    if n is 4\n        stop\n    say n"));
    }

    [Fact]
    public void Each_is_safe_when_the_list_changes_inside()
    {
        Assert.Equal("list 1 2 1 2", O.Out("me has bag list 1 2\neach n in bag\n    bag gain n\nsay bag"));
    }

    [Fact]
    public void Lists_are_shared_not_copied()
    {
        Assert.Equal("list 1 2\nlist 1", O.Out("""
            me has a list 1
            me has b a
            me has c me copy a
            b gain 2
            say a
            say c
            """));
    }

    [Fact]
    public void Action_can_change_a_list_it_was_given()
    {
        Assert.Equal("list \"rock\"", O.Out("me has bag list\nme can add_rock sack\n    sack gain \"rock\"\nme add_rock bag\nsay bag"));
    }

    [Fact]
    public void Lists_are_same_when_insides_are_same()
    {
        Assert.Equal("yes\nno\nno", O.Out("say list 1 2 is list 1 2\nsay list 1 2 is list 2 1\nsay list 1 is 1"));
    }

    [Fact]
    public void Plus_joins_two_lists()
    {
        Assert.Equal("list 1 2 3\nbag: list 1", O.Out("say (list 1) + (list 2 3)\nsay \"bag: \" + (list 1)"));
    }

    [Fact]
    public void List_of_lists()
    {
        Assert.Equal("6\nlist (list 1 2) (list 3 7)", O.Out("""
            me has grid list (list 1 2) (list 3 4)
            say (item 2 of item 1 of grid) * 3
            item 2 of item 2 of grid is 7
            say grid
            """));
    }

    [Fact]
    public void A_list_inside_itself_does_not_hang()
    {
        Assert.Contains("...", O.Out("me has bag list 1\nbag gain bag\nsay bag"));
    }
}

public class BoxTests
{
    [Fact]
    public void Make_a_box_and_read_parts()
    {
        Assert.Equal("box health 100 name \"grok\"\n100\ngrok", O.Out("""
            me has player box health 100 name "grok"
            say player
            say health of player
            say name of player
            """));
    }

    [Fact]
    public void Change_and_add_parts()
    {
        Assert.Equal("box health 70 speed 5\n71", O.Out("""
            me has player box health 100
            health of player lose 30
            speed of player is 5
            say player
            health of player gain 1
            say health of player
            """));
    }

    [Fact]
    public void Boxes_inside_boxes()
    {
        Assert.Equal("9\nbox pos (box x 9 y 2)", O.Out("""
            me has p box pos (box x 1 y 2)
            x of pos of p is 9
            say x of pos of p
            say p
            """));
    }

    [Fact]
    public void Box_as_a_lookup_with_item()
    {
        Assert.Equal("30\nyes\nno\nlist \"grok\" \"zug\"\n2", O.Out("""
            me has ages box
            item "grok" of ages is 30
            item "zug" of ages is 25
            say item "grok" of ages
            say ages has "grok"
            say ages has "ug"
            say me keys ages
            say size of ages
            """));
    }

    [Fact]
    public void Item_name_of_box_uses_the_name_as_position()
    {
        Assert.Equal("2\n20", O.Out("""
            me has k 2
            me has bag list 10 20
            me has p box pos 2
            say k
            say item (pos of p) of bag
            """));
    }

    [Fact]
    public void Each_walks_through_box_part_names()
    {
        Assert.Equal("a=1\nb=2", O.Out("me has b box a 1 b 2\neach k in b\n    say k + \"=\" + (item k of b)"));
    }

    [Fact]
    public void Action_changes_the_box_it_was_given()
    {
        Assert.Equal("ouch 70\n70", O.Out("""
            me has player box health 100
            me can hit who amount
                health of who lose amount
                say "ouch " + health of who
            me hit player 30
            say health of player
            """));
    }

    [Fact]
    public void Boxes_are_same_when_parts_are_same()
    {
        Assert.Equal("yes\nno", O.Out("say (box a 1 b 2) is (box a 1 b 2)\nsay (box a 1) is (box a 2)"));
    }

    [Fact]
    public void Take_removes_a_part()
    {
        Assert.Equal("1\nbox b 2", O.Out("me has b box a 1 b 2\nsay me take b \"a\"\nsay b"));
    }
}

public class TextTests
{
    [Fact]
    public void Letters_size_and_each()
    {
        Assert.Equal("5\ne\nh-e-y-", O.Out("""
            say size of "hello"
            say item 2 of "hello"
            me has out ""
            each ch in "hey"
                out gain ch + "-"
            say out
            """));
    }

    [Fact]
    public void Text_has_text()
    {
        Assert.Equal("yes\nno", O.Out("say \"caveman\" has \"man\"\nsay \"caveman\" has \"dog\""));
    }

    [Theory]
    [InlineData("\"abc\" is small \"abd\"", "yes")]
    [InlineData("\"b\" is big \"a\"", "yes")]
    [InlineData("\"a\" is big or same \"a\"", "yes")]
    [InlineData("\"Z\" is small \"a\"", "yes")]
    public void Texts_compare_in_letter_order(string check, string expected)
    {
        Assert.Equal(expected, O.Out("say " + check));
    }

    [Fact]
    public void Tab_escape()
    {
        Assert.Equal("a\tb", O.Out("say \"a\\tb\""));
    }
}

public class KindAndRemainderTests
{
    [Theory]
    [InlineData("5", "number")]
    [InlineData("\"hi\"", "text")]
    [InlineData("yes", "yes or no")]
    [InlineData("nothing", "nothing")]
    [InlineData("list 1", "list")]
    [InlineData("box a 1", "box")]
    [InlineData("me csharp_new \"System.Text.StringBuilder\"", "csharp")]
    public void Kind_of_names_the_kind(string value, string expected)
    {
        Assert.Equal(expected, O.Out("say kind of " + value));
    }

    [Theory]
    [InlineData("say 17 % 5", "2")]
    [InlineData("say 10 % 2", "0")]
    [InlineData("say -7 % 3", "2")]
    [InlineData("say 7.5 % 2", "1.5")]
    [InlineData("say 2 + 7 % 4", "5")]          // % happens with * and /
    public void Remainder(string src, string expected)
    {
        Assert.Equal(expected, O.Out(src));
    }

    [Fact]
    public void Guess_game_can_check_for_numbers_now()
    {
        Assert.Equal("that not a number\nok 5", O.Out("""
            repeat 2
                me has guess ask
                if kind of guess is not "number"
                    say "that not a number"
                else
                    say "ok " + guess
            """, "grok", "5"));
    }
}
