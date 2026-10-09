# Ooga next to C#

The same 10 programs, written in Ooga and in C#. Each pair shows **exactly the same output**: a test runs both languages and compares them line by line, so nothing here is guessed.

Ooga itself is run by a C# program, so learning Ooga is a good first step towards C#. The ideas are the same; C# just writes them with more symbols.

Every code block below is copied from a real file in [`docs/ooga-vs-csharp/`](docs/ooga-vs-csharp/). Run the Ooga side yourself with `.\ooga --test docs\ooga-vs-csharp`.

> The C# files only show the class. A real C# program also needs `using System;` and `using System.Collections.Generic;` at the top, unless the project turns on *implicit usings* (new .NET projects do).

## Cheat sheet

| Idea | Ooga | C# |
| --- | --- | --- |
| show a line | `say x` | `Console.WriteLine(x);` |
| make a thing | `me has hp 100` | `int hp = 100;` |
| change a thing | `hp is 5` · `hp gain 1` · `hp lose 1` | `hp = 5;` · `hp += 1;` · `hp -= 1;` |
| same / not same | `a is b` · `a is not b` | `a == b` · `a != b` |
| bigger / smaller | `a is big b` · `a is small or same b` | `a > b` · `a <= b` |
| and / or / not | `and` · `or` · `not` | `&&` · `\|\|` · `!` |
| a block | push lines in 4 spaces | `{ ... }` |
| do N times | `repeat 3` | `for (int k = 0; k < 3; k++)` |
| count | `count i from 1 to 3` | `for (int i = 1; i <= 3; i++)` |
| loop while | `repeat while x is small 10` | `while (x < 10)` |
| leave / next round | `stop` · `skip` | `break;` · `continue;` |
| teach an action | `me can double x` | `static int Double(int x) { ... }` |
| answer | `give x * 2` | `return x * 2;` |
| use an action | `me double 5` | `Double(5)` |
| list | `list 1 2 3` | `new List<int> { 1, 2, 3 }` |
| first item | `item 1 of bag` | `bag[0]` |
| size | `size of bag` | `bag.Count` |
| box / object | `box name "grok" hp 100` | `new Player { Name = "grok", Hp = 100 }` |
| part of a box | `hp of player` | `player.Hp` |
| catch problems | `try` / `oops why` | `try { } catch (Exception why) { }` |
| make a problem | `fail "bad"` | `throw new Exception("bad");` |
| ask | `me has n ask "n?"` | `Console.Write("n? "); var n = Console.ReadLine();` |
| note | `# note` | `// note` |

## 1. Showing text (print)

<table>
<tr><th>Ooga</th><th>C#</th></tr>
<tr>
<td valign="top">

```text
say "ooga booga"
say "hello " + "grok"
say 7 / 2
```

</td>
<td valign="top">

```csharp
public static class Say
{
    public static void Main()
    {
        Console.WriteLine("ooga booga");
        Console.WriteLine("hello " + "grok");
        Console.WriteLine(7.0 / 2);
    }
}
```

</td>
</tr>
</table>

**Both show:**

```text
ooga booga
hello grok
3.5
```

**Differences:**

- **Show a line:** Ooga says `say`. C# says `Console.WriteLine(...)`.
- **Brackets and semicolons:** C# needs `( )` around what it shows and a `;` at the end of every line. Ooga needs neither.
- **The wrapper:** C# code has to live inside a `class` and a `Main` method. Ooga starts at the first line of the file.
- **Splitting numbers:** in C#, `7 / 2` is `3`, because whole numbers drop the `.5`. You must write `7.0 / 2` to get `3.5`. In Ooga, `7 / 2` is always `3.5`.

## 2. Things (variables)

<table>
<tr><th>Ooga</th><th>C#</th></tr>
<tr>
<td valign="top">

```text
me has health 100
me has name "grok"
health lose 30
health gain 5
name gain "!"
health is health * 2
say name + " has " + health
```

</td>
<td valign="top">

