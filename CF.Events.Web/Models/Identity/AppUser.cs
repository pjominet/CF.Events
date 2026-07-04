using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace CF.Events.Web.Models;

public class AppUser : IdentityUser
{
    [StringLength(100)]
    public string? DisplayName { get; set; }
    public bool MustChangePassword { get; set; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime? LastLogin { get; set; }
    public bool IsActive { get; set; } = true;

    // navigation properties
    public HashSet<InviteGroup> InvitedPersons { get; set; } = [];
}
