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
