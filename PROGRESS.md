# Ooga Progress

Last updated: 2026-10-09, branch `main-kp3dyi`.

## Where things stand

The standalone language core is done and tested: a C# command-line interpreter that runs `.ooga` files in a console.
Game engine work (Godot, Unity, Unreal) has **not** started on purpose.

The interpreter was already C# (.NET 8) before this round. It was kept and built on, not rewritten.
The same lexer, parser, checker and interpreter design remains. They were moved into their own projects and hardened.
An older idea of writing the ooga compiler in GDScript is dropped. C# is the direction.

## Tools used

| Tool | Version | Note |
|---|---|---|
| .NET SDK | 8.0.131 (Ubuntu 24.04 package `dotnet-sdk-8.0`) | Microsoft's download server was blocked in the cloud machine; Ubuntu's package worked |
| Target framework | `net8.0` | matches the .NET 8 already on the user's PC |
| Roll forward | `Major` | `ooga.exe` also runs if only a newer .NET (9, 10, ...) is installed |
| `global.json` | SDK 8.0.100, `rollForward: latestMajor` | builds with SDK 8 or any newer SDK |
| Test framework | xunit 2.9.2, Microsoft.NET.Test.Sdk 17.11.1 | |

**.NET 8 support ends on 10 November 2026.** Moving the target to `net10.0` (the current long-term release) is a small change. But the user would need the .NET 10 runtime installed first. See "Next recommended task".

## Implemented

| Feature | Ooga words |
|---|---|
| Things (variables) | `me has x 5`, `x is 6`, `x gain 1`, `x lose 1`, text `gain` joins |
| Numbers | `5`, `3.5`, negative with `-`; shown without extra zeros |
| Text | `"hi"`, escapes `\"` `\n` `\\`; `+` with text joins |
| yes/no and nothing | `yes`, `no`, `nothing` |
| Math | `+ - * /`, `( )`, minus in front; school order (see rulebook) |
| Checks | `is`, `is same`, `is not`, `is big`, `is small`, `is big or same`, `is small or same`, `and`, `or`, `not` |
| Conditions | `if` / `else if` / `else`, nested to any depth |
| Loops | `repeat N`, `repeat while`, `count i from A to B` (up or down), `stop`, `skip` |
| Actions | `me can name a b` with things, `give` answers, recursion, usable before the line that teaches them |
| Scope | file things vs. action things; action things hide file things of the same name |
| Console | `say`, `ask`, `random A to B`, `wait N`, `me die` |
| Errors | file, line, column, the code line, a `^` pointer, caveman message, "you mean ...?" hints |
| Safety | step limit and cancellation (used by tests, ready for engines), recursion limit, big stack |

### Engine layout (the boundary)

- `engine/Ooga.Core`: the language only. It talks to the outside through `IOogaHost` (`Say`, `Ask`, `Wait`).
- `engine/Ooga.Cli`: the `ooga` command. It provides `ConsoleHost` and turns errors into the console report.
- Future engine adapters: new projects that give the core their own `IOogaHost`. `when` and `me is` are kept for them.
  `when` gives a clear "come later" error today. `me is` is accepted and does nothing in the console.

## Decisions made (and why)

1. **No new syntax was needed.** Every required feature already had an ooga form, so the grammar is unchanged.
2. **Things handed to an action are single values.** `me f n - 1` means `(me f n) - 1`. This was already how the parser worked, and it keeps `say (me double 5) + 1` meaningful. It is now written clearly in the rulebook. There are also targeted error hints: math after a call (`me show x + 2`), an action's answer handed to another action without `( )`, and recursion that never ends.
3. **`ask` only turns plain numbers into numbers** (`5`, `-3`, `2.5`). Before, words like `NaN` and `Infinity` and forms like `1e5` became numbers. Now they stay text.
4. **`ask` after the input has ended is an error**, not an empty answer. This stops a `repeat while` + `ask` loop from running forever when input is piped in.
5. **Math that becomes infinitely big is an error** ("number too big for ooga") instead of printing `∞`.
6. **Errors go to the error stream (stderr)** and include the column. Exit codes are 0 ok (also after `me die`), 1 mistake in the script, 2 wrong command use or file not found, 3 engine bug.
7. **Typo hints**: swapped letters count as one typo (`sya` → `say`). Names of 3 letters or fewer only get a hint when one letter is off, so `x` never suggests `hp`. Capital letters are noticed (`Say` → `say`).
8. **Count things stay after the loop** at file level (`count i ...` then `say i` shows the last value). This is existing behaviour, kept and tested.
9. **A byte-order mark** (an invisible mark some Windows editors add) at the start of a file is ignored.
10. **The runner is framework-dependent** (small, needs the .NET runtime) like before, built for `win-x64`.

## Limitations (known, not bugs)

- No lists, no remainder (`%`), no rounding, no "is this a number?" check. So `04_guess_number` stops with an error if you type a word instead of a number.
- No way to read files or split a script over several files.
- `when` events, `me is`, and anything about players, enemies or scenes need a game engine adapter. None exists yet.
- Text checks can only be same or not same (no alphabetical big/small).
- `wait` blocks the whole program (fine for the console, an engine adapter will handle it differently).
- Using a file thing before its `me has` line has run is caught **while running**, not before. For example, an action called above the `me has` line it uses.
- `bin\ooga.exe` was built on Linux for Windows. It is a valid Windows x64 program, but it has **not been run on Windows** in this round. The same build's `ooga.dll` was run on Linux and works.

## Tests run

Command: `dotnet test Ooga.sln --blame-hang-timeout 2m` (on Linux, .NET SDK 8.0.131, after a clean rebuild).

Result: **246 passed, 0 failed, 0 skipped** (about 1 second).

What they cover (in `tests/Ooga.Tests`):

- `LexerParserTests`: tokens, positions, push-in, notes, Windows line endings, parse shapes, precedence in the tree.
- `ExecutionTests`: values and how they show, math and precedence, all checks, conditions (nested, else matching), all loops, `stop` / `skip` in nested loops, `ask` / `wait` / `random` / `me die`.
- `ActionAndScopeTests`: things, answers, early `give`, recursion (factorial, fibonacci, two actions calling each other), scope (inside vs. file, hiding, recursion frames, count inside actions).
- `ErrorTests`: 70 wrong programs, each checked for the exact line, column and message, plus the full error report text.
- `SafetyTests`: endless loops stop at the step limit or on cancel; 20,000-deep recursion does not crash.
- `ExampleAndCliTests`: every file in `examples/` runs through the real `ooga` command with typed answers and exact expected output; usage, missing file, stderr, exit codes, byte-order mark, input ending.

Every test runs with a 10-second wall-clock limit and a 2,000,000-step limit. The test run itself uses `--blame-hang-timeout 2m`.

Also checked by hand: `bin/ooga.dll` (the published runner) runs examples 01, 02, 05, 07, 08 and 09 on Linux with the output shown in `QUICKSTART.md`.

## Next recommended task

**Try it on Windows and write your own scripts.** Run `.\ooga examples\01_hello.ooga` and press Ctrl+Shift+B in VS Code to confirm the rebuilt `ooga.exe` works. Then write 2–3 small programs in `my_scripts\` and note every place ooga felt awkward.

After that, the most useful language additions, all small and in the same style, are:

1. a number check for `ask` answers (so games do not stop when you type a word),
2. remainder (for "every 3rd" without counters),
3. lists.

Before 10 November 2026: move from .NET 8 to .NET 10 (install the .NET 10 runtime on the PC, change `net8.0` to `net10.0` in `Directory.Build.props`, rebuild `bin`).
