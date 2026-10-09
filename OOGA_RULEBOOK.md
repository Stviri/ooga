# Ooga Rulebook (v2)

Run any `.ooga` file: open it in VS Code and press **Ctrl + Shift + B**, or type `.\ooga file.ooga`.
Try things out line by line: `.\ooga --talk`.

## Basics

- One thing per line.
- Lines **pushed in 4 spaces** belong to the line above them (under `if`, `repeat`, `count`, `each`, `try`, `me can`, `else`, `oops`).
- `#` starts a note. Ooga ignores it.
- Text goes in quotes: `"hello"`. Inside text: `\"` = quote, `\n` = new line, `\t` = tab, `\\` = backslash.

## Things (variables)

| Ooga | Meaning |
|---|---|
| `me has health 100` | make a new thing called health, start at 100 |
| `health is 50` | change health to 50 |
| `health gain 10` | add 10 |
| `health lose 10` | take away 10 |
| `name gain "!"` | stick text on the end |

Values: numbers `5`, `3.5` · text `"hi"` · `yes` / `no` · `nothing` · lists · boxes · actions

Math: `+  -  *  /  %` and `( )`. `%` is what is left over after splitting: `17 % 5` is `2`.
Text `+` anything = joined text: `"hp: " + health`. List `+` list = one longer list.

Numbers show without extra zeros: `7 / 2` shows `3.5`, `4 / 2` shows `2`.

### Which part happens first

From first to last:

1. `( )`
2. `-` in front of a value (`-5`, `-health`)
3. `*`, `/` and `%`
4. `+` and `-`
5. checks with `is` or `has` (`a + 1 is big b * 2` does the math on both sides first)
6. `not`
7. `and`
8. `or`

Same level goes left to right: `10 - 4 - 3` is `3`.
So `yes or yes and no` is `yes`, because `and` happens before `or`.

## Talking

| Ooga | Meaning |
|---|---|
| `say "hello"` | show something |
| `me has name ask "your name?"` | ask the person, store the answer |
| `random 1 to 6` | random whole number, 1 to 6 |
| `wait 2` | pause 2 seconds |

`ask` gives a number if they typed a plain number (`5`, `-3`, `2.5`), otherwise text.

## Checks

```text
if health is small or same 0
    say "dead"
else if health is small 30
    say "hurt"
else
    say "fine"
```

| Ooga | Meaning |
|---|---|
| `a is b` / `a is same b` | same |
| `a is not b` | not same |
| `a is big b` | a bigger than b |
| `a is small b` | a smaller than b |
| `a is big or same b` | bigger or same |
| `a is small or same b` | smaller or same |
| `bag has 5` | the list (or box, or text) has it inside |
| `kind of x` | what kind x is: `"number"`, `"text"`, `"yes or no"`, `"nothing"`, `"list"`, `"box"`, `"action"`, `"csharp"` |
| `and`, `or`, `not` | combine checks |

Big and small work on numbers, and on texts in letter order (`"apple" is small "banana"`).
Lists and boxes are the same when everything inside is the same.

## Doing things many times

```text
repeat 3
    say "ooga"

repeat while health is big 0
    health lose 10

count i from 1 to 10
    say i

each fruit in list "apple" "berry"
    say fruit
```

- `stop`: leave the loop now
- `skip`: jump to the next round of the loop
- `each` works on a list (every item), a text (every letter) and a box (every part name).

## Lists

```text
me has bag list 1 2 3        # a list. "list" alone is an empty list
say item 1 of bag            # 1. items are counted 1, 2, 3...
item 2 of bag is 20          # change one item
bag gain 4                   # add to the end
bag lose 20                  # take out the first 20
say size of bag              # how many things
if bag has 3                 # check inside
    say "has 3"
```

- Lists can hold anything, even other lists: `list 1 "two" (list 3 4)`.
- Lists are **shared**: `me has other bag` gives the same list a second name. Changing one changes both.
  To get a separate list, use `me copy bag`.
