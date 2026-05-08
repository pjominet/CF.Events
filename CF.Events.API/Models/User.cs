using System.ComponentModel.DataAnnotations;

namespace CF.Events.API.Models;

public class User
{
    public int Id { get; init; }

    [Required] [StringLength(64)] public string PasswordHash { get; init; } = string.Empty;

    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}
