using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Ooga;

// Ooga words for the outside world: folders and files, spreadsheets (CSV), JSON, the web, and the clock.
// They do what the C# door can do, but in ooga words, so nobody needs to know C# names.
public static class WorldLibrary
{
    static readonly Regex PlainNumber = new(@"^-?[0-9]+(\.[0-9]+)?$");
    static readonly HttpClient Web = MakeWebClient();

    public static IEnumerable<OogaAction> Actions()
    {
        // ---------- paths (just text, no files touched) ----------
        yield return new("join_path", 2, -1, c => Path.Combine(Enumerable.Range(0, c.Count).Select(c.Text).ToArray()));
        yield return new("file_name", 1, c => Path.GetFileName(c.Text(0)));
        yield return new("file_ending", 1, c => Path.GetExtension(c.Text(0)).TrimStart('.'));
        yield return new("file_folder", 1, c => Path.GetDirectoryName(c.Text(0)) is string d && d != "" ? d : ".");

        // ---------- folders ----------
        yield return new("folder_exists", 1, c => Files(c, () => c.Files().FolderExists(c.Text(0))));
        yield return new("files_in", 1, c => Files(c, () => new OogaList(c.Files().FilesIn(c.Text(0)))));
        yield return new("folders_in", 1, c => Files(c, () => new OogaList(c.Files().FoldersIn(c.Text(0)))));
        yield return new("make_folder", 1, c => Files(c, () => { c.Files().MakeFolder(c.Text(0)); return null; }));

        // ---------- files ----------
        yield return new("move_file", 2, c => Files(c, () => { c.Files().MoveFile(c.Text(0), Into(c, c.Text(1))); return null; }));
        yield return new("copy_file", 2, c => Files(c, () => { c.Files().CopyFile(c.Text(0), Into(c, c.Text(1))); return null; }));
        yield return new("delete_file", 1, c => Files(c, () => { c.Files().DeleteFile(c.Text(0)); return null; }));
        yield return new("file_size", 1, c => Files(c, () => (double)c.Files().FileSize(c.Text(0))));
        yield return new("read_lines", 1, c => Files(c, () => new OogaList(SplitLines(c.Files().ReadFile(c.Text(0))))));
        yield return new("write_lines", 2, c => Files(c, () =>
        {
            var lines = c.List(1).Items.Select(Values.Show).ToList();
            c.Files().WriteFile(c.Text(0), lines.Count == 0 ? "" : string.Join("\n", lines) + "\n", add: false);
            return null;
        }));

        // ---------- spreadsheets (CSV) ----------
        yield return new("from_csv", 1, c => FromCsv(c, c.Text(0)));
        yield return new("to_csv", 1, c => ToCsv(c, c[0]));
        yield return new("read_csv", 1, c => FromCsv(c, (string)Files(c, () => c.Files().ReadFile(c.Text(0)))));
        yield return new("write_csv", 2, c =>
        {
            string text = ToCsv(c, c[1]);
            return Files(c, () => { c.Files().WriteFile(c.Text(0), text, add: false); return null; });
        });

        // ---------- JSON ----------
        yield return new("from_json", 1, c => FromJson(c, c.Text(0)));
        yield return new("to_json", 1, c => ToJson(c, c[0]));

        // ---------- the web ----------
        yield return new("web_get", 1, c => WebSend(c, HttpMethod.Get, c.Text(0), null));
        yield return new("web_get_json", 1, c => FromJson(c, WebSend(c, HttpMethod.Get, c.Text(0), null)));
        yield return new("web_post", 2, c => WebSend(c, HttpMethod.Post, c.Text(0), c[1]));

        // ---------- the clock ----------
        yield return new("now", 0, c =>
        {
            var t = DateTime.Now;
            var box = new OogaBox();
            box.Set("year", (double)t.Year);
            box.Set("month", (double)t.Month);
            box.Set("day", (double)t.Day);
            box.Set("hour", (double)t.Hour);
            box.Set("minute", (double)t.Minute);
            box.Set("second", (double)t.Second);
            box.Set("weekday", t.DayOfWeek.ToString());
            return box;
        });
    }

    // ---------- files ----------

    // "move_file a.txt photos": when the place is a folder, the file goes inside it with its own name.
    static string Into(ActionCall c, string to) =>
        c.Files().FolderExists(to) ? Path.Combine(to, Path.GetFileName(c.Text(0))) : to;

