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
