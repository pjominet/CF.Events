using CF.Events.Web.Data;
using CF.Events.Web.Infrastructure.Extensions;
using CF.Events.Web.Models;
using CF.Events.Web.Models.Requests;
using Microsoft.EntityFrameworkCore;

namespace CF.Events.Web.Services;

public class EventService(EventsDbContext db) : IEventService
{
    public async Task<RsvpResponses?> GetRsvpResponsesAsync(int eventId, string userId)
    {
        var rsvp = await db.Rsvps
            .Where(r => r.EventId == eventId && r.UserId == userId)
            .Select(r => new
            {
                r.ParticipantsAttendance,
                r.ParticipantsDiets,
                r.Comments
            })
            .FirstOrDefaultAsync();

        if (rsvp is null) return null;

        var guestGroup = await db.GuestGroups.FirstOrDefaultAsync(gg => gg.GuestUserId == userId);

        return new RsvpResponses
        {
            GuestGroup = guestGroup?.Label ?? "Guest Group",
            ParticipantsAttendance = rsvp.ParticipantsAttendance,
            ParticipantsDiets = rsvp.ParticipantsDiets,
            Comments = rsvp.Comments
        };
    }

    public async Task RemoveInviteeAsync(int eventId, string userId)
    {
        var userEvent = await db.EventUsers.FirstOrDefaultAsync(r => r.EventId == eventId && r.UserId == userId);
        if (userEvent is not null)
        {
            db.EventUsers.Remove(userEvent);
            await db.SaveChangesAsync();
        }
    }

    public async Task<int> RemoveInviteesAsync(int eventId, List<string> userIds)
    {
        var userEvents = await db.EventUsers
            .Where(r => r.EventId == eventId && userIds.Contains(r.UserId))
            .ToListAsync();

        if (userEvents.Count == 0) return 0;

        db.EventUsers.RemoveRange(userEvents);
        await db.SaveChangesAsync();
        return userEvents.Count;
    }

    public async Task<(RsvpRequest Model, int MaxParticipants, int EventDuration)> GetAdminRsvpDataAsync(int eventId, string userId)
    {
        var user = await db.Users.Include(u => u.GuestGroup).FirstAsync(u => u.Id == userId);
        var participants = user.GuestGroup?.Participants ?? (user.DisplayName.HasValue() ? [user.DisplayName] : []);

        var rsvp = await db.Rsvps
            .Include(r => r.ParticipantsDiets)
            .Include(r => r.ParticipantsAttendance)
            .FirstOrDefaultAsync(r => r.EventId == eventId && r.UserId == userId);

        var eventData = await db.Events
            .Where(e => e.Id == eventId)
            .Select(e => new { e.EventDuration, e.MaxParticipantsPerRsvp })
            .FirstAsync();

        var maxParticipants = user.GuestGroup?.MaxPeople ?? 0;

        if (maxParticipants == 0)
            maxParticipants = eventData.MaxParticipantsPerRsvp > 0 ? eventData.MaxParticipantsPerRsvp : 2;

        var model = new RsvpRequest
        {
            Participants = participants,
            Attending = rsvp?.Attending ?? true,
            ParticipantsAttendance = rsvp?.ParticipantsAttendance ?? [],
            ParticipantsDiets = rsvp?.ParticipantsDiets ?? [],
            Comments = rsvp?.Comments
        };

        return (model, maxParticipants, eventData.EventDuration);
    }

