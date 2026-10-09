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
