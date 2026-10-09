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

Check that every example still shows what it should:

```text
.\ooga --test examples tests\cases
```

Talk to ooga line by line:

```text
.\ooga --talk
```

New here? Start with **[QUICKSTART.md](QUICKSTART.md)**. Every word is in **[OOGA_RULEBOOK.md](OOGA_RULEBOOK.md)**.

## Folders

- `OOGA_RULEBOOK.md`: every ooga word and what it does
- `QUICKSTART.md`: exact commands and what you should see
- `PROGRESS.md`: what works, what does not yet, and what comes next
- `examples/`: starter scripts
- `my_scripts/`: your own scripts
- `bin/`: the ready-made `ooga.exe` runner (`ooga.cmd` and `ooga.sh` start it)
- `engine/`: the C# interpreter
- `tests/`: automatic checks for the interpreter (`tests/cases/` holds ooga scripts with their expected output)
- `vscode-ooga/`: VS Code colors (run `pack.ps1` to install)

## How the engine is built

```text
engine/Ooga.Core     the language. no console, no files, no game engine.
    Lexer.cs         text -> tokens (words, numbers, texts, symbols, push-in changes)
    Parser.cs        tokens -> program shape (Ast.cs)
    Checker.cs       finds typos and rule mistakes before anything runs
    Interpreter.cs   runs the program shape
    Values.cs        lists, boxes, action values; how values are shown and compared
    Library.cs       actions ooga already knows (round, split, sort, read_file, ...) + OogaAction for hosts
    CSharpBridge.cs  the C# door: csharp, csharp_new, csharp_call, csharp_get, csharp_set
    Host.cs          IOogaHost (say / ask / wait), IOogaFiles (optional files), RunOptions, OogaRunner
    Session.cs       OogaSession: talk mode, one piece at a time
    Errors.cs        OogaError (file + line + column), error report, "you mean ...?" spelling help

engine/Ooga.Cli      the "ooga" command: files, talk mode, console host, error printing

tests/Ooga.Tests     xunit tests (run with: dotnet test)
```

A future Godot (or other engine) adapter is a new project next to `Ooga.Cli` that gives the core its own `IOogaHost`
and adds its own actions through `RunOptions.Actions`. The core does not change for it.

Using ooga from your own C# program:

```csharp
var host = new MyHost();   // implements Ooga.IOogaHost (and Ooga.IOogaFiles if it has files)
Ooga.OogaRunner.Run(scriptText, host, new Ooga.RunOptions
{
    FileName = "game.ooga",
    Actions = new[] { new Ooga.OogaAction("jump", 1, call => { Jump(call.Number(0)); return null; }) },
});
```

Rebuild the runner after changing engine code (needs the .NET 8 SDK or newer):

```text
dotnet test
dotnet publish engine/Ooga.Cli -c Release -r win-x64 --self-contained false -p:DebugType=none -o bin
```
