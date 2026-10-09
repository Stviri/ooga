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