    // Problems with files become ooga problems that "try" can catch, in plain words.
    static object Files(ActionCall c, Func<object> work)
    {
        try
        {
            return work();
        }
        catch (FileNotFoundException e)
        {
            throw c.Problem($"{c.Name} no find file: {e.FileName ?? Values.Show(c[0])}");
        }
        catch (DirectoryNotFoundException)
        {
            throw c.Problem($"{c.Name} no find folder for: {string.Join(" or ", c.Things.Select(Values.Show))}");
        }
        catch (UnauthorizedAccessException)
        {
            throw c.Problem($"{c.Name} not allowed to touch: {Values.Show(c[0])}");
        }
        catch (IOException e) when (e.Message.Contains("already exists"))
        {
            throw c.Problem($"{c.Name} no can: there is already a file at {Values.Show(c[c.Count - 1])}");
        }
        catch (Exception e) when (e is IOException or ArgumentException or NotSupportedException)
        {
            throw c.Problem($"{c.Name} no work: {e.Message}");
        }
    }

    static IEnumerable<string> SplitLines(string text)
    {
        var lines = text.Replace("\r\n", "\n").Split('\n').ToList();
        if (lines.Count > 0 && lines[^1] == "") lines.RemoveAt(lines.Count - 1);
        return lines;
    }

    // ---------- CSV ----------

    // A spreadsheet as text -> a list of boxes, one per row, with the header row as part names.
    // Plain numbers become numbers. Fields may be in "quotes" (with "" for a quote inside).
    // Files that use ; instead of , (some countries' Excel does) work too.
    static OogaList FromCsv(ActionCall c, string text)
    {
        text = text.TrimStart('﻿');
        string firstLine = text.Split('\n')[0];
        char sep = !firstLine.Contains(',') && firstLine.Contains(';') ? ';' : ',';
        var rows = ParseCsvRows(c, text, sep);
        var result = new OogaList();
        if (rows.Count == 0) return result;
        var header = rows[0];
        for (int r = 1; r < rows.Count; r++)
        {
            var row = rows[r];
            if (row.Count == 1 && row[0] == "") continue;   // empty line
            var box = new OogaBox();
            for (int i = 0; i < Math.Max(header.Count, row.Count); i++)
            {
                string name = i < header.Count && header[i] != "" ? header[i] : $"column {i + 1}";
                string cell = i < row.Count ? row[i] : "";
                box.Set(name, PlainNumber.IsMatch(cell.Trim()) ? double.Parse(cell.Trim(), CultureInfo.InvariantCulture) : cell);
            }
            result.Items.Add(box);
        }
        return result;
    }

    static List<List<string>> ParseCsvRows(ActionCall c, string text, char sep)
    {
        var rows = new List<List<string>>();
        var row = new List<string>();
        var cell = new StringBuilder();
        bool quoted = false, any = false;
        for (int i = 0; i < text.Length; i++)
        {
            char ch = text[i];
            any = true;
            if (quoted)
            {
                if (ch == '"' && i + 1 < text.Length && text[i + 1] == '"') { cell.Append('"'); i++; }
                else if (ch == '"') quoted = false;
                else cell.Append(ch);
            }
            else if (ch == '"' && cell.Length == 0) quoted = true;
            else if (ch == sep) { row.Add(cell.ToString()); cell.Clear(); }
            else if (ch == '\r') { }
            else if (ch == '\n') { row.Add(cell.ToString()); cell.Clear(); rows.Add(row); row = new List<string>(); any = false; }
            else cell.Append(ch);
        }
        if (quoted) throw c.Problem($"{c.Name}: a \" in the spreadsheet starts quoted text but never ends it.");
        if (any) { row.Add(cell.ToString()); rows.Add(row); }
        return rows;
    }

    // A list of boxes (one per row) -> spreadsheet text with a header row. A list of lists works too (no header).
    static string ToCsv(ActionCall c, object value)
    {
        if (value is not OogaList list)
            throw c.Problem($"{c.Name} need a list of rows (boxes or lists), but got {Values.Describe(value)}.");
        var lines = new List<string>();
        if (list.Items.All(r => r is OogaBox))
        {
            var columns = new List<object>();
            foreach (OogaBox box in list.Items)
                foreach (var key in box.Keys)
                    if (!columns.Any(k => Values.Same(k, key))) columns.Add(key);
            lines.Add(string.Join(",", columns.Select(k => CsvCell(Values.Show(k)))));
            foreach (OogaBox box in list.Items)
                lines.Add(string.Join(",", columns.Select(k => box.TryGet(k, out var v) ? CsvCell(Values.Show(v)) : "")));
        }
        else if (list.Items.All(r => r is OogaList))
        {
            foreach (OogaList row in list.Items)
                lines.Add(string.Join(",", row.Items.Select(v => CsvCell(Values.Show(v)))));
        }
        else
        {
            throw c.Problem($"{c.Name} need every row to be a box (named parts) or every row to be a list.");
        }
        return string.Join("\n", lines) + "\n";
    }

