using System.Security.Cryptography;

namespace CF.Events.Web.Infrastructure;

public static class CodeGenerator
{
    private const string Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";

    public static string Generate(int length = 8)
    {
        var bytes = new byte[length];
        RandomNumberGenerator.Fill(bytes);
        return new string(bytes.Select(b => Alphabet[b % Alphabet.Length]).ToArray());
    }
}
