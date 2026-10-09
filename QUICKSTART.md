# Ooga Quickstart

This page shows how to get Ooga running and what you should see. Every output on this page was copied from a real run.

## 1. What you need

- **Windows** with the **.NET 8 runtime** (or anything newer: .NET 9, 10, ...).
  Check in a terminal with `dotnet --list-runtimes`. You need a line that starts with `Microsoft.NETCore.App 8.` (or higher).
  If it is missing, install ".NET 8 Runtime" (or newer) from https://dotnet.microsoft.com/download.
- **VS Code**. It is optional, but it makes things nicer.

You do **not** need to build anything. The ready-made runner is already in `bin\ooga.exe`.

## 2. Get the newest Ooga

Open a terminal (PowerShell) in your Ooga folder and type:

```text
cd C:\Users\Stviri\Desktop\Ooga
git fetch origin
git checkout main-kp3dyi
git pull
```

(When this branch is merged into `main` later, use `git checkout main` and `git pull` instead.)

## 3. Run your first script

```text
.\ooga examples\01_hello.ooga
```

Ooga asks for your name. Type `Grok` and press Enter:

```text
ooga booga
what your name? Grok
hello Grok, welcome to cave.
```

**In VS Code:** open any `.ooga` file and press **Ctrl + Shift + B**. That runs the file you have open.

## 4. Try the other examples

| Command | What it shows |
|---|---|
| `.\ooga examples\02_calculator.ooga` | `ask`, `if` / `else if` / `else`, and math |
| `.\ooga examples\03_counting.ooga` | `repeat`, `count`, `stop`, `skip`, `wait` |
| `.\ooga examples\04_guess_number.ooga` | `random`, `repeat while`, a full small game |
| `.\ooga examples\05_actions.ooga` | your own actions with `me can` and `give` |
| `.\ooga examples\06_player.ooga` | an action that changes a thing, and `me die` |
| `.\ooga examples\07_mistakes.ooga` | what an error looks like |
| `.\ooga examples\08_shop.ooga` | actions, loops, checks and `ask` together |
| `.\ooga examples\09_fizzbuzz.ooga` | counters and joining text |

What you should see:

**02_calculator**: type `6`, then `*`, then `7`:

```text
first number? 6
what to do? (+ - * /) *
second number? 7
42
```

**05_actions**:

```text
ooga, grok!
ooga, zug!
42
5
11
```

**06_player**:

```text
ouch! health now 70
ouch! health now 40
ouch! health now 10
ouch! health now -20
player die
```

**08_shop**: type `2`, then `9`, then `0`:

```text
welcome to cave shop. you have 20 shells.
1 = club (5 shells), 2 = rock (2 shells), 0 = leave
buy what? 2
you buy rock. 18 shells left.
buy what? 9
ooga no sell that.
buy what? 0
bye! you leave with 18 shells and 1 things.
```

**09_fizzbuzz** (one number per line; shown here on one line to save space):

```text
1 2 fizz 4 buzz fizz 7 8 fizz buzz 11 fizz 13 14 fizzbuzz
```

## 5. When something is wrong

```text
.\ooga examples\07_mistakes.ooga
```

```text
ooga booga! problem in examples\07_mistakes.ooga line 7, column 5:
    say heath
        ^
no thing called "heath". you mean "health"?
```

The `^` points at the exact spot. Fix the line, save, and run again.
Ooga checks the **whole file before running it**, so typos in names and actions are found even in lines that would run much later.

## 6. Write your own

Open `my_scripts\playground.ooga`, write something, and run it:

```text
me has health 100

me can hit amount
    health lose amount
    give health

say "health left: " + (me hit 30)
```

```text
.\ooga my_scripts\playground.ooga
```

```text
health left: 70
```

Every word you can use is in [OOGA_RULEBOOK.md](OOGA_RULEBOOK.md).

## 7. Linux or macOS (optional)

You need .NET 8 or newer installed. Then:

```text
./ooga.sh examples/01_hello.ooga
```

## 8. For later: rebuilding and testing the engine

You only need this if you change the C# code in `engine\`. It needs the **.NET 8 SDK** (or newer), not just the runtime.

```text
dotnet test                                   # run all the engine tests
dotnet run --project engine\Ooga.Cli -- examples\01_hello.ooga
dotnet publish engine\Ooga.Cli -c Release -r win-x64 --self-contained false -p:DebugType=none -o bin
```

The last line rebuilds `bin\ooga.exe`.
