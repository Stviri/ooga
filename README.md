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

Needs Windows with the .NET 8 runtime (or newer).

```text
.\ooga examples\01_hello.ooga
```

Or open the folder in VS Code, open a `.ooga` file, and press **Ctrl + Shift + B**.

New here? Start with **[QUICKSTART.md](QUICKSTART.md)**. Every word is in **[OOGA_RULEBOOK.md](OOGA_RULEBOOK.md)**.

## Folders

- `OOGA_RULEBOOK.md`: every ooga word and what it does
- `QUICKSTART.md`: exact commands and what you should see
- `PROGRESS.md`: what works, what does not yet, and what comes next
- `examples/`: starter scripts
- `my_scripts/`: your own scripts
- `bin/`: the ready-made `ooga.exe` runner (`ooga.cmd` and `ooga.sh` start it)
- `engine/`: the C# interpreter
- `tests/`: automatic checks for the interpreter
- `vscode-ooga/`: VS Code colors (run `pack.ps1` to install)

## How the engine is built

```text
engine/Ooga.Core     the language. no console, no files, no game engine.
    Lexer.cs         text -> tokens (words, numbers, texts, symbols, push-in changes)
    Parser.cs        tokens -> program shape (Ast.cs)
    Checker.cs       finds typos and rule mistakes before anything runs
    Interpreter.cs   runs the program shape
    Host.cs          IOogaHost: say / ask / wait go out through this. OogaRunner: the easy front door
    Errors.cs        OogaError (line + column), error report, "you mean ...?" spelling help

engine/Ooga.Cli      the "ooga" command: reads a file, gives the core a console host, prints errors

tests/Ooga.Tests     xunit tests (run with: dotnet test)
```

A future Godot (or other engine) adapter is a new project next to `Ooga.Cli` that gives the core its own `IOogaHost`.
The core does not change for it.

Rebuild the runner after changing engine code (needs the .NET 8 SDK or newer):

```text
dotnet test
dotnet publish engine/Ooga.Cli -c Release -r win-x64 --self-contained false -p:DebugType=none -o bin
```
