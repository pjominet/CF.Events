using System.ComponentModel.DataAnnotations;

namespace CF.Events.Web.Models;

public class GuestGroup
{
    public int Id { get; set; }
    [StringLength(500)]
    public required string Label { get; set; }
    [StringLength(450)]
    public required string GuestUserId { get; set; }
    public List<string> Participants { get; set; } = [];

    // navigation properties
    public AppUser GuestUser { get; set; } = null!;
}
