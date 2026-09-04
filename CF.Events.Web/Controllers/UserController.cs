using CF.Events.Web.Data;
using CF.Events.Web.Infrastructure.Extensions;
using CF.Events.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using NToastNotify;
using static CF.Events.Web.Infrastructure.Constants;

namespace CF.Events.Web.Controllers;

[Route("users")]
[Authorize(Roles = Roles.Admin)]
public class UserController(
    UserManager<AppUser> userManager,
    IToastNotification toastNotification,
    EventsDbContext db) : Controller
{
    private readonly string[] _allowedFileExtensions = [".csv", ".txt"];

    [HttpPost("import")]
    public async Task<IActionResult> ImportUsers(
        [FromForm] IFormFile? userList,
        [FromForm] int skipRows = 0,
        [FromForm] string delimiter = ",",
        [FromForm] List<string>? selectedRoles = null,
        [FromQuery] string? search = null)
    {
        if (userList is null || userList.Length == 0)
            return BadRequest("No file uploaded");

        // Ensure it's a valid file type
        var extension = Path.GetExtension(userList.FileName).ToLowerInvariant();
        if (!_allowedFileExtensions.Contains(extension))
            return BadRequest("Only CSV files are allowed");

        if (!delimiter.HasValue(false)) delimiter = ",";

        selectedRoles ??= [Roles.Guest];

        // Read all lines from the file
        List<string> importErrors = [];
        using var reader = new StreamReader(userList.OpenReadStream());

        var currentRow = 0;
        while (await reader.ReadLineAsync() is { } line)
        {
            currentRow++;

            // Skip specified header rows
            if (currentRow <= skipRows)
                continue;

            // Skip empty lines
            if (!line.HasValue())
                continue;

            var parts = line.Split(delimiter);

            var name = parts.Length > 0 ? parts[0].Trim() : null;
            if (!name.HasValue())
            {
                importErrors.Add($"Error importing row {currentRow}: Name is required.");
                continue;
            }

            var email = GetValidEmail(parts.Length > 1 ? parts[1].Trim() : null, name);
            if (email is null)
            {
                importErrors.Add($"Error importing row {currentRow}: Email is invalid: {email}");
                continue;
            }

            var phone = parts.Length > 2 ? parts[2].Trim() : null;

            var guestGroupLabel = parts.Length > 3 ? parts[3].Trim() : null;
            var maxPeopleStr = parts.Length > 4 ? parts[4].Trim() : null;
            var maxPeople = int.TryParse(maxPeopleStr, out var parsed) ? parsed : 4;

            if (selectedRoles.Contains(Roles.Guest) && !guestGroupLabel.HasValue())
            {
                importErrors.Add($"Error importing row {currentRow}: Guest group is required for guest role.");
                continue;
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
                    continue;

                importErrors.Add($"Error creating user {email}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                continue;
            }

            await userManager.AddToRolesAsync(user, selectedRoles);

            var participants = guestGroupLabel?.Split("&").Select(p => p.Trim()).ToList() ??[];
            user.GuestGroup = new GuestGroup
            {
                Label = guestGroupLabel.HasValue() ? guestGroupLabel : name!,
                GuestUserId = user.Id,
                Participants = participants.Count > 0 ? participants : [user.DisplayName!],
                MaxPeople = maxPeople
            };
            await db.SaveChangesAsync();
        }

        if (importErrors.Count == 0)
            toastNotification.AddSuccessToastMessage("Users imported successfully");
        else toastNotification.AddWarningToastMessage($"Import had issues:{Environment.NewLine}{string.Join(Environment.NewLine, importErrors)}");
        TempData[ViewDataKeys.ImportErrors] = importErrors;
        return RedirectToPage("/admin/users", new { search });
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
