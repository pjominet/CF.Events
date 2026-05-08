using System.ComponentModel.DataAnnotations;

namespace CF.Events.Shared.Models;

public class LoginRequest
{
    [Required]
    public string Password { get; set; } = string.Empty;
}