```csharp
public static class Things
{
    public static void Main()
    {
        int health = 100;
        string name = "grok";
        health -= 30;
        health += 5;
        name += "!";
        health = health * 2;
        Console.WriteLine(name + " has " + health);
    }
}
```

</td>
</tr>
</table>

**Both show:**

```text
grok! has 150
```

**Differences:**

- **Making a thing:** Ooga says `me has health 100`. C# says `int health = 100;` and must name the *kind* first: `int` for whole numbers, `string` for text.
- **Changing it:** Ooga's `health is 50`, `gain` and `lose` are C#'s `=`, `+=` and `-=`.
- **Changing kinds:** in Ooga a thing can hold a number now and text later. In C#, an `int` can only ever hold whole numbers.
- **One meaning per word:** C# uses `=` to change a thing and `==` to compare. Ooga uses `is` for both, and the place on the line decides which.

## 3. Checks (if / else)

<table>
<tr><th>Ooga</th><th>C#</th></tr>
<tr>
<td valign="top">

```text
me has health 20
if health is small or same 0
    say "dead"
else if health is small 30 and health is not 13
    say "hurt"
else
    say "fine"
```

</td>
<td valign="top">

```csharp
public static class Checks
{
    public static void Main()
    {
        int health = 20;
        if (health <= 0)
        {
            Console.WriteLine("dead");
        }
        else if (health < 30 && health != 13)
        {
            Console.WriteLine("hurt");
        }
        else
        {
            Console.WriteLine("fine");
        }
    }
}
```

</td>
</tr>
</table>

**Both show:**

```text
hurt
```

**Differences:**

- **Same words:** both have `if`, `else if` and `else`.
- **Blocks:** C# marks a block with `{ }` and puts the check in `( )`. Ooga marks a block by pushing lines in 4 spaces.
- **Check words:** Ooga writes words where C# writes symbols: `is small or same` is `<=`, `is small` is `<`, `is not` is `!=`, `is` is `==`, `and` is `&&`, `or` is `||`, `not` is `!`.

## 4. Loops

<table>
<tr><th>Ooga</th><th>C#</th></tr>
<tr>
<td valign="top">

```text
repeat 2
    say "ooga"
count i from 1 to 3
    say i
count i from 3 to 1
    say i
me has n 0
repeat while n is small 10
    n gain 3
    if n is 6
        skip
    if n is 9
        stop
    say n
```

</td>
<td valign="top">

```csharp
public static class Loops
{
    public static void Main()
    {
        for (int k = 0; k < 2; k++)
        {
            Console.WriteLine("ooga");
        }
        for (int i = 1; i <= 3; i++)
        {
            Console.WriteLine(i);
        }
        for (int i = 3; i >= 1; i--)
        {
            Console.WriteLine(i);
        }
        int n = 0;
        while (n < 10)
        {
            n += 3;
            if (n == 6)
            {
                continue;
            }
            if (n == 9)
            {
                break;
            }
            Console.WriteLine(n);
        }
    }
}
```

</td>
</tr>
</table>

**Both show:**

```text
ooga
ooga
1
2
3
3
2
1
3
```

**Differences:**

- **Do it N times:** Ooga's `repeat 2` is C#'s `for (int k = 0; k < 2; k++)`, which counts from 0 and stops before 2.
- **Count up or down:** Ooga's `count i from 3 to 1` works out the direction by itself. C# must say `i--` to go down, and `>=` to stop at 1.
- **Loop while true:** Ooga's `repeat while` is C#'s `while`.
- **Skip and stop:** Ooga's `skip` is C#'s `continue`, and `stop` is `break`.

## 5. Actions (functions)

<table>
<tr><th>Ooga</th><th>C#</th></tr>
<tr>
<td valign="top">

```text
me can greet who
    say "ooga, " + who + "!"

me can double x
    give x * 2

me can factorial n
    if n is small or same 1
        give 1
    give n * (me factorial (n - 1))

me greet "zug"
say me double 21
say (me double 5) + 1
say me factorial 5
```

</td>
<td valign="top">