- `item k of bag`: a plain name right after `item` is the position. To use a part as the position, wrap it: `item (pos of p) of bag`.

## Boxes

A box is one thing with named parts. Good for a player, an enemy, a save file.

```text
me has player box name "grok" health 100
say health of player         # 100
health of player lose 30     # change a part
speed of player is 5         # add a new part
say player                   # box name "grok" health 70 speed 5
```

- Boxes inside boxes: `x of pos of player`.
- A box can also be a lookup by text: `item "grok" of ages is 30`, `say item "grok" of ages`, `if ages has "grok"`.
- `me keys ages` gives a list of the part names. `me take ages "grok"` takes a part out.
- Boxes are **shared** like lists. `me copy player` makes a separate one.

## Actions (teach me new things)

```text
me can greet who
    say "ooga, " + who

me can double x
    give x * 2

me greet "grok"
say me double 21
say (me double 5) + 1
```

- `me can NAME things...` teaches an action (must be at the left edge).
- `me NAME things...` uses it. You can use an action above the line that teaches it.
- `give` hands back an answer and leaves the action right away. `give` alone, or no `give`, answers `nothing`.
- Each thing you hand to an action is **one value**. Math needs `( )`:

  ```text
  me hit (5 + 1)          # hit gets 6
  me factorial (n - 1)    # right
  me factorial n - 1      # wrong: this is (me factorial n) - 1
  me double (me double 2) # an answer handed to another action also needs ( )
  me shout ("hi " + name) # same for text
  ```
- When an action's answer goes into math, wrap it in `( )`: `say (me double 5) + 1`.
- An action that gets a list or box can change it (they are shared).

### Actions as values

```text
me has f action double       # the action itself, not its answer
say me call f 21             # 42
say me keep (list 1 2 3 4) (action is_even)
```

### Where things live

- Things made with `me has` at the left edge, or inside `if` / `repeat` / `count` / `each` / `try` that are not inside an action, belong to **the whole file**.
- Actions can see and change those file things.
- Things handed to an action, and things made with `me has` inside an action, stay **inside that action**. They disappear when the action ends.
  If one has the same name as a file thing, the inside one wins, and the file thing is not touched.
- Each `me has NAME` line can appear once per file (or once per action). To change a thing later, write `NAME is ...`.
- A `me has` line inside a loop runs again each round. That is fine.

## When things go wrong: try, oops, fail

```text
try
    me has n me number (me read_file "save.txt")
oops why
    say "could not read: " + why
```

- If any line under `try` has a problem, ooga jumps to `oops`. `why` (any name you pick, or none) holds the problem text.
- `fail "reason"` makes your own problem. Without a `try` around it, the program stops and shows the reason.
- `me die` is not a problem. `try` does not stop it.

## More files: use

```text
use "tools"          # brings in tools.ooga from the same folder (".ooga" can be left off)
```

- Actions and things from that file can now be used. Each file is only brought in once.
- `use` must be at the left edge.

## Actions ooga already knows

Use them like your own: `say me round 2.5`. Their names can not be taught again.

| Numbers | Gives |
|---|---|
| `me round x` | nearest whole number (`2.5` → `3`) |
| `me round_to x places` | `me round_to 3.14159 2` → `3.14` |
| `me round_down x` / `me round_up x` | whole number below / above |
| `me positive x` | x without minus (`-4` → `4`) |
| `me power a b` | a times itself b times (`2 10` → `1024`) |
| `me root x` | square root |
| `me biggest a b` / `me smallest a b` | bigger / smaller one. Also works with one list: `me biggest scores` |
| `me numbers a b` | list of whole numbers from a to b |

| Text | Gives |
|---|---|
| `me upper t` / `me lower t` | BIG / small letters |
| `me trim t` | text without spaces at the ends |
| `me split t sep` | list of pieces: `me split "a,b" ","`. `""` splits into letters |
| `me join bag sep` | one text: `me join (list 1 2) "-"` → `1-2` |
| `me replace t old new` | text with old swapped for new |
| `me starts_with t start` / `me ends_with t end` | yes or no |
| `me number t` | the number in a text, or `nothing` if it is not a number |
| `me text x` | x as text, the way `say` shows it |

