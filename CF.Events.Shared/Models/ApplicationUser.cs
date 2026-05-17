using Microsoft.AspNetCore.Identity;

namespace CF.Events.Shared.Models;

public class ApplicationUser : IdentityUser
{
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}
