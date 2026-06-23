using System.ComponentModel.DataAnnotations;
using CF.Events.Web.Infrastructure.Settings;
using Microsoft.Extensions.Options;

namespace CF.Events.Web.Infrastructure.Attributes;

[AttributeUsage(AttributeTargets.Property)]
public class PasswordValidationAttribute(int maxLength = 100) : ValidationAttribute
{
    private int MaxLength { get; } = maxLength;

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        var password = value as string;
        if (string.IsNullOrEmpty(password))
            return ValidationResult.Success;

        var options = validationContext.GetService<IOptions<AppSettings>>();
        var environment = validationContext.GetService<IWebHostEnvironment>();

        var minLength = options?.Value.PasswordLength ?? MaxLength;
        var isDevelopment = environment?.IsDevelopment() ?? false;

        if (password.Length < minLength)
            return new ValidationResult($"The password  must be at least {minLength} characters long.");

        if (password.Length > MaxLength)
            return new ValidationResult($"The password must be at most {MaxLength} characters long.");

        if (isDevelopment)
            return ValidationResult.Success;

        if (!password.Any(char.IsDigit))
            return new ValidationResult("The password must contain at least one digit (0-9).");

        if (!password.Any(char.IsLower))
            return new ValidationResult("The password must contain at least one lowercase letter (a-z).");

        if (!password.Any(char.IsUpper))
            return new ValidationResult("The password must contain at least one uppercase letter (A-Z).");

        if (password.All(char.IsLetterOrDigit))
            return new ValidationResult("The password must contain at least one special character.");

        return ValidationResult.Success;
    }
}
