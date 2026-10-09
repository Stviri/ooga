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
