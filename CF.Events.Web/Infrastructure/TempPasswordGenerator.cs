namespace CF.Events.Web.Infrastructure;

public static class TempPasswordGenerator
{
    private const string Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
    private const int Length = 8;

    public static string Generate() =>
        new(Enumerable.Repeat(Alphabet, Length)
            .Select(s => s[Random.Shared.Next(s.Length)])
            .ToArray());
}
