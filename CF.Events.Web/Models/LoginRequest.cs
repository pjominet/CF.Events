using System.ComponentModel.DataAnnotations;

namespace CF.Events.Web.Models;

public class LoginRequest
{
    [Required]
    public string Password { get; set; } = string.Empty;
}
