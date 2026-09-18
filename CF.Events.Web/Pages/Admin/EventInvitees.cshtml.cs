using CF.Events.Web.Data;
using CF.Events.Web.Models;
using CF.Events.Web.Models.Requests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using CF.Events.Web.Infrastructure.Extensions;
using static CF.Events.Web.Infrastructure.Constants;

namespace CF.Events.Web.Pages.Admin;

[Authorize(Roles = Roles.Admin)]
public class EventInviteesModel(EventsDbContext db) : PageModel
{
    public required Event EventData { get; set; }
    public List<SelectListItem> AccommodationCodes { get; private set; } = [];

    public UsersInviteRequest NewInvite { get; set; } = new();

    public List<InviteeRow> Invitees { get; private set; } = [];
    public required Statistics Stats { get; set; }

    public List<SelectListItem> AvailableUsers { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync(int id)
    {
        EventData = await db.Events.FirstAsync(e => e.Id == id);

        AccommodationCodes = [.. EventData.AccommodationCodes.Select(ac => new SelectListItem(ac, ac))];

        var invitedUsers = db.EventUsers
            .Where(ue => ue.EventId == id)
            .Include(ue => ue.User)
            .ThenInclude(u => u.GuestGroup)
            .Select(ue => new { ue.AssignedAccommodationCode, ue.User, InvitationEmailSent = ue.InviteEmailSent, SaveTheDateSent = ue.SaveTheDateEmailSent, ue.ScheduledFor, ue.InvitationPriority })
            .ToList();

        var rsvps = db.Rsvps.Where(r => r.EventId == id).ToList();

        var unavailableUsers = new HashSet<string>();
        Invitees =
        [
            .. invitedUsers.Select(iu =>
                {
                    var user = iu.User;
                    var rsvp = rsvps.FirstOrDefault(r => r.UserId == user.Id);
                    var responded = rsvp?.SubmittedAt > DateTime.MinValue.AddDays(1);
                    var status = responded ? (rsvp?.Attending == true ? AttendanceStatus.Attending : AttendanceStatus.Declined) : AttendanceStatus.Pending;
                    unavailableUsers.Add(user.Id);
                    return new InviteeRow(
                        user.Id,
                        user.DisplayName!,
                        user.Email!,
                        iu.AssignedAccommodationCode,
                        status,
                        iu.InvitationEmailSent,
                        iu.SaveTheDateSent,
                        iu.ScheduledFor,
                        iu.InvitationPriority);
                })
                .OrderBy(i => i.DisplayName)
        ];

        Stats = new Statistics(
            invitedUsers.Count,
            invitedUsers.Sum(iu => iu.User.GuestGroup?.MaxPeople ?? 1),
            Invitees.Count(i => i.Status is AttendanceStatus.Attending),
            Invitees.Count(i => i.Status is AttendanceStatus.Declined)
        );

        AvailableUsers = await (from u in db.Users
                join ur in db.UserRoles on u.Id equals ur.UserId
                join r in db.Roles on ur.RoleId equals r.Id
                where u.IsActive && !unavailableUsers.Contains(u.Id) && r.Name == Roles.Guest
                orderby u.DisplayName
                select new SelectListItem($"{u.DisplayName} ({u.Email})", u.Id))
            .ToListAsync();

        return Page();
    }

    public List<SelectListItem> GetAccommodationCodes(string? currentCode)
    {
        var list = EventData.AccommodationCodes
            .Select(ac => new SelectListItem(ac, ac, ac == currentCode))
            .ToList();
        list.Insert(0, new SelectListItem("none", "", !currentCode.HasValue(false)));
        return list;
    }

    public record InviteeRow(string UserId, string DisplayName, string Email, string? AssignedAccommodationCode, AttendanceStatus Status, DateTime? InvitationEmailSent, DateTime? SaveTheDateSent, DateTime? ScheduledFor, ushort InvitationPriority = 1);

    public record Statistics(int Count, int MaxPeopleSum, int Attending, int Declined);
}

public enum AttendanceStatus
{
    Pending,
    Attending,
    Declined
}
