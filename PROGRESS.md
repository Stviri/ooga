# Ooga Progress

Last updated: 2026-10-09, branch `main-kp3dyi` (pull request #1).

## Where things stand

Ooga is a general-purpose language now, still in caveman style, run by a C# interpreter.
It has lists, boxes (named parts), loops over them, problem catching, more than one file, a library of ready-made actions, actions as values, an interactive talk mode, and a door into all of C# and .NET.

Game engine work (Godot, Unity, Unreal) has **not** started on purpose.

## Tools used

| Tool | Version | Note |
|---|---|---|
| .NET SDK | 8.0.131 (Ubuntu 24.04 package `dotnet-sdk-8.0`) | Microsoft's download server was blocked in the cloud machine; Ubuntu's package worked |
| Target framework | `net8.0` | matches the .NET 8 already on the user's PC |
| Roll forward | `Major` | `ooga.exe` also runs if only a newer .NET (9, 10, ...) is installed |
| `global.json` | SDK 8.0.100, `rollForward: latestMajor` | builds with SDK 8 or any newer SDK |
| Test framework | xunit 2.9.2, Microsoft.NET.Test.Sdk 17.11.1 | |

**.NET 8 support ends on 10 November 2026.** See "Next recommended tasks".

## Implemented

| Area | Ooga words |
|---|---|
| Things (variables) | `me has x 5`, `x is 6`, `x gain 1`, `x lose 1`, text `gain` joins |
| Values | numbers, text (`\"` `\n` `\t` `\\`), `yes`/`no`, `nothing`, lists, boxes, actions, C# things |
| Math | `+ - * / %`, `( )`, minus in front; school order (see rulebook) |
| Checks | `is`, `is same`, `is not`, `is big`, `is small`, `is big or same`, `is small or same`, `has`, `kind of`, `and`, `or`, `not`; texts compare in letter order; lists and boxes compare by what is inside |
| Conditions | `if` / `else if` / `else`, nested to any depth |
| Loops | `repeat N`, `repeat while`, `count i from A to B`, `each x in bag`, `stop`, `skip` |
| Lists | `list 1 2 3`, `item N of bag` (read and change), `size of`, `gain` / `lose`, `has`, lists inside lists |
| Boxes | `box name "grok" health 100`, `health of player` (read, change, add), boxes inside boxes, `item "key" of box` lookups |
| Actions | `me can name a b`, `give`, recursion, use before teaching, shared lists and boxes can be changed |
| Actions as values | `action double`, `me call f ...`, `keep`, `change_each`, `sort_by` |
| Problems | `try` / `oops why`, `fail "reason"`; safety stops (`ooga tired`) and `me die` can not be caught |
| More files | `use "tools"` (each file once; errors name the right file) |
| Library | round, round_to, round_down, round_up, positive, power, root, biggest, smallest, numbers, upper, lower, trim, split, join, replace, starts_with, ends_with, number, text, piece, find, reverse, sort, take, put_at, keys, copy, pick, shuffle, call, keep, change_each, sort_by, time, date, arguments, read_file, write_file, add_to_file, file_exists |
| C# door | `csharp`, `csharp_new`, `csharp_call`, `csharp_get`, `csharp_set`; values convert both ways; ooga actions become C# delegates; generic methods and LINQ work |
| Console | `say`, `ask`, `random A to B`, `wait N`, `me die`, `me arguments` |
| Talk mode | `ooga --talk`: type pieces, things are kept, a value alone is shown |
| Script checks | `ooga --test examples tests/cases`: every script is compared with `expected/NAME.out` (typed answers from `.in`, words from `.args`); `--make-expected` writes missing ones; `--seed N` for repeatable random |
| Errors | file, line, column, the code line, a `^` pointer, caveman message, "you mean ...?" hints |
| Safety | step limit and cancellation, recursion limit, big stack, cycle-safe showing of lists |

### Engine layout (the boundary)

- `engine/Ooga.Core`: the language only. It talks to the outside through `IOogaHost` (`Say`, `Ask`, `Wait`) and, if the host has files, `IOogaFiles`.
  A host can add its own actions with `RunOptions.Actions` (`OogaAction`). This is how a game engine adapter will add things like `jump` or `play_sound`.
  `RunOptions.AllowCSharp = false` turns the C# door off (for example inside a game where scripts should not touch the computer).
- `engine/Ooga.Cli`: the `ooga` command. It provides `ConsoleHost` (console + files), talk mode, and the error printout.
- `when` and `me is` are kept for engine adapters. `when` gives a clear "come later" error today.

## Decisions made (and why)

1. **No change to the old syntax.** Every v1 program still runs the same, except that it now can't use the new special words as names (see 2).
2. **New special words:** `list box of item size kind each in try oops fail use action`. The only clash in this repository was the parameter `item` in `08_shop`, which is now `choice`. Old scripts that use any of these words as names must rename them; ooga says exactly which line.
3. **Ready-made actions use the existing action form** (`me round 2.5`), so no new grammar was needed for the library. Their names are reserved as action names only. Things may still be called `round` or `text`.
4. **Positions start at 1** (`item 1 of bag`), like `count i from 1`.
5. **Lists and boxes are shared, not copied**, like in C#. An action can change the player box it was given. `me copy` makes a separate one.
6. **`item k of bag`**: a plain name right after `item` is the position, never `k of bag`. Use `( )` for anything else.
7. **Things handed to an action are single values** (unchanged): `me f (n - 1)`, `me shout ("hi " + name)`. The same rule applies to list items (`list 1 (-2)`).
8. **`%` is "what is left over"**, and never goes below 0 when splitting by a number above 0 (`-7 % 3` is `2`).
9. **`try` can not catch** `ooga tired` (the step limit), cancellation, or `me die`. A runaway program can always be stopped.
10. **C# door rules:** objects made with `csharp_new` stay live C# objects, even collections. Results of calls and properties that are sequences become ooga lists, and dictionaries become boxes. Numbers go to `int` / `long` / ... only when they are whole and fit.
11. **Talk mode** lets an action be taught again (handy while trying things). Files still forbid it.
12. **Script checks are repeatable:** random uses seed 1, `wait` does not wait, files go to a fresh temp folder, and line endings do not count. A `.out` file that shows `ooga booga! problem in` means the script must also end with exit code 1.
13. **Exit codes:** 0 ok (also after `me die`), 1 mistake in the script, 2 wrong command use or file not found, 3 engine bug.

## Limitations (known)

- No classes or "kinds" with their own actions. Use boxes plus actions that take the box.
- C# door: no `ref` / `out` parameters, no indexers by name (use `get_Item`), no events, no generic **types** without the long name (`` List`1[System.String] ``), and no extension methods called as `x.Method()` (call them on their static class, like `System.Linq.Enumerable`).
- Lists of C# things returned by C# are copies. Changing them does not change the C# object.
- Whole numbers are exact up to 9,007,199,254,740,992 (ooga numbers are C# `double`).
- `wait` blocks the whole program (fine for the console, an engine adapter will handle it differently).
- `when` events, `me is`, players, enemies and scenes need a game engine adapter. None exists yet.
- Using a file thing before its `me has` line has run is caught **while running**, not before.
- `bin\ooga.exe` is built on Linux for Windows. **Checked on the user's Windows PC (2026-10-09):** `.\ooga --test examples tests\cases` gave `32 ok, 0 wrong`. Talk mode and VS Code Ctrl+Shift+B have not been reported from Windows yet.

## Tests run

Command: `dotnet test Ooga.sln --blame-hang-timeout 2m` (on Linux, .NET SDK 8.0.131).

Result: **530 passed, 0 failed, 0 skipped** (about 2 seconds).

Also: `ooga --test examples tests/cases` → **32 ok, 0 wrong** (15 examples + 17 cases), run with the published `bin/ooga.dll`.

| Test file | What it covers |
|---|---|
| `LexerParserTests` | tokens, positions, push-in, notes, line endings, parse shapes, precedence in the tree |
| `ExecutionTests` | values, math and precedence, checks, nested conditions, loops, `ask` / `wait` / `random` / `me die` |
| `ActionAndScopeTests` | actions, answers, recursion, scope (inside vs. file, hiding, recursion frames) |
| `CollectionTests` | lists, boxes, items, parts, `each`, `has`, sharing, text letters, `kind of`, `%` |
| `LibraryTests` | every ready-made action, files, arguments, host-added actions, library mistakes |
| `TryUseBridgeTests` | `try` / `oops` / `fail`, `use`, the C# door, and 27 new wrong programs with exact line and column |
| `ActionValueTests` | actions as values, `keep` / `change_each` / `sort_by`, ooga actions as C# delegates, LINQ |
| `TalkTests` | talk sessions and `ooga --talk` |
| `ErrorTests` | 70 wrong programs with exact line, column and message, and the full error report |
| `SafetyTests` | endless loops stop, cancellation, 20,000-deep recursion |
| `ExampleAndCliTests` | every file in `examples/` through the real `ooga` command, with exact output; CLI behaviour |
| `ScriptCaseTests` | every script in `examples/` and `tests/cases/` against its `expected/*.out` (the same check as `ooga --test`) |
| `TestRunnerTests` | the checker itself fails when it should: wrong line, missing expected file, never-ending script, wrong exit code |
| `OogaVsCSharpTests` | each of the 10 C# programs in `docs/ooga-vs-csharp/` shows exactly what its Ooga twin shows, and `OOGA_VS_CSHARP.md` shows exactly that code |

### Script cases (`tests/cases/`)

Expected output for these was written by hand from the rules **before** running them, not copied from what ooga printed.

| Kind | Cases |
|---|---|
| Nested collections | `nested_01_grid` (lists in a list), `nested_02_boxes_in_lists` (boxes in a list, lists in boxes, actions changing them), `nested_03_lookups_and_copies` (box lookup of lists, shallow `copy`, boxes three deep, deep sameness) |
| Recursion | `recursion_01_classic` (factorial, fibonacci, gcd), `recursion_02_walk_nested` (sum, flatten and depth of nested lists), `recursion_03_mutual_and_limit` (two actions calling each other; endless recursion caught with `try`), `recursion_04_hanoi` |
| Imports (`use`) | `use_01_chain` (a used file using another file; each file once), `use_02_shared_things`, `use_03_mistake_in_used_file`, `use_04_missing_file`, `use_05_problem_while_running` (caught, then uncaught, error shows the used file) |
| Failures | `fail_01_uncaught_fail`, `fail_02_try_in_loops` (try per round, a problem inside oops), `fail_03_nested_out_of_range`, `fail_04_found_before_running`, `fail_05_caught_messages` |

**Bug found by these cases and fixed:** when a used file used another file (`lib/shapes` → `math_tools`), ooga looked for the second file from the *shown* name of the first, not its real place. It only worked when ooga was started from the right folder. Now the console host remembers where each used file really is.

Every test runs with a 10-second wall-clock limit and a 2,000,000-step limit.

Also checked by hand with the published `bin/ooga.dll`: all 15 examples (exit code 0, and 1 for `07_mistakes` on purpose), a talk-mode session, nested `use` started from another folder, and that `--test` says WRONG (exit 1) when an expected file is changed on purpose.

## Next recommended tasks

1. **Finish the Windows check**: `.\ooga --test examples tests\cases` already passes on Windows. Still to try there: `.\ooga --talk`, Ctrl+Shift+B in VS Code, and reinstalling the VS Code colours (`vscode-ooga\pack.ps1`) for the new words.
2. **Write a bigger program yourself** (a text adventure with boxes for rooms, or a score tracker with files) and note what felt awkward.
3. **Move from .NET 8 to .NET 10** before 10 November 2026: install the .NET 10 runtime, change `net8.0` to `net10.0` in `Directory.Build.props`, rebuild `bin`.
4. Then the **Godot adapter**: a new project that implements `IOogaHost`, adds game actions through `RunOptions.Actions`, and turns `when` into engine events.
