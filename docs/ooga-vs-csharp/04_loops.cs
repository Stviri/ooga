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
