using System.ComponentModel.DataAnnotations;

namespace CF.Events.Web.ViewModels;

public sealed class EventViewModel
{
    [Required]
    [StringLength(100)]
    public string Name { get; init; } = string.Empty;

    public DateTime Date { get; init; }

    public string? Location { get; init; }

    [StringLength(500)]
    public string? Description { get; init; }

    public IFormFile? InvitationImage { get; init; }
}