```csharp
public static class Actions
{
    static void Greet(string who)
    {
        Console.WriteLine("ooga, " + who + "!");
    }

    static int Double(int x)
    {
        return x * 2;
    }

    static int Factorial(int n)
    {
        if (n <= 1)
        {
            return 1;
        }
        return n * Factorial(n - 1);
    }

    public static void Main()
    {
        Greet("zug");
        Console.WriteLine(Double(21));
        Console.WriteLine(Double(5) + 1);
        Console.WriteLine(Factorial(5));
    }
}
```

</td>
</tr>
</table>

**Both show:**

```text
ooga, zug!
42
11
120
```

**Differences:**

- **Teaching an action:** Ooga's `me can double x` is a C# *method*: `static int Double(int x)`. C# names the kind of every thing handed in (`int x`) and of the answer (`int`, or `void` for no answer).
- **Giving an answer:** Ooga's `give` is C#'s `return`.
- **Using an action:** Ooga writes `me double 21`. C# writes `Double(21)`, with the things in `( )` and split by `,`.
- **Math in what you hand over:** C#'s brackets show where the things end, so `Factorial(n - 1)` just works. Ooga has no brackets there, so math needs `( )`: `me factorial (n - 1)`.
- **Order:** both can use an action above the place it is taught.

## 6. Lists

<table>
<tr><th>Ooga</th><th>C#</th></tr>
<tr>
<td valign="top">

```text
me has scores list 40 75 12
scores gain 99
say item 1 of scores
say size of scores
say scores has 75
item 2 of scores is 80
me has total 0
each s in scores
    total gain s
say total
```

</td>
<td valign="top">

```csharp
public static class Lists
{
    public static void Main()
    {
        List<int> scores = new List<int> { 40, 75, 12 };
        scores.Add(99);
        Console.WriteLine(scores[0]);
        Console.WriteLine(scores.Count);
        Console.WriteLine(scores.Contains(75) ? "yes" : "no");
        scores[1] = 80;
        int total = 0;
        foreach (int s in scores)
        {
            total += s;
        }
        Console.WriteLine(total);
    }
}
```

</td>
</tr>
</table>

**Both show:**

```text
40
4
yes
231
```

**Differences:**

- **Counting from 1:** this is the biggest difference. Ooga's first item is `item 1`. C#'s first item is `scores[0]`, because C# counts from 0. So Ooga's `item 2` is C#'s `[1]`.
- **Making a list:** Ooga writes `list 40 75 12`. C# writes `new List<int> { 40, 75, 12 }`, and the list can only hold the kind named in `< >`.
- **Adding, measuring, checking:** Ooga's `gain`, `size of` and `has` are C#'s `.Add(...)`, `.Count` and `.Contains(...)`.
- **Yes and no:** C# shows a check as `True` or `False`. To print `yes` like Ooga, the C# side writes `? "yes" : "no"`.
- **Walking through:** Ooga's `each s in scores` is C#'s `foreach (int s in scores)`.

## 7. Boxes (objects)

<table>
<tr><th>Ooga</th><th>C#</th></tr>
<tr>
<td valign="top">

```text
me has player box name "grok" health 100
health of player lose 30
speed of player is 5
say name of player + " has " + health of player
say speed of player
```

</td>
<td valign="top">

```csharp
public class Player
{
    public string Name = "";
    public int Health;
    public int Speed;
}

public static class Boxes
{
    public static void Main()
    {
        Player player = new Player { Name = "grok", Health = 100 };
        player.Health -= 30;
        player.Speed = 5;
        Console.WriteLine(player.Name + " has " + player.Health);
        Console.WriteLine(player.Speed);
    }
}
```

</td>
</tr>
</table>

**Both show:**

```text
grok has 70
5
```

**Differences:**

- **Planning ahead:** in C#, a box's parts must be planned first, as a `class` that lists every part and its kind. In Ooga, you make the box and its parts on the spot.
- **Adding parts later:** Ooga can add `speed` to a box any time. A C# object can only have the parts its class lists, so `Speed` had to be in the class from the start.
- **Reading a part:** Ooga reads a part as `health of player`, while C# writes `player.Health`. The order of the words is turned around.

