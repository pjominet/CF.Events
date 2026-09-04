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

    public int? GuestGroupId { get; set; }

    // navigation properties
    public GuestGroup? GuestGroup { get; set; }
    public List<EventUser> UserEvents { get; set; } = [];
    public List<AuthCode> InviteCodes { get; set; } = [];
    public List<LoginAudit> LoginAudits { get; set; } = [];
    public List<Feedback> GivenFeedbacks { get; set; } = [];
}
