using System.Net;
using System.Net.Sockets;
using System.Text;
using Ooga;

namespace Ooga.Tests;

// Folders, files, spreadsheets, JSON, the web and the clock, in ooga words.
public class FolderTests
{
    static FakeHost Cave()
    {
        var host = new FakeHost();
        host.Files["messy/cat.jpg"] = "meow";
        host.Files["messy/dog.jpg"] = "woof";
        host.Files["messy/notes.txt"] = "one\ntwo\n";
        return host;
    }

    [Fact]
    public void Sort_files_into_folders_by_ending()
    {
        var host = Cave();
        var run = O.Run("""
            each file in me files_in "messy"
                me has place me join_path "sorted" (me file_ending file)
                if not me folder_exists place
                    me make_folder place
                me move_file file place
            say me folders_in "sorted"
            say me files_in "sorted/jpg"
            say me files_in "messy"
            """, host);
        Assert.Equal("sorted/jpg, sorted/txt\nsorted/jpg/cat.jpg, sorted/jpg/dog.jpg\n(empty)", run.Output);
        Assert.Equal("meow", host.Files["sorted/jpg/cat.jpg"]);
    }

    [Fact]
    public void Paths_are_just_text()
    {
        Assert.Equal("a/b/c.txt\nc.txt\ntxt\na/b\n.", O.Out("""
            me has p me join_path "a" "b" "c.txt"
            say p
            say me file_name p
            say me file_ending p
            say me file_folder p
            say me file_folder "c.txt"
            """));
    }

    [Fact]
    public void Copy_delete_size_and_lines()
    {
        var host = Cave();
        var run = O.Run("""
            me copy_file "messy/notes.txt" "notes2.txt"
            say me read_lines "notes2.txt"
            say me file_size "notes2.txt"
            me write_lines "list.txt" (list "a" 2 yes)
            say me read_file "list.txt"
            me delete_file "notes2.txt"
            say me file_exists "notes2.txt"
            """, host);
        Assert.Equal("one, two\n8\na\n2\nyes\n\nno", run.Output);
    }

    [Theory]
    [InlineData("say me files_in \"nowhere\"", "files_in no find folder for: nowhere")]
    [InlineData("me move_file \"ghost.txt\" \"b.txt\"", "move_file no find file: ghost.txt")]
    [InlineData("me copy_file \"messy/cat.jpg\" \"messy/dog.jpg\"", "copy_file no can: there is already a file at messy/dog.jpg")]
    [InlineData("me delete_file \"ghost.txt\"", "delete_file no find file: ghost.txt")]
    [InlineData("me copy_file \"messy/cat.jpg\" \"no/such/place.jpg\"", "copy_file no find folder for:")]
    public void File_mistakes_are_catchable_problems(string line, string message)
    {
        var e = O.Fails(line, Cave());
        Assert.Contains(message, e.Message);
        Assert.Equal("caught", O.Run("try\n    " + line + "\noops\n    say \"caught\"", Cave()).Output);
    }
}

public class CsvTests
{
    [Fact]
    public void Read_a_spreadsheet_into_boxes()
    {
        var host = new FakeHost();
        host.Files["scores.csv"] = "name,score,city\r\ngrok,90,\"Cave, North\"\r\nzug,75,\"say \"\"hi\"\"\"\r\n\r\n";
        var run = O.Run("""
            me has rows me read_csv "scores.csv"
            say size of rows
            each row in rows
                say name of row + " | " + (score of row + 1) + " | " + city of row
            say kind of score of item 1 of rows
            """, host);
        Assert.Equal("2\ngrok | 91 | Cave, North\nzug | 76 | say \"hi\"\nnumber", run.Output);
    }

    [Fact]
    public void Semicolon_spreadsheets_and_short_rows()
    {
        Assert.Equal("a: 1, b: 2\na: 3, b: \nx, y, column 3", O.Out("""
            me has rows me from_csv "a;b\n1;2\n3"
            each row in rows
                say row
            say me keys (item 1 of (me from_csv "x,y\n1,2,3"))
            """));
    }

    [Fact]
    public void Write_a_spreadsheet_and_read_it_back()
    {
        var host = new FakeHost();
        var run = O.Run("""
            me has rows list
                box name "grok" note "likes rocks, sticks"
                box name "zug" note "say \"hi\"" age 9
            me write_csv "out.csv" rows
            say me read_file "out.csv"
            say (me read_csv "out.csv") is rows
            """, host);
        Assert.Equal("name,note,age\ngrok,\"likes rocks, sticks\",\nzug,\"say \"\"hi\"\"\",9\n\nno", run.Output);
        // "no": grok's missing age comes back as empty text, so the rows are not exactly the same
    }

    [Fact]
    public void List_of_lists_has_no_header()
    {
        Assert.Equal("1,2\na,\"b,c\"\n", O.Out("say me to_csv (list (list 1 2) (list \"a\" \"b,c\"))"));
    }

    [Theory]
    [InlineData("say me to_csv 5", "to_csv need a list of rows (boxes or lists), but got number 5.")]
    [InlineData("say me to_csv (list (box a 1) (list 2))", "to_csv need every row to be a box (named parts) or every row to be a list.")]
    [InlineData("say me from_csv \"a\\n\\\"oops\"", "from_csv: a \" in the spreadsheet starts quoted text but never ends it.")]
    public void Csv_mistakes(string line, string message)
    {
        Assert.Contains(message, O.Fails(line).Message);
    }
}

public class JsonTests
{
    [Fact]
    public void From_json_gives_boxes_and_lists()
    {
        Assert.Equal("grok\nclub, rock\n100\nyes\nnothing\nbox", O.Out("""
            me has hero me from_json "{\"name\": \"grok\", \"items\": [\"club\", \"rock\"], \"hp\": 100, \"ok\": true, \"pet\": null}"
            say name of hero
            say items of hero
            say hp of hero
            say ok of hero
            say pet of hero
            say kind of hero
            """));
    }