## 8. Problems (try / catch)

<table>
<tr><th>Ooga</th><th>C#</th></tr>
<tr>
<td valign="top">

```text
me can withdraw amount
    if amount is big 50
        fail "not enough shells"
    give 50 - amount

try
    say me withdraw 20
    say me withdraw 80
    say "never"
oops why
    say "oops: " + why
say "after"
```

</td>
<td valign="top">

```csharp
public static class Problems
{
    static int Withdraw(int amount)
    {
        if (amount > 50)
        {
            throw new Exception("not enough shells");
        }
        return 50 - amount;
    }

    public static void Main()
    {
        try
        {
            Console.WriteLine(Withdraw(20));
            Console.WriteLine(Withdraw(80));
            Console.WriteLine("never");
        }
        catch (Exception why)
        {
            Console.WriteLine("oops: " + why.Message);
        }
        Console.WriteLine("after");
    }
}
```

</td>
</tr>
</table>

**Both show:**

```text
30
oops: not enough shells
after
```

**Differences:**

- **Catching problems:** Ooga's `try` / `oops why` is C#'s `try` / `catch (Exception why)`. In C#, the text of the problem is `why.Message`; in Ooga, `why` *is* the text.
- **Making your own problem:** Ooga's `fail "..."` is C#'s `throw new Exception("...")`.
- **Same flow:** in both, the lines after the problem (`"never"`) are skipped, and the program goes on after the catch block.

## 9. Asking the person (input)

<table>
<tr><th>Ooga</th><th>C#</th></tr>
<tr>
<td valign="top">

```text
me has name ask "your name?"
me has age ask "your age?"
say name + " is " + (age + 1) + " next year"
```

</td>
<td valign="top">

```csharp
public static class Asking
{
    public static void Main()
    {
        Console.Write("your name? ");
        string name = Console.ReadLine();
        Console.Write("your age? ");
        int age = int.Parse(Console.ReadLine());
        Console.WriteLine(name + " is " + (age + 1) + " next year");
    }
}
```

</td>
</tr>
</table>

**Both show:**

```text
your name? your age? Grok is 31 next year
```

Typed answers: `Grok`, then `30`. In a real window each answer shows after its question; this is what a check sees.

**Differences:**

- **Asking:** Ooga's `ask "your name?"` shows the question and waits. C# needs two steps: `Console.Write(...)` to show the question, and `Console.ReadLine()` to wait for the answer.
- **Number answers:** C# always gets text back, and `int.Parse` turns it into a number. Ooga turns a typed number into a number by itself. If you type a word, C#'s `int.Parse` stops the program with a problem, while Ooga keeps it as text.

## 10. A whole program: FizzBuzz

<table>
<tr><th>Ooga</th><th>C#</th></tr>
<tr>
<td valign="top">

```text
count i from 1 to 15
    if i % 15 is 0
        say "fizzbuzz"
    else if i % 3 is 0
        say "fizz"
    else if i % 5 is 0
        say "buzz"
    else
        say i
```

</td>
<td valign="top">

```csharp
public static class FizzBuzz
{
    public static void Main()
    {
        for (int i = 1; i <= 15; i++)
        {
            if (i % 15 == 0)
            {
                Console.WriteLine("fizzbuzz");
            }
            else if (i % 3 == 0)
            {
                Console.WriteLine("fizz");
            }
            else if (i % 5 == 0)
            {
                Console.WriteLine("buzz");
            }
            else
            {
                Console.WriteLine(i);
            }
        }
    }
}
```

</td>
</tr>
</table>

**Both show:**

```text
1
2
fizz
4
buzz
fizz
7
8
fizz
buzz
11
fizz
13
14
fizzbuzz
```

**Differences:**

- **Same program, different size:** both programs follow the same steps. The C# version is about twice as many lines, mostly `{ }` and the `class` / `Main` wrapper.
- **Remainder:** `%` (what is left over after splitting) is the same in both.