    static string CsvCell(string s) =>
        s.IndexOfAny(new[] { ',', '"', '\n', ';' }) >= 0 || s != s.Trim() ? "\"" + s.Replace("\"", "\"\"") + "\"" : s;

    // ---------- JSON ----------

    static object FromJson(ActionCall c, string text)
    {
        try
        {
            using var doc = JsonDocument.Parse(text);
            return FromJsonElement(doc.RootElement);
        }
        catch (JsonException e)
        {
            throw c.Problem($"{c.Name}: this is not good JSON ({e.Message.Split('.')[0]}).");
        }
    }

    static object FromJsonElement(JsonElement e) => e.ValueKind switch
    {
        JsonValueKind.Object => JsonBox(e),
        JsonValueKind.Array => new OogaList(e.EnumerateArray().Select(FromJsonElement).ToList()),
        JsonValueKind.String => e.GetString(),
        JsonValueKind.Number => e.GetDouble(),
        JsonValueKind.True => true,
        JsonValueKind.False => false,
        _ => null,
    };

    static OogaBox JsonBox(JsonElement e)
    {
        var box = new OogaBox();
        foreach (var p in e.EnumerateObject()) box.Set(p.Name, FromJsonElement(p.Value));
        return box;
    }

    static string ToJson(ActionCall c, object value)
    {
        using var stream = new MemoryStream();
        using (var w = new Utf8JsonWriter(stream))
            WriteJson(c, w, value, 0);
        return Encoding.UTF8.GetString(stream.ToArray());
    }

    static void WriteJson(ActionCall c, Utf8JsonWriter w, object v, int depth)
    {
        if (depth > 64) throw c.Problem($"{c.Name}: lists or boxes inside each other too deep (maybe one is inside itself).");
        switch (v)
        {
            case null: w.WriteNullValue(); break;
            case bool b: w.WriteBooleanValue(b); break;
            case double d: w.WriteNumberValue(d); break;
            case string s: w.WriteStringValue(s); break;
            case OogaList list:
                w.WriteStartArray();
                foreach (var item in list.Items) WriteJson(c, w, item, depth + 1);
                w.WriteEndArray();
                break;
            case OogaBox box:
                w.WriteStartObject();
                foreach (var key in box.Keys)
                {
                    box.TryGet(key, out var item);
                    w.WritePropertyName(Values.Show(key));
                    WriteJson(c, w, item, depth + 1);
                }
                w.WriteEndObject();
                break;
            default:
                throw c.Problem($"{c.Name} no can turn {Values.Describe(v)} into JSON.");
        }
    }

    // ---------- the web ----------

    static HttpClient MakeWebClient()
    {
        var client = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("ooga", "1.0"));
        return client;
    }

    // Sends a web request and gives back the answer as text. Data to send: text is sent as it is,
    // lists and boxes are sent as JSON.
    static string WebSend(ActionCall c, HttpMethod method, string url, object data)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || (uri.Scheme != "http" && uri.Scheme != "https"))
            throw c.Problem($"{c.Name} need a web address starting with http:// or https://, but got \"{url}\".");
        using var request = new HttpRequestMessage(method, uri);
        if (data != null)
            request.Content = data is string text
                ? new StringContent(text, Encoding.UTF8, "text/plain")
                : new StringContent(ToJson(c, data), Encoding.UTF8, "application/json");
        try
        {
            using var response = Web.Send(request, c.Options.Cancel);
            string body = response.Content.ReadAsStringAsync(c.Options.Cancel).GetAwaiter().GetResult();
            if (!response.IsSuccessStatusCode)
                throw c.Problem($"{c.Name}: the web said {(int)response.StatusCode} ({response.ReasonPhrase}) for {url}");
            return body;
        }
        catch (OperationCanceledException) when (c.Options.Cancel.IsCancellationRequested)
        {
            throw;
        }
        catch (TaskCanceledException)
        {
            throw c.Problem($"{c.Name}: the web took too long to answer (more than 30 seconds): {url}");
        }
        catch (HttpRequestException e)
        {
            throw c.Problem($"{c.Name} no reach {url}: {e.Message}");
        }
    }
}
