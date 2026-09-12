using CF.Events.Web.Data;
using CF.Events.Web.Infrastructure.Extensions;
using CF.Events.Web.Models;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Identity;
using static CF.Events.Web.Infrastructure.Constants;

namespace CF.Events.Web.Services;

public interface IImportService
{
    Task<(int ImportedCount, List<string> Errors)> ImportUsersFromCsvAsync(Stream stream, int skipRows, string delimiter, List<string> selectedRoles);
    Task<(int ImportedCount, List<string> Errors)> ImportUsersFromExcelAsync(Stream stream, int skipRows, List<string> selectedRoles);
}

public class ImportService(
    UserManager<AppUser> userManager,
    EventsDbContext db) : IImportService
{
    public async Task<(int ImportedCount, List<string> Errors)> ImportUsersFromCsvAsync(Stream stream, int skipRows, string delimiter, List<string> selectedRoles)
    {
        List<string> errors = [];
        var importedCount = 0;
        var currentRow = 0;

        using var reader = new StreamReader(stream);
        while (await reader.ReadLineAsync() is { } line)
        {
            currentRow++;

            if (currentRow <= skipRows)
                continue;

            if (!line.HasValue())
                continue;

            var parts = line.Split(delimiter);

            var name = parts.Length > 0 ? parts[0].Trim() : null;
            var emailRaw = parts.Length > 1 ? parts[1].Trim() : null;
            var phone = parts.Length > 2 ? parts[2].Trim() : null;
            var guestGroupLabel = parts.Length > 3 ? parts[3].Trim() : null;
            var maxPeopleStr = parts.Length > 4 ? parts[4].Trim() : null;
            var maxPeople = int.TryParse(maxPeopleStr, out var parsed) ? parsed : 4;

            if (await ProcessUserImport(name, emailRaw, phone, guestGroupLabel, maxPeople, currentRow, selectedRoles, errors))
            {
                importedCount++;
            }
        }

        return (importedCount, errors);
    }

    public async Task<(int ImportedCount, List<string> Errors)> ImportUsersFromExcelAsync(Stream stream, int skipRows, List<string> selectedRoles)
    {
        List<string> errors = [];
        var importedCount = 0;

        try
        {
            using var workbook = new XLWorkbook(stream);
            var worksheet = workbook.Worksheets.FirstOrDefault();
            if (worksheet == null)
            {
                errors.Add("The Excel file contains no worksheets.");
                return (0, errors);
            }

            var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 0;
            for (var rowNum = skipRows + 1; rowNum <= lastRow; rowNum++)
            {
                var row = worksheet.Row(rowNum);
                if (row.IsEmpty()) continue;

                var name = row.Cell(1).GetValue<string>()?.Trim();
                var emailRaw = row.Cell(2).GetValue<string>()?.Trim();
                var phone = row.Cell(3).GetValue<string>()?.Trim();
                var guestGroupLabel = row.Cell(4).GetValue<string>()?.Trim();
                var maxPeople = row.Cell(5).GetValue<int?>() ?? 4;

                if (await ProcessUserImport(name, emailRaw, phone, guestGroupLabel, maxPeople, rowNum, selectedRoles, errors))
                {
                    importedCount++;
                }
            }
        }
        catch (Exception ex)
        {
            errors.Add($"Error reading Excel file: {ex.Message}");
        }

        return (importedCount, errors);
    }

    private async Task<bool> ProcessUserImport(
        string? name,
        string? emailRaw,
        string? phone,
        string? guestGroupLabel,
        int maxPeople,
        int rowNumber,
        List<string> selectedRoles,
        List<string> importErrors)
    {
        if (!name.HasValue())
        {
            importErrors.Add($"Error importing row {rowNumber}: Name is required.");
            return false;
        }

        var email = GetValidEmail(emailRaw, name);
        if (email is null)
        {
            importErrors.Add($"Error importing row {rowNumber}: Email is invalid: {emailRaw}");
            return false;
        }

        if (selectedRoles.Contains(Roles.Guest) && !guestGroupLabel.HasValue())
        {
            importErrors.Add($"Error importing row {rowNumber}: Guest group is required for guest role.");
            return false;
        }

        var user = new AppUser
        {
            UserName = email,
            Email = email,
            PhoneNumber = phone.HasValue() ? phone : null,
            DisplayName = name,
            MustChangePassword = !selectedRoles.Contains(Roles.Guest),
            EmailConfirmed = true,
            IsActive = true
        };

        var result = await userManager.CreateAsync(user);
        if (!result.Succeeded)
        {
            if (result.Errors.Any(error => error.Code is "DuplicateUserName" or "DuplicateEmail"))
                return false;

            importErrors.Add($"Error creating user {email} (row {rowNumber}): {string.Join(", ", result.Errors.Select(e => e.Description))}");
            return false;
        }

        await userManager.AddToRolesAsync(user, selectedRoles);

        var participants = guestGroupLabel?.Split("&").Select(p => p.Trim()).ToList() ?? [];
        user.GuestGroup = new GuestGroup
        {
            Label = guestGroupLabel.HasValue() ? guestGroupLabel : name!,
            GuestUserId = user.Id,
            Participants = participants.Count > 0 ? participants : [user.DisplayName!],
            MaxPeople = maxPeople
        };
        await db.SaveChangesAsync();
        return true;
    }

    private static string? GetValidEmail(string? extractedValue, string fallbackName)
    {
        if (!extractedValue.HasValue())
            return null;

        if (extractedValue.StartsWith('!'))
            return $"{fallbackName.ToLower()}@{Email.NonSendableEmail}";

        return extractedValue.IsEmail() ? extractedValue : null;
    }
}
