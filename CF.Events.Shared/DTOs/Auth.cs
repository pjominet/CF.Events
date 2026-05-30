using System.ComponentModel.DataAnnotations;

namespace CF.Events.Shared.DTOs;

public class RegisterRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(100, MinimumLength = 3)]
    public string DisplayName { get; set; } = string.Empty;

    [Required]
    [StringLength(100, MinimumLength = 6)]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [DataType(DataType.Password)]
    [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

public class RegisterResponse
{
    public bool Success { get; init; }
}

public class LoginRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;
}

public class LoginResponse
{
    public bool Success { get; init; }
    public string? Token { get; init; }
    public string? RefreshToken { get; init; }
    public string? Email { get; init; }
    public bool MustChangePassword { get; init; }
}

public class AuthResponse
{
    public bool Success { get; init; }
    public string? Token { get; init; }
    public string? RefreshToken { get; init; }
    public string? Error { get; init; }
    public string? Email { get; init; }
    public bool MustChangePassword { get; init; }
}

public class SetupAccountRequest
{
    [Required]
    [DataType(DataType.Password)]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required]
    [StringLength(100, MinimumLength = 8)]
    [DataType(DataType.Password)]
    public string NewPassword { get; set; } = string.Empty;

    [DataType(DataType.Password)]
    [Compare("NewPassword", ErrorMessage = "The password and confirmation password do not match.")]
    public string ConfirmNewPassword { get; set; } = string.Empty;
}

public class UserDto
{
    public string Id { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public IEnumerable<string> Roles { get; init; } = [];
}

public class UpdatePasswordRequest
{
    [Required]
    [DataType(DataType.Password)]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required]
    [StringLength(100, MinimumLength = 8)]
    [DataType(DataType.Password)]
    public string NewPassword { get; set; } = string.Empty;

    [DataType(DataType.Password)]
    [Compare("NewPassword", ErrorMessage = "The new password and confirmation password do not match.")]
    public string ConfirmNewPassword { get; set; } = string.Empty;
}