    public async Task UpdateAdminRsvpAsync(int eventId, string userId, RsvpRequest newRsvp)
    {
        var @event = await db.Events.FindAsync(eventId);
        if (@event is null) throw new ArgumentException("Event not found");

        var user = await db.Users
            .Include(u => u.GuestGroup)
            .FirstAsync(u => u.Id == userId);

        var eventMaxParticipants = await db.Events
            .Where(e => e.Id == eventId)
            .Select(e => e.MaxParticipantsPerRsvp)
            .FirstAsync();

        var maxParticipants = user.GuestGroup?.MaxPeople ?? 0;

        if (maxParticipants == 0)
            maxParticipants = eventMaxParticipants > 0 ? eventMaxParticipants : 2;

        newRsvp.Participants = [.. newRsvp.Participants.Where(p => p.HasValue())];

        if (newRsvp.Attending && newRsvp.Participants.Count > maxParticipants)
            throw new ArgumentException($"Maximum {maxParticipants} participants allowed per RSVP.");

        if (user.GuestGroup is not null)
        {
            user.GuestGroup.Participants = newRsvp.Participants;
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

        rsvp.Attending = newRsvp.Attending;
        rsvp.SubmittedAt = DateTime.UtcNow;

        if (newRsvp.Attending)
        {
            var attendanceToDelete = await db.ParticipantsAttendance.Where(pa => pa.EventId == eventId && pa.UserId == userId).ToListAsync();
            db.ParticipantsAttendance.RemoveRange(attendanceToDelete);

            rsvp.ParticipantsAttendance =
            [
                .. newRsvp.ParticipantsAttendance.Select(pa => new ParticipantAttendance
                {
                    EventId = eventId,
                    UserId = userId,
                    ParticipantName = pa.ParticipantName,
                    AttendingDays = pa.AttendingDays
                })
            ];

            var dietsToDelete = await db.ParticipantsDiets.Where(pd => pd.EventId == eventId && pd.UserId == userId).ToListAsync();
            db.ParticipantsDiets.RemoveRange(dietsToDelete);

            rsvp.ParticipantsDiets =
            [
                .. newRsvp.ParticipantsDiets.Select(o => new ParticipantDiet
                {
                    EventId = eventId,
                    UserId = userId,
                    ParticipantName = o.ParticipantName,
                    Restrictions = o.Restrictions,
                    OtherDetails = o.OtherDetails
                })
            ];

            rsvp.Comments = newRsvp.Comments;
        }
        else
        {
            var attendanceToDelete = await db.ParticipantsAttendance.Where(pa => pa.EventId == eventId && pa.UserId == userId).ToListAsync();
            db.ParticipantsAttendance.RemoveRange(attendanceToDelete);
            rsvp.ParticipantsAttendance = [];

            var dietsToDelete = await db.ParticipantsDiets.Where(pd => pd.EventId == eventId && pd.UserId == userId).ToListAsync();
            db.ParticipantsDiets.RemoveRange(dietsToDelete);
            rsvp.ParticipantsDiets = [];
        }

        await db.SaveChangesAsync();
    }

    public async Task<int> UpdateInviteesAsync(int eventId, List<InviteeUpdateRequest> updates)
    {
        if (updates.Count == 0) return 0;

        var userIds = updates
            .Select(u => u.UserId)
            .Where(id => !string.IsNullOrEmpty(id))
            .Distinct()
            .ToList();

        if (userIds.Count == 0) return 0;

        var eventUsers = await db.EventUsers
            .Where(r => r.EventId == eventId && userIds.Contains(r.UserId))
            .ToListAsync();

        var userUpdatesGroups = updates
            .Where(u => !string.IsNullOrEmpty(u.UserId))
            .GroupBy(u => u.UserId)
            .ToDictionary(g => g.Key, g => g.ToList());

        foreach (var eventUser in eventUsers)
        {
            if (!userUpdatesGroups.TryGetValue(eventUser.UserId, out var userUpdates))
                continue;

            foreach (var update in userUpdates)
            {
                if (update.AccommodationCode.HasValue())
                    eventUser.AssignedAccommodationCode = update.AccommodationCode;

                if (update.Priority.HasValue)
                    eventUser.InvitationPriority = update.Priority.Value;
            }
        }

        await db.SaveChangesAsync();
        return eventUsers.Count;
    }
}