| Lists (some also texts) | Gives |
|---|---|
| `me piece x from to` | part of a text or list: `me piece "hello" 2 4` → `ell` |
| `me find x what` | position of what (0 when not there) |
| `me reverse x` | text or list backwards |
| `me sort bag` | sorted copy (only numbers, or only texts) |
| `me take bag pos` | takes the item out of the list (or a part out of a box) and gives it |
| `me put_at bag pos x` | puts x in at that position |
| `me keys box` | list of a box's part names |
| `me copy x` | a separate copy of a list or box |
| `me pick bag` | one random item |
| `me shuffle bag` | mixed-up copy |

| Actions as values | Gives |
|---|---|
| `me call f things...` | runs the action held in f |
| `me keep bag f` | list of the items where f gives `yes` |
| `me change_each bag f` | list of what f gives for each item |
| `me sort_by bag f` | copy sorted by what f gives for each item |

| Outside world | Gives |
|---|---|
| `me time` | seconds since the program started |
| `me date` | now, like `2026-10-09 17:45:03` |
| `me arguments` | list of words typed after the file name: `.\ooga game.ooga easy 3` |
| `me read_file path` | the text inside a file |
| `me write_file path x` | puts x in the file (old text is replaced) |
| `me add_to_file path x` | adds x at the end of the file |
| `me file_exists path` | yes or no |

## The C# door

Ooga can use anything C# and .NET have. Write the full C# name in quotes.

| Ooga | C# |
|---|---|
| `me csharp "System.Math" "Sqrt" 16` | `Math.Sqrt(16)` (static method) |
| `me csharp "System.Math" "PI"` | `Math.PI` (static property or field) |
| `me csharp_new "System.Text.StringBuilder"` | `new StringBuilder()` |
| `me csharp_call sb "Append" "hi"` | `sb.Append("hi")` |
| `me csharp_get "hello" "Length"` | `"hello".Length` |
| `me csharp_set thing "Name" "grok"` | `thing.Name = "grok"` |

- Numbers, texts, yes/no and nothing turn into what C# wants (int, double, string, ...), and back.
- Ooga lists turn into C# arrays and lists. C# arrays, lists and LINQ answers come back as ooga lists. C# dictionaries come back as boxes.
- A thing made with `csharp_new` stays a C# thing (even a C# List), so you can keep calling its methods.
- An ooga action can be handed where C# wants a Func, Action, Predicate or Comparison: `(action is_even)`.
- Generic methods (like `Array.Find` or LINQ `Where`) guess their type from what you hand them.
- Generic types need the C# long name: `"System.Collections.Generic.List`1[System.String]"`.
- Problems inside C# become ooga problems ("C# say problem: ...") that `try` can catch.

## Other

| Ooga | Meaning |
|---|---|
| `me is player` | say what kind of thing this file is. In the console it does nothing yet (game engines use it later) |
| `me die` | end right now (this is a normal ending, not an error) |
| `when ...` | saved for game events. Not working yet: ooga tells you so if you use it |

## When ooga finds a mistake

```text
ooga booga! problem in my_scripts\game.ooga line 7, column 5:
    say heath
        ^
no thing called "heath". you mean "health"?
```

Ooga reads and checks the whole file first (and every file it uses). Typos in names, missing `me has`, wrong number of things for an action,
and `stop` / `skip` / `give` in the wrong place are all found **before** the first line runs.
Some problems can only be found while running (like `split by 0`). Then the lines before it have already run.

## Special words (no can use as names)

`me is has can die say if else repeat while count from to stop skip give wait when and or not same big small yes no nothing ask random gain lose list box of item size kind each in try oops fail use action`

Names of actions ooga already knows (like `round` or `split`) can still be used as names of **things**, just not taught as new actions.
