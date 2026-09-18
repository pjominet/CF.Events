using CF.Events.Web.Data;
using CF.Events.Web.Infrastructure.Extensions;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using static CF.Events.Web.Infrastructure.Constants;

namespace CF.Events.Web.Services;

public interface IExportService
{
    Task<(byte[] Bytes, string FileName)> ExportInviteesToExcelAsync(int eventId);
    Task<(byte[] Bytes, string FileName)> ExportUsersToExcelAsync(List<string> selectedRoles);
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
                            ParticipantAttendance = eu.Rsvp.ParticipantsAttendance.ToList(),
                            DietaryOptions = eu.Rsvp.ParticipantsDiets.ToList(),
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
        var headers = new[] { "DisplayName", "Email", "Status", "AttendingDays", "DietaryOptions", "Comments", "SubmittedAt" };
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
            var attendingDays = eu.Rsvp is not null
                ? string.Join("|", eu.Rsvp.ParticipantAttendance.Select(pa => $"{pa.ParticipantName}: {string.Join(", ", pa.AttendingDays)}"))
                : string.Empty;
            var dietaryOptions = eu.Rsvp is not null
                ? string.Join("|", eu.Rsvp.DietaryOptions.Select(d => $"{d.ParticipantName}: {string.Join(", ", d.Restrictions)}{(d.OtherDetails.HasValue() ? $" (Other: {d.OtherDetails})" : "")}"))
                : string.Empty;
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
            worksheet.Cell(row, 6).Value = comments;
            worksheet.Cell(row, 7).Value = submittedAt;

            row++;
        }

        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        var content = stream.ToArray();

        var fileName = $"{@event.Name.Replace(" ", "_")}_{@event.StartDate.Year}.xlsx";
        return (content, fileName);
    }

    public async Task<(byte[] Bytes, string FileName)> ExportUsersToExcelAsync(List<string> selectedRoles)
    {
        var excludedRoles = await db.Roles
            .Where(r => r.Name == Roles.Admin || r.Name == Roles.Sudo)
            .Select(r => r.Id)
            .ToListAsync();

        var rolesToInclude = await db.Roles
            .Where(r => selectedRoles.Contains(r.Name!))
            .Select(r => r.Id)
            .ToListAsync();

        var users = await db.Users
            .Include(u => u.GuestGroup)
            .Where(u => !db.UserRoles.Any(ur => ur.UserId == u.Id && excludedRoles.Contains(ur.RoleId)))
            .Where(u => db.UserRoles.Any(ur => ur.UserId == u.Id && rolesToInclude.Contains(ur.RoleId)))
            .OrderBy(u => u.DisplayName)
            .Select(u => new
            {
                u.DisplayName,
                u.Email,
                u.PhoneNumber,
                GuestGroupLabel = u.GuestGroup != null ? u.GuestGroup.Label : "",
                MaxPeople = u.GuestGroup != null ? u.GuestGroup.MaxPeople : 4
            })
            .ToListAsync();

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Users");

        // Header - matches UserController.ImportUsers expectations
        var headers = new[] { "Name", "Email", "Phone", "GuestGroupLabel", "MaxPeople" };
        for (var i = 0; i < headers.Length; i++)
        {
            var cell = worksheet.Cell(1, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#0D6EFD"); // Bootstrap primary color
            cell.Style.Font.FontColor = XLColor.White;
        }

        var row = 2;
        foreach (var user in users)
        {
            worksheet.Cell(row, 1).Value = user.DisplayName;
            worksheet.Cell(row, 2).Value = user.Email;
            worksheet.Cell(row, 3).Value = user.PhoneNumber;
            worksheet.Cell(row, 4).Value = user.GuestGroupLabel;
            worksheet.Cell(row, 5).Value = user.MaxPeople;
            row++;
        }

        // Add total sum of MaxPeople
        if (users.Count > 0)
        {
            var totalMaxPeople = users.Sum(u => u.MaxPeople);
            var totalRow = worksheet.Row(row);
            totalRow.Cell(4).Value = "Total";
            totalRow.Cell(4).Style.Font.Bold = true;
            totalRow.Cell(5).Value = totalMaxPeople;
            totalRow.Cell(5).Style.Font.Bold = true;
        }

        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        var content = stream.ToArray();

        var rolesStr = string.Join("_", selectedRoles);
        var fileName = $"Users_Export_{rolesStr}_{DateTime.Now:yyyyMMdd}.xlsx";
        return (content, fileName);
    }
}