    [Fact]
    public void To_json_and_back()
    {
        Assert.Equal("{\"name\":\"zug\",\"tags\":[1,2.5,\"x\"],\"ok\":false,\"pet\":null}\nyes", O.Out("""
            me has zug box name "zug" tags (list 1 2.5 "x") ok no pet nothing
            say me to_json zug
            say (me from_json (me to_json zug)) is zug
            """));
    }

    [Theory]
    [InlineData("say me from_json \"{oops\"", "from_json: this is not good JSON")]
    [InlineData("say me to_json (action to_json)", "to_json no can turn action to_json into JSON.")]
    public void Json_mistakes(string line, string message)
    {
        Assert.Contains(message, O.Fails(line).Message);
    }
}

// A tiny real web server on this computer, so web words are tested for real (no internet needed).
public sealed class LocalWeb : IDisposable
{
    readonly HttpListener listener = new();
    public string Url { get; }
    public readonly List<string> Received = new();

    public LocalWeb()
    {
        var probe = new TcpListener(IPAddress.Loopback, 0);
        probe.Start();
        int port = ((IPEndPoint)probe.LocalEndpoint).Port;
        probe.Stop();
        Url = $"http://127.0.0.1:{port}/";
        listener.Prefixes.Add(Url);
        listener.Start();
        Task.Run(Serve);
    }

    async Task Serve()
    {
        while (listener.IsListening)
        {
            HttpListenerContext ctx;
            try { ctx = await listener.GetContextAsync(); } catch (Exception) { return; }
            string body = new StreamReader(ctx.Request.InputStream).ReadToEnd();
            lock (Received) Received.Add($"{ctx.Request.HttpMethod} {ctx.Request.Url!.AbsolutePath} {ctx.Request.ContentType} {body}");
            (int status, string answer) = ctx.Request.Url.AbsolutePath switch
            {
                "/hero.json" => (200, "{\"name\": \"grok\", \"hp\": 100}"),
                "/hello" => (200, "ooga from the web"),
                "/echo" => (200, "got: " + body),
                _ => (404, "no such page"),
            };
            ctx.Response.StatusCode = status;
            byte[] bytes = Encoding.UTF8.GetBytes(answer);
            ctx.Response.OutputStream.Write(bytes);
            ctx.Response.Close();
        }
    }

    public void Dispose() => listener.Close();
}

public class WebTests
{
    [Fact]
    public void Get_text_json_and_post()
    {
        using var web = new LocalWeb();
        var run = O.Run($"""
            say me web_get "{web.Url}hello"
            me has hero me web_get_json "{web.Url}hero.json"
            say name of hero + " has " + hp of hero + " hp"
            say me web_post "{web.Url}echo" (box score 5)
            say me web_post "{web.Url}echo" "plain text"
            """, new FakeHost());
        Assert.Equal("ooga from the web\ngrok has 100 hp\ngot: {\"score\":5}\ngot: plain text", run.Output);
        Assert.Contains(web.Received, r => r.StartsWith("POST /echo application/json"));
        Assert.Contains(web.Received, r => r.StartsWith("POST /echo text/plain"));
    }

    [Fact]
    public void Missing_page_and_bad_address_are_catchable_problems()
    {
        using var web = new LocalWeb();
        Assert.Equal($"web_get: the web said 404 (Not Found) for {web.Url}nope", O.Out($"""
            try
                me web_get "{web.Url}nope"
            oops why
                say why
            """));
        Assert.Contains("web_get need a web address starting with http:// or https://", O.Fails("say me web_get \"cave.com\"").Message);
        Assert.StartsWith("web_get no reach http://127.0.0.1:1/", O.Fails("say me web_get \"http://127.0.0.1:1/\"").Message);
    }
}

public class ClockTests
{
    [Fact]
    public void Now_is_a_box_with_the_date_and_time()
    {
        var before = DateTime.Now;
        string said = O.Out("me has t me now\nsay year of t\nsay me keys t\nsay weekday of t");
        var lines = said.Split('\n');
        Assert.Equal(before.Year.ToString(), lines[0]);
        Assert.Equal("year, month, day, hour, minute, second, weekday", lines[1]);
        Assert.Contains(lines[2], Enum.GetNames<DayOfWeek>());
    }
}

public class CSharpPartTests
{
    [Fact]
    public void Read_and_change_parts_of_csharp_things()
    {
        Assert.Equal("9\nooga\n2\n4", O.Out("""
            me has sb me csharp_new "System.Text.StringBuilder" "oogabooga"
            say length of sb
            length of sb is 4
            say sb
            me has names me csharp_new "System.Collections.Generic.List`1[System.String]"
            me csharp_call names "Add" "a"
            me csharp_call names "Add" "b"
            say size of names
            say LENGTH of sb
            """));
    }

    [Theory]
    [InlineData("me has sb me csharp_new \"System.Text.StringBuilder\"\nsay nope of sb", 2, 5, "StringBuilder no have part nope. it has: capacity, length, maxcapacity")]
    [InlineData("me has s me csharp_new \"System.Text.StringBuilder\"\nmaxcapacity of s is 5", 2, 1, "StringBuilder's maxcapacity can only be read, not changed.")]
    [InlineData("me has sb me csharp_new \"System.Text.StringBuilder\"\nlength of sb is \"x\"", 2, 1, "no can turn text \"x\" into C# Int32.")]
    public void Part_mistakes(string source, int line, int col, string message)
    {
        var e = O.Fails(source);
        Assert.Contains(message, e.Message);
        Assert.Equal((line, col), (e.Line, e.Col));
    }
}
