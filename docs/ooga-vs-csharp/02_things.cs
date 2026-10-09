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
