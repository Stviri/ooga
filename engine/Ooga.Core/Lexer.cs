using System.Globalization;
using System.Text;

namespace Ooga;

public enum Tok { Number, Text, Word, Symbol, Newline, Indent, Dedent, End }

public class Token
{
    public Tok Kind;
    public string Value;    // word text, symbol text, or text contents
    public double Number;
    public int Line;
    public int Col;
    public string File;

    public override string ToString() => Kind switch
    {
        Tok.Number => Value,
        Tok.Text => "\"" + Value + "\"",
        Tok.Word or Tok.Symbol => Value,
        Tok.Newline => "end of line",
        Tok.Indent => "pushed-in line",
        Tok.Dedent => "end of block",
        _ => "end of file",
    };
}

// Turns script text into tokens: words, numbers, texts, symbols, and indentation changes.
public static class Lexer
{
    const int IndentSize = 4;

    public static List<Token> Run(string source, string file = null)
    {
        try
        {
            var tokens = Read(source);
            foreach (var t in tokens) t.File = file;
            return tokens;
        }
        catch (OogaError e)
        {
            e.File ??= file;
            throw;
        }
    }

    static List<Token> Read(string source)
    {
        var tokens = new List<Token>();
        var indents = new Stack<int>();
        indents.Push(0);

        if (source.StartsWith('\uFEFF')) source = source.Substring(1);   // invisible mark some editors put at the start
        string[] lines = source.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');

        for (int li = 0; li < lines.Length; li++)
        {
            string line = lines[li];
            int lineNo = li + 1;

            // Measure indentation.
            int i = 0;
            while (i < line.Length && (line[i] == ' ' || line[i] == '\t'))
            {
                if (line[i] == '\t')
                    throw new OogaError(lineNo, i + 1, "found a tab key push here. ooga only like spaces. use 4 spaces for each push-in.");
                i++;
            }

            // Skip blank and comment-only lines.
            if (i >= line.Length || line[i] == '#')
                continue;

            int indent = i;
            if (indent > indents.Peek())
            {
                if (indent - indents.Peek() != IndentSize)
                    throw new OogaError(lineNo, 1, $"this line pushed in {indent - indents.Peek()} spaces. ooga want exactly {IndentSize} spaces more than line above.");
                indents.Push(indent);
                tokens.Add(new Token { Kind = Tok.Indent, Line = lineNo, Col = 1 });
            }
            else
            {
                while (indent < indents.Peek())
                {
                    indents.Pop();
                    tokens.Add(new Token { Kind = Tok.Dedent, Line = lineNo, Col = 1 });
                }
                if (indent != indents.Peek())
                    throw new OogaError(lineNo, 1, "this line push-in no match any line above. line up with a block, using 4 spaces per push-in.");
            }

            // Read tokens on this line.
            while (i < line.Length)
            {
                char c = line[i];
                int col = i + 1;

                if (c == ' ') { i++; continue; }
                if (c == '\t')
                    throw new OogaError(lineNo, col, "found a tab key push here. ooga only like spaces.");
                if (c == '#') break;

                if (char.IsDigit(c))
                {
                    int start = i;
                    while (i < line.Length && char.IsDigit(line[i])) i++;
                    if (i + 1 < line.Length && line[i] == '.' && char.IsDigit(line[i + 1]))
                    {
                        i++;
                        while (i < line.Length && char.IsDigit(line[i])) i++;
                    }
                    if (i < line.Length && (char.IsLetter(line[i]) || line[i] == '_'))
                        throw new OogaError(lineNo, col, "names no can start with number. try put letters first, like \"hit2\" not \"2hit\".");
                    string text = line.Substring(start, i - start);
                    tokens.Add(new Token { Kind = Tok.Number, Value = text, Number = double.Parse(text, CultureInfo.InvariantCulture), Line = lineNo, Col = col });
                    continue;
                }

                if (char.IsLetter(c) || c == '_')
                {
                    int start = i;
                    while (i < line.Length && (char.IsLetterOrDigit(line[i]) || line[i] == '_')) i++;
                    tokens.Add(new Token { Kind = Tok.Word, Value = line.Substring(start, i - start), Line = lineNo, Col = col });
                    continue;
                }

                if (c == '"')
                {
                    var sb = new StringBuilder();
                    i++;
                    bool closed = false;
                    while (i < line.Length)
                    {
                        char d = line[i];
                        if (d == '"') { closed = true; i++; break; }
                        if (d == '\\' && i + 1 < line.Length)
                        {
                            char e = line[i + 1];
                            if (e == 'n') sb.Append('\n');
                            else if (e == '"') sb.Append('"');
                            else if (e == '\\') sb.Append('\\');
                            else if (e == 't') sb.Append('\t');
                            else throw new OogaError(lineNo, i + 1, $"ooga no know \\{e}. inside text, use \\\" for quote, \\n for new line, \\t for tab, \\\\ for backslash.");
                            i += 2;
                            continue;
                        }
                        sb.Append(d);
                        i++;
                    }
                    if (!closed)
                        throw new OogaError(lineNo, col, "text start here but never end. put a \" at the end.");
                    tokens.Add(new Token { Kind = Tok.Text, Value = sb.ToString(), Line = lineNo, Col = col });
                    continue;
                }

                if ("+-*/%()".IndexOf(c) >= 0)
                {
                    tokens.Add(new Token { Kind = Tok.Symbol, Value = c.ToString(), Line = lineNo, Col = col });
                    i++;
                    continue;
                }

                if (c == '=')
                    throw new OogaError(lineNo, col, "ooga no use =. to change a thing write \"health is 5\". to check, write \"if health is 5\".");

                throw new OogaError(lineNo, col, $"ooga no know this sign: {c}");
            }

            tokens.Add(new Token { Kind = Tok.Newline, Line = lineNo, Col = line.Length + 1 });
        }

        int lastLine = lines.Length;
        while (indents.Count > 1)
        {
            indents.Pop();
            tokens.Add(new Token { Kind = Tok.Dedent, Line = lastLine, Col = 1 });
        }
        tokens.Add(new Token { Kind = Tok.End, Line = lastLine, Col = 1 });
        return tokens;
    }
}
