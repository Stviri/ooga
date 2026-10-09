# Ooga Rulebook (v0)

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

## Talking

| Ooga | Meaning |
|---|---|
| `say "hello"` | show something |
| `me has name ask "your name?"` | ask the person, store the answer |
| `random 1 to 6` | random whole number, 1 to 6 |
| `wait 2` | pause 2 seconds |

`ask` gives a number if they typed a number, otherwise text.

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
- `me NAME things...` uses it.
- `give` hands back an answer.
- Things made with `me has` inside an action stay inside that action.
- When an action's answer goes into math, wrap it in `( )`.

## Other

| Ooga | Meaning |
|---|---|
| `me is player` | say what kind of thing this file is (used with Godot later) |
| `me die` | end right now |
| `when ...` | saved for Godot game events, not working yet |

## Special words (no can use as names)

`me is has can die say if else repeat while count from to stop skip give wait when and or not same big small yes no nothing ask random gain lose`
