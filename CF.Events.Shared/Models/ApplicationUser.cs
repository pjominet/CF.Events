using Microsoft.AspNetCore.Identity;

namespace CF.Events.Shared.Models;

public class ApplicationUser : IdentityUser
{
    public string? DisplayName { get; set; }
    public bool MustChangePassword { get; set; } = true;
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}
