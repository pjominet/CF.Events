namespace CF.Events.Web.Infrastructure;

public static class CodeGenerator
{
    private const string Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

    public static string Generate(int length = 8) =>
        new(Enumerable.Repeat(Alphabet, length)
            .Select(s => s[Random.Shared.Next(s.Length)])
            .ToArray());
}
