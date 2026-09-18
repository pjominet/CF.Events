using CF.Events.Web.Models;
using CF.Events.Web.Models.Requests;

namespace CF.Events.Web.Services;

public interface IEventService
{
    Task<RsvpResponses?> GetRsvpResponsesAsync(int eventId, string userId);
    Task RemoveInviteeAsync(int eventId, string userId);
    Task<int> RemoveInviteesAsync(int eventId, List<string> userIds);
    Task<(RsvpRequest Model, int MaxParticipants, int EventDuration)> GetAdminRsvpDataAsync(int eventId, string userId);
    Task UpdateAdminRsvpAsync(int eventId, string userId, RsvpRequest newRsvp);
    Task<int> UpdateInviteesAsync(int eventId, List<InviteeUpdateRequest> updates);
}
