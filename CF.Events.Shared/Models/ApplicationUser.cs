using Microsoft.AspNetCore.Identity;

namespace CF.Events.Shared.Models;

public class ApplicationUser : IdentityUser
{
    public string? DisplayName { get; set; }
    public bool MustChangePassword { get; set; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime? LastLogin { get; set; }
}
