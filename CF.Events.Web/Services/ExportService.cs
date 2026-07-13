using CF.Events.Web.Data;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;

namespace CF.Events.Web.Services;

public interface IExportService
{
    Task<(byte[] Bytes, string FileName)> ExportInviteesToExcelAsync(int eventId);
}

public class ExportService(EventsDbContext db) : IExportService
{
    public async Task<(byte[] Bytes, string FileName)> ExportInviteesToExcelAsync(int eventId)
    {
        var @event = await db.Events
            .Where(e => e.Id == eventId)
            .Select(e => new
            {
                e.Name,
                e.StartDate,
                EventUsers = e.EventUsers
                    .OrderBy(eu => eu.User.DisplayName)
                    .Select(eu => new
                    {
                        eu.User.DisplayName,
                        eu.User.Email,
                        Rsvp = eu.Rsvp == null ? null : new
                        {
                            eu.Rsvp.Attending,
                            eu.Rsvp.AttendanceDays,
                            eu.Rsvp.DietaryOptionNbrPeople,
                            eu.Rsvp.CommonDietaryOptions,
                            eu.Rsvp.OtherDietaryDetails,
                            eu.Rsvp.Comments,
                            eu.Rsvp.SubmittedAt
                        }
                    })
                    .ToList()
            })
            .FirstOrDefaultAsync();

        if (@event is null)
            throw new ArgumentException("Event not found", nameof(eventId));

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Invitees");

        // Header
        var headers = new[] { "DisplayName", "Email", "Status", "AttendingDays", "DietaryOptions", "OtherDietaryDetails", "Comments", "SubmittedAt" };
        for (var i = 0; i < headers.Length; i++)
        {
            var cell = worksheet.Cell(1, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#198754"); // Bootstrap success color
            cell.Style.Font.FontColor = XLColor.White;
        }

        var row = 2;
        foreach (var eu in @event.EventUsers)
        {
            var status = eu.Rsvp is null ? "No Response" : (eu.Rsvp.Attending ? "Attending" : "Declined");
            var attendingDays = eu.Rsvp is not null ? string.Join("|", eu.Rsvp.AttendanceDays.Select(d => $"Day {d.Key} ({d.Value})")) : string.Empty;
            var dietaryOptions = eu.Rsvp is not null ? string.Join("|", eu.Rsvp.CommonDietaryOptions) : string.Empty;
            if (eu.Rsvp is not null && eu.Rsvp.DietaryOptionNbrPeople > 0)
            {
                dietaryOptions = $"({eu.Rsvp.DietaryOptionNbrPeople} people) {dietaryOptions}";
            }
            var otherDietary = eu.Rsvp?.OtherDietaryDetails ?? string.Empty;
            var comments = eu.Rsvp?.Comments ?? string.Empty;
            var submittedAt = eu.Rsvp?.SubmittedAt.ToString("yyyy-MM-dd HH:mm:ss") ?? string.Empty;

            worksheet.Cell(row, 1).Value = eu.DisplayName;
            worksheet.Cell(row, 2).Value = eu.Email;

            var statusCell = worksheet.Cell(row, 3);
            statusCell.Value = status;

            // Color coding for status
            if (eu.Rsvp is null)
            {
                statusCell.Style.Fill.BackgroundColor = XLColor.LightGray;
            }
            else if (eu.Rsvp.Attending)
            {
                statusCell.Style.Fill.BackgroundColor = XLColor.FromHtml("#D1E7DD"); // Light success
                statusCell.Style.Font.FontColor = XLColor.FromHtml("#0F5132");
            }
            else
            {
                statusCell.Style.Fill.BackgroundColor = XLColor.FromHtml("#F8D7DA"); // Light danger
                statusCell.Style.Font.FontColor = XLColor.FromHtml("#842029");
            }

            worksheet.Cell(row, 4).Value = attendingDays;
            worksheet.Cell(row, 5).Value = dietaryOptions;
            worksheet.Cell(row, 6).Value = otherDietary;
            worksheet.Cell(row, 7).Value = comments;
            worksheet.Cell(row, 8).Value = submittedAt;

            row++;
        }

        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        var content = stream.ToArray();

        var fileName = $"{@event.Name.Replace(" ", "_")}_{@event.StartDate.Year}.xlsx";
        return (content, fileName);
    }
}
