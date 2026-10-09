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

## Install on Windows, step by step

About 15 minutes. You only do this once. Every command below is typed into **PowerShell**:
press the **Windows key**, type `powershell`, press **Enter**. Type (or paste) one command, press **Enter**, and wait until it finishes before the next one.

### Step 1: Install the three tools

Ooga needs **.NET** (it runs Ooga), **Git** (it downloads Ooga and its updates) and **VS Code** (where you write Ooga). Paste these one at a time:

```text
winget install --id Microsoft.DotNet.Runtime.8 -e
winget install --id Git.Git -e
winget install --id Microsoft.VisualStudioCode -e
```

If it asks you to agree to terms, type `Y` and press Enter. If Windows asks "Do you want to allow this app to make changes?", click **Yes**.

Then **close PowerShell and open it again**, so it notices the new tools.

> No `winget`? Download the installers instead: [.NET 8 Runtime](https://dotnet.microsoft.com/download/dotnet/8.0) (pick ".NET Runtime", Windows x64), [Git](https://git-scm.com/download/win), [VS Code](https://code.visualstudio.com/). Click Next through each one.

### Step 2: Download Ooga

This puts an `ooga` folder on your Desktop:

```text
cd $HOME\Desktop
git clone https://github.com/Stviri/ooga.git
cd ooga
```

The Ooga repository is private, so Git opens a window to **sign in to GitHub**. Sign in with the account that has access.

### Step 3: Run your first script

```text
.\ooga examples\01_hello.ooga
```

Type a name and press Enter. You should see:

```text
ooga booga
what your name? Grok
hello Grok, welcome to cave.
```

The `.\` at the start matters: it tells PowerShell "the ooga in this folder".

### Step 4: Check that everything works

```text
.\ooga --test examples tests\cases
```

The last two lines should be `32 ok, 0 wrong.` and `ooga happy.`

### Step 5: Add Ooga colours to VS Code

This makes Ooga words colourful in VS Code and pushes lines in for you after `if`, `repeat`, `each` and the other block words. It is a PowerShell script, so type it **without** `ooga` in front:

```text
powershell -ExecutionPolicy Bypass -File .\vscode-ooga\pack.ps1
```

`-ExecutionPolicy Bypass` lets this one script run. Windows blocks scripts by default, and this does not change that setting. When it says the extension was installed, close VS Code if it is open.

### Step 6: Write Ooga in VS Code

```text
code .
```

(or open VS Code and choose **File > Open Folder** and pick the `ooga` folder). If VS Code asks whether you trust the authors of the files, click **Yes, I trust the authors**.

Open `examples\01_hello.ooga` and press **Ctrl + Shift + B** to run it. The answer appears in the panel at the bottom.
Your own scripts go in `my_scripts`. Start with `my_scripts\playground.ooga`.

Pressing **Tab** puts in 4 spaces. That is exactly how much Ooga wants a line pushed in.

### Later: get the newest Ooga

In PowerShell, inside the `ooga` folder:

```text
git pull
```

If the update changed VS Code colours (anything in `vscode-ooga`), run the Step 5 command again.

### If something goes wrong

| What you see | What to do |
|---|---|
| `running scripts is disabled on this system` | Use the full Step 5 command, starting with `powershell -ExecutionPolicy Bypass`. |
| `ooga booga! problem in vscode-ooga\pack.ps1 ... no know this sign: $` | You put `ooga` in front of a PowerShell script. Use the Step 5 command instead. |
| `The term 'ooga' is not recognized` | Type `.\ooga`, with `.\` in front, and make sure you are in the `ooga` folder (`cd $HOME\Desktop\ooga`). |
| `ooga no find file: ...` | The file name or folder is wrong. Check the spelling, and that you are in the `ooga` folder. |
| `You must install or update .NET to run this application` | Do the .NET part of Step 1, then close and reopen PowerShell. |
| `'winget' is not recognized` | Use the download links under Step 1. |
| `'git' is not recognized` or `'code' is not recognized` | Close PowerShell and open it again after installing. If it still happens, reinstall that tool. |
| Step 5 says it cannot find `code.cmd` | In VS Code press **Ctrl + Shift + P**, type `Install from VSIX`, press Enter, and pick `vscode-ooga\ooga-language.vsix`. |
| `git pull` says files would be overwritten | Close VS Code and any running Ooga window, then run `git pull` again. |

## Everyday commands

Run a script:

```text
.\ooga examples\01_hello.ooga
```

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
