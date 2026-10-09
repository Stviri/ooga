# ooga

A caveman-style programming language. Simple words, one meaning each.

```text
me has health 100

me can hit amount
    health lose amount
    if health is small or same 0
        say "me die"
        me die

me hit 30
```

## Run a script

Needs Windows with .NET 8.

```text
.\ooga examples\01_hello.ooga
```

Or open the folder in VS Code, open a `.ooga` file, and press **Ctrl + Shift + B**.

## Folders

- `OOGA_RULEBOOK.md`: every ooga word and what it does
- `examples/`: starter scripts
- `my_scripts/`: your own scripts
- `vscode-ooga/`: VS Code colors (run `pack.ps1` to install)
- `engine/`: the C# interpreter. Rebuild with `dotnet publish engine -c Release -o bin`
