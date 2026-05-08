using System.ComponentModel.DataAnnotations;

namespace CF.Events.API.Models;

public class LoginRequest
{
    [Required]
    public string Password { get; set; } = string.Empty;
}
