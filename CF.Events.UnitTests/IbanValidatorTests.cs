using CF.Events.Web.Infrastructure.Validators;

namespace CF.Events.UnitTests;

public class IbanValidatorTests
{
    [Theory]
    [InlineData("GB29 NWBK 6016 1331 9268 19")] // UK valid
    [InlineData("GB29NWBK60161331926819")] // UK valid no spaces
    [InlineData("gb29 nwbk 6016 1331 9268 19")] // Lowercase
    [InlineData("DE89 3704 0044 0532 0130 00")] // Germany valid
    [InlineData("FR76 3000 6000 0112 3456 7890 189")] // France valid
    [InlineData("IT60 X054 2811 1010 0000 0123 456")] // Italy valid
    [InlineData("NL91 ABNA 0417 1643 00")] // Netherlands valid
    [InlineData("BE71 0961 2345 6769")] // Belgium valid
    public void IsValid_ShouldReturnTrue_ForValidIbans(string iban)
    {
        var result = IbanValidator.IsValid(iban);
        Assert.True(result, $"Expected IBAN {iban} to be valid.");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("GB28 NWBK 6016 1331 9268 19")] // Invalid check digits (GB28 instead of GB29)
    [InlineData("ABC")] // Too short
    [InlineData("GB29 NWBK 6016 1331 9268 19 1234567890")] // Too long (general range)
    [InlineData("GB29 NWBK 6016 1331 9268 1@")] // Invalid characters
    [InlineData("1129 NWBK 6016 1331 9268 19")] // Doesn't start with letters
    [InlineData("DE89 3704 0044 0532 0130 00 12")] // Germany invalid length (too long)
    [InlineData("NL91 ABNA 0417 1643 0")] // Netherlands invalid length (too short)
    [InlineData("FR76 3000 6000 0112 3456 7890 18")] // France invalid length (too short)
    [InlineData("XX12 3456 7890 1234 5678 90")] // Unknown country code (fallback to format check)
    public void IsValid_ShouldReturnFalse_ForInvalidIbans(string? iban)
    {
        var result = IbanValidator.IsValid(iban);
        Assert.False(result, $"Expected IBAN {iban} to be invalid.");
    }
}
