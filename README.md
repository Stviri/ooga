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
winget install --id Microsoft.DotNet.DesktopRuntime.8 -e
winget install --id Git.Git -e
winget install --id Microsoft.VisualStudioCode -e
```

If it asks you to agree to terms, type `Y` and press Enter. If Windows asks "Do you want to allow this app to make changes?", click **Yes**.

Then **close PowerShell and open it again**, so it notices the new tools.

> No `winget`? Download the installers instead: [.NET 8 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/8.0) (pick ".NET Desktop Runtime", Windows x64), [Git](https://git-scm.com/download/win), [VS Code](https://code.visualstudio.com/). Click Next through each one.

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

The last two lines should be `36 ok, 0 wrong.` and `ooga happy.`

### Step 5: Set ooga up on Windows

One command does three things, for your Windows user only (no administrator rights needed):

1. `ooga` works in **any folder and any terminal**, just like `python`.
2. `.ooga` files get the **ooga icon**, and **double-clicking** one runs it in a window.
3. VS Code gets the **Ooga extension**: colours, automatic push-in, the ooga file icon and a **Run button**.

It is a PowerShell script, so type it **without** `ooga` in front:

```text
powershell -ExecutionPolicy Bypass -File .\install.ps1
```

`-ExecutionPolicy Bypass` lets this one script run. Windows blocks scripts by default, and this does not change that setting.
You should see three lines starting `[1/3]`, `[2/3]` and `[3/3]`, then `ooga ready!`.

Then **close PowerShell and open a new one** (and close VS Code if it was open), so they notice the change. Now this works from any folder:

```text
ooga hello.ooga
```

To undo all of it later: `powershell -ExecutionPolicy Bypass -File .\uninstall.ps1`. Your scripts are not touched.

### Step 6: Write Ooga in VS Code

```text
code .
```

(or open VS Code and choose **File > Open Folder** and pick the `ooga` folder). If VS Code asks whether you trust the authors of the files, click **Yes, I trust the authors**.

Open `examples\01_hello.ooga` and run it with any of these:

- the **Run button** (the triangle at the top right of the file)
- **Ctrl + F5**
- right-click the file in the Explorer on the left and choose **Run Ooga File**

The answer appears in the panel at the bottom; type your answers there when the script asks. (**Ctrl + Shift + B** still works too, inside the `ooga` folder.)
Your own scripts go in `my_scripts`. Start with `my_scripts\playground.ooga`. `.ooga` files show the ooga face in VS Code's file list and tabs.

Pressing **Tab** puts in 4 spaces. That is exactly how much Ooga wants a line pushed in.

### Double-click to run

In File Explorer, `.ooga` files now have the ooga icon. Double-click one: a window opens, the script runs, and the window waits for a key press so you can read what it said.
Right-click a `.ooga` file for **Edit in Notepad** or **Edit in VS Code**.

### Later: get the newest Ooga

In PowerShell, inside the `ooga` folder:

```text
git pull
powershell -ExecutionPolicy Bypass -File .\install.ps1
```

Running `install.ps1` again is safe. It updates the VS Code extension and the file icon, and never adds ooga to PATH twice. Run it again too if you move the `ooga` folder.

### If something goes wrong

| What you see | What to do |
|---|---|
| `running scripts is disabled on this system` | Use the full Step 5 command, starting with `powershell -ExecutionPolicy Bypass`. |
| `ooga booga! problem in ...ps1 ... no know this sign: $` | You put `ooga` in front of a PowerShell script. Use the Step 5 command instead. |
| `The term 'ooga' is not recognized` | Open a **new** PowerShell window after Step 5. Inside the `ooga` folder, `.\ooga` (with `.\` in front) always works. |
| `ooga no find file: ...` | The file name or folder is wrong. Check the spelling, and which folder you are in (`cd` shows it). |
| `You must install or update .NET to run this application` | ooga needs the **.NET Desktop Runtime** (it can open windows). Run `winget install --id Microsoft.DotNet.DesktopRuntime.8 -e` (or `install.ps1`, which offers to), then close and reopen PowerShell. |
| `'winget' is not recognized` | Use the download links under Step 1. |
| `'git' is not recognized` or `'code' is not recognized` | Close PowerShell and open it again after installing. If it still happens, reinstall that tool. |
| `[3/3] VS Code not found` | Install VS Code (Step 1), then run `install.ps1` again. |
| No Run button, or no colours, in VS Code | Close every VS Code window and open it again. Still nothing: press **Ctrl + Shift + P**, type `Install from VSIX`, press Enter, and pick `vscode-ooga\ooga-language.vsix`. |
| No ooga face on `.ooga` files in VS Code | Your VS Code *file icon theme* may draw its own icons. **File > Preferences > Theme > File Icon Theme**, pick **Seti**. |
| No ooga icon in File Explorer yet | Windows caches icons. Wait a minute, or sign out and back in. |
| Double-click opens another program | Run `install.ps1` again: it clears an old "Open with" choice for `.ooga` files. If it says Windows kept that choice, right-click the file and pick **Run with Ooga**. |
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
Know some C#, or want to learn it next? **[OOGA_VS_CSHARP.md](OOGA_VS_CSHARP.md)** shows 10 programs written in both, side by side.

## Folders

- `OOGA_RULEBOOK.md`: every ooga word and what it does
- `QUICKSTART.md`: exact commands and what you should see
- `PROGRESS.md`: what works, what does not yet, and what comes next
- `OOGA_VS_CSHARP.md`: the same programs in Ooga and C#, side by side (code in `docs/ooga-vs-csharp/`)
- `examples/`: starter scripts
- `my_scripts/`: your own scripts
- `bin/`: the ready-made Windows `ooga.exe` runner (`ooga.cmd` starts it; `ooga.sh` runs ooga from source on Linux and macOS)
- `engine/`: the C# interpreter
- `tests/`: automatic checks for the interpreter (`tests/cases/` holds ooga scripts with their expected output)
- `install.ps1` / `uninstall.ps1`: set ooga up on Windows (PATH, file icon, double-click, VS Code extension), or undo it
- `ooga-run.cmd`: what double-clicking a `.ooga` file runs
- `icons/`: the ooga icon
- `setup/`: helpers used by the install scripts
- `vscode-ooga/`: the VS Code extension (colours, Run button, file icon); `install.ps1` installs it

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
dotnet publish engine/Ooga.Cli -f net8.0-windows -c Release -r win-x64 --self-contained false -p:DebugType=none -o bin
```
