using System.ComponentModel.DataAnnotations;
using CF.Events.Web.Data;
using CF.Events.Web.Infrastructure.Extensions;
using CF.Events.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using NToastNotify;

namespace CF.Events.Web.Pages.Events;

[Authorize]
public class RsvpModel(EventsDbContext db, IToastNotification toastNotification) : PageModel
{
    public required Event EventData { get; set; }
    public bool HasResponded { get; private set; }
    public bool RespondedAttending { get; private set; }
    public string? AssignedAccommodationCode { get; private set; }
    public List<string> GroupParticipants { get; private set; } = [];

    [BindProperty]
    public InputModel NewRsvp { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int eventId)
    {
        var userId = User.GetId();
        var user = await db.Users.Include(u => u.GuestGroup).FirstAsync(u => u.Id == userId);
        GroupParticipants = user.GuestGroup?.Participants ?? (user.DisplayName != null ? [user.DisplayName] : []);

        var userEvent = await db.EventUsers.FirstOrDefaultAsync(r => r.EventId == eventId && r.UserId == userId);
        if (userEvent is null && !User.IsAdmin())
        {
            toastNotification.AddWarningToastMessage("You are not invited to this event");
            return Redirect("/");
        }

        var rsvp = await db.Rsvps
            .Include(r => r.ParticipantsDiets)
            .Include(r => r.ParticipantsAttendance)
            .FirstOrDefaultAsync(r => r.EventId == eventId && r.UserId == userId);

        EventData = await db.Events
            .Include(e => e.BookingLinks)
            .FirstAsync(e => e.Id == eventId);

        AssignedAccommodationCode = userEvent?.AssignedAccommodationCode;
        HasResponded = rsvp?.SubmittedAt > DateTime.MinValue.AddDays(1);
        RespondedAttending = rsvp?.Attending ?? false;

        if (rsvp is not null)
        {
            NewRsvp = new InputModel
            {
                Participants = user.GuestGroup?.Participants ?? (user.DisplayName is not null ? [user.DisplayName] : []),
                Attending = rsvp.Attending,
                ParticipantsAttendance =
                [
                    .. rsvp.ParticipantsAttendance.Select(pa => new ParticipantAttendance
                    {
                        Id = pa.Id,
                        EventId = pa.EventId,
                        UserId = pa.UserId,
                        ParticipantName = pa.ParticipantName,
                        AttendingDays = pa.AttendingDays
                    })
                ],
                ParticipantsDiets =
                [
                    .. rsvp.ParticipantsDiets.Select(o => new ParticipantDiet
                    {
                        Id = o.Id,
                        EventId = o.EventId,
                        UserId = o.UserId,
                        ParticipantName = o.ParticipantName,
                        Restrictions = o.Restrictions,
                        OtherDetails = o.OtherDetails
                    })
                ],
                Comments = rsvp.Comments
            };
        }
        else
        {
            NewRsvp.Participants = GroupParticipants;
            // Default first participant for Day 1
            if (GroupParticipants.Count > 0)
            {
                NewRsvp.ParticipantsAttendance =
                [
                    new ParticipantAttendance
                    {
                        ParticipantName = GroupParticipants[0],
                        AttendingDays = [1]
                    }
                ];
            }
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int eventId)
    {
        var userId = User.GetId();

        var maxParticipants = await db.Events
            .Where(e => e.Id == eventId)
            .Select(e => e.MaxParticipantsPerRsvp)
            .FirstAsync();

        if (NewRsvp.Attending && NewRsvp.Participants.Count > maxParticipants)
        {
            toastNotification.AddErrorToastMessage($"Maximum {maxParticipants} participants allowed per RSVP.");
            return Page();
        }

        var user = await db.Users
            .Include(u => u.GuestGroup)
            .FirstAsync(u => u.Id == userId);
        if (user.GuestGroup is not null)
        {
            user.GuestGroup.Participants = NewRsvp.Participants;
            db.GuestGroups.Update(user.GuestGroup);
        }

        var rsvp = await db.Rsvps
            .Include(r => r.ParticipantsDiets)
            .Include(r => r.ParticipantsAttendance)
            .FirstOrDefaultAsync(r => r.EventId == eventId && r.UserId == userId);

        if (rsvp is null)
        {
            rsvp = new Rsvp { EventId = eventId, UserId = userId };
            db.Rsvps.Add(rsvp);
        }

        rsvp.Attending = NewRsvp.Attending;
        rsvp.SubmittedAt = DateTime.UtcNow;
        if (NewRsvp.Attending)
        {
            // Handle attendance update
            db.ParticipantsAttendance.RemoveRange(rsvp.ParticipantsAttendance);
            rsvp.ParticipantsAttendance =
            [
                .. NewRsvp.ParticipantsAttendance.Select(pa => new ParticipantAttendance
                {
                    EventId = eventId,
                    UserId = userId,
                    ParticipantName = pa.ParticipantName,
                    AttendingDays = pa.AttendingDays
                })
            ];

            // Handle dietary options update
            db.ParticipantsDiets.RemoveRange(rsvp.ParticipantsDiets);
            rsvp.ParticipantsDiets =
            [
                .. NewRsvp.ParticipantsDiets.Select(o => new ParticipantDiet
                {
                    EventId = eventId,
                    UserId = userId,
                    ParticipantName = o.ParticipantName,
                    Restrictions = o.Restrictions,
                    OtherDetails = o.OtherDetails
                })
            ];

            rsvp.Comments = NewRsvp.Comments;
        }
        else
        {
            db.ParticipantsAttendance.RemoveRange(rsvp.ParticipantsAttendance);
            rsvp.ParticipantsAttendance = [];
            db.ParticipantsDiets.RemoveRange(rsvp.ParticipantsDiets);
            rsvp.ParticipantsDiets = [];
        }

        await db.SaveChangesAsync();

        toastNotification.AddSuccessToastMessage("Thank you for your response!");
        return Redirect("/");
    }

    public async Task<IActionResult> OnPostCancelAsync(int eventId)
    {
        var userId = User.GetId();

        var rsvp = await db.Rsvps
            .Include(r => r.ParticipantsDiets)
            .Include(r => r.ParticipantsAttendance)
            .FirstOrDefaultAsync(r => r.EventId == eventId && r.UserId == userId);
        if (rsvp is null)
        {
            toastNotification.AddWarningToastMessage("You are not invited to this event");
            return Redirect("/");
        }

        db.Rsvps.Remove(rsvp);

        await db.SaveChangesAsync();

        toastNotification.AddSuccessToastMessage("Your RSVP has been cancelled. You can submit a new response.");
        return RedirectToPage(new { eventId });
    }

    public bool HasAccommodationInfo()
    {
        return !string.IsNullOrWhiteSpace(AssignedAccommodationCode)
               || !string.IsNullOrWhiteSpace(EventData.AccommodationDetails)
               || EventData.AccommodationCodes.Count > 0;
    }

    public sealed class InputModel
    {
        public List<string> Participants { get; set; } = [];
        public bool Attending { get; set; } = true;
        public List<ParticipantAttendance> ParticipantsAttendance { get; set; } = [];
        public List<ParticipantDiet> ParticipantsDiets { get; set; } = [];
        [StringLength(500)]
        public string? Comments { get; set; }
    }
}
