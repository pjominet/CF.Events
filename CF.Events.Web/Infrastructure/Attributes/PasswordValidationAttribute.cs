using System.ComponentModel.DataAnnotations;
using CF.Events.Web.Infrastructure.Settings;
using Microsoft.Extensions.Options;

namespace CF.Events.Web.Infrastructure.Attributes;

[AttributeUsage(AttributeTargets.Property)]
public class PasswordValidationAttribute(int maxLength = 6) : ValidationAttribute
{
    private int MaxLength { get; } = maxLength;

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        var password = value as string;
        if (string.IsNullOrEmpty(password))
            return ValidationResult.Success;

        var options = validationContext.GetService<IOptions<AppSettings>>();
        var minLength = options?.Value.PasswordLength ?? MaxLength;

        if (password.Length < minLength)
            return new ValidationResult($"The password  must be at least {minLength} characters long.");

        if (password.Length > MaxLength)
            return new ValidationResult($"The password must be at most {MaxLength} characters long.");

        return ValidationResult.Success;
    }
}
