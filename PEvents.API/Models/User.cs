using System.ComponentModel.DataAnnotations;

namespace PEvents.API.Models;

public class User
{
    public int Id { get; set; }

    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
