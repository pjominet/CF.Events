using System.Text;
using CF.Events.Web.Data;
using Microsoft.EntityFrameworkCore;

namespace CF.Events.Web.Services;

public interface IExportService
{
    Task<(byte[] Bytes, string FileName)> ExportInviteesToCsvAsync(int eventId);
}

public class ExportService(EventsDbContext db) : IExportService
{
    public async Task<(byte[] Bytes, string FileName)> ExportInviteesToCsvAsync(int eventId)
    {
        var @event = await db.Events
            .Include(e => e.EventUsers)
                .ThenInclude(eu => eu.User)
            .Include(e => e.EventUsers)
                .ThenInclude(eu => eu.Rsvp)
            .FirstOrDefaultAsync(e => e.Id == eventId);

        if (@event == null)
        {
            throw new ArgumentException("Event not found", nameof(eventId));
        }

        var csv = new StringBuilder();

        // Header
        csv.AppendLine("DisplayName,Email,Status,AttendingDays,DietaryOptions,OtherDietaryDetails,Comments,SubmittedAt");

        foreach (var eu in @event.EventUsers.OrderBy(x => x.User.DisplayName))
        {
            var status = eu.Rsvp == null ? "No Response" : (eu.Rsvp.Attending ? "Attending" : "Declined");
            var attendingDays = eu.Rsvp != null ? string.Join("|", eu.Rsvp.AttendanceDays) : "";
            var dietaryOptions = eu.Rsvp != null ? string.Join("|", eu.Rsvp.CommonDietaryOptions) : "";
            var otherDietary = eu.Rsvp?.OtherDietaryDetails?.Replace(",", ";") ?? "";
            var comments = eu.Rsvp?.Comments?.Replace(",", ";").Replace("\r", " ").Replace("\n", " ") ?? "";
            var submittedAt = eu.Rsvp?.SubmittedAt.ToString("yyyy-MM-dd HH:mm:ss") ?? "";

            csv.AppendLine($"\"{eu.User.DisplayName}\",\"{eu.User.Email}\",\"{status}\",\"{attendingDays}\",\"{dietaryOptions}\",\"{otherDietary}\",\"{comments}\",\"{submittedAt}\"");
        }

        var fileName = $"{@event.Name.Replace(" ", "_")}_{@event.StartDate.Year}.csv";
        return (Encoding.UTF8.GetBytes(csv.ToString()), fileName);
    }
}
