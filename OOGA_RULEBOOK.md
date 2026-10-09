# Ooga Rulebook (v1)

Run any `.ooga` file: open it in VS Code and press **Ctrl + Shift + B**.

## Basics

- One thing per line.
- Lines **pushed in 4 spaces** belong to the line above them (under `if`, `repeat`, `count`, `me can`, `else`).
- `#` starts a note. Ooga ignores it.
- Text goes in quotes: `"hello"`. Inside text: `\"` = quote, `\n` = new line.

## Things (variables)

| Ooga | Meaning |
|---|---|
| `me has health 100` | make a new thing called health, start at 100 |
| `health is 50` | change health to 50 |
| `health gain 10` | add 10 |
| `health lose 10` | take away 10 |
| `name gain "!"` | stick text on the end |

Values: numbers `5`, `3.5` · text `"hi"` · `yes` / `no` · `nothing`

Math: `+  -  *  /` and `( )`. Text `+` anything = joined text: `"hp: " + health`

Numbers show without extra zeros: `7 / 2` shows `3.5`, `4 / 2` shows `2`.

### Which part happens first

From first to last:

1. `( )`
2. `-` in front of a value (`-5`, `-health`)
3. `*` and `/`
4. `+` and `-`
5. checks with `is` (`a + 1 is big b * 2` does the math on both sides first)
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
| `and`, `or`, `not` | combine checks |

## Doing things many times

```text
repeat 3
    say "ooga"

repeat while health is big 0
    health lose 10

count i from 1 to 10
    say i
```

- `stop`: leave the loop now
- `skip`: jump to the next round of the loop

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
  ```
- When an action's answer goes into math, wrap it in `( )`: `say (me double 5) + 1`.

### Where things live

- Things made with `me has` at the left edge, or inside `if` / `repeat` / `count` that are not inside an action, belong to **the whole file**.
- Actions can see and change those file things.
- Things handed to an action, and things made with `me has` inside an action, stay **inside that action**. They disappear when the action ends.
  If one has the same name as a file thing, the inside one wins, and the file thing is not touched.
- Each `me has NAME` line can appear once per file (or once per action). To change a thing later, write `NAME is ...`.
- A `me has` line inside a loop runs again each round. That is fine.

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

Ooga reads and checks the whole file first. Typos in names, missing `me has`, wrong number of things for an action,
and `stop` / `skip` / `give` in the wrong place are all found **before** the first line runs.
Some problems can only be found while running (like `split by 0`). Then the lines before it have already run.

## Special words (no can use as names)

`me is has can die say if else repeat while count from to stop skip give wait when and or not same big small yes no nothing ask random gain lose`
