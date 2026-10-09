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
