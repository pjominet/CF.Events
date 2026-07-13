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
        [FromForm] List<string>? selectedRoles = null)
    {
        if (userList is null || userList.Length == 0)
            return BadRequest("No file uploaded");

        // Ensure it's a valid file type
        var extension = Path.GetExtension(userList.FileName).ToLowerInvariant();
        if (!_allowedFileExtensions.Contains(extension))
            return BadRequest("Only CSV files are allowed");

        if (string.IsNullOrEmpty(delimiter)) delimiter = ",";

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
            if (string.IsNullOrWhiteSpace(line))
                continue;

            var parts = line.Split(delimiter);

            var name = parts.Length > 0 ? parts[0].Trim() : null;
            // Skip if email is invalid
            if (string.IsNullOrWhiteSpace(name))
            {
                importErrors.Add($"Error importing {name}: Name is required.");
                continue;
            }

            var email = parts.Length > 1 ? parts[1].Trim() : null;

            // Skip if email is invalid
            if (string.IsNullOrWhiteSpace(email) || !email.IsEmail())
            {
                importErrors.Add($"Error importing {email}: Email is invalid.");
                continue;
            }

            var phone = parts.Length > 2 ? parts[2].Trim() : null;
            var guestGroupLabel = parts.Length > 3 ? parts[3].Trim() : null;

            if (selectedRoles.Contains(Roles.Guest) && string.IsNullOrWhiteSpace(guestGroupLabel) && string.IsNullOrWhiteSpace(name))
            {
                importErrors.Add($"Error importing {email}: Guest group or user name is required for guest role.");
                continue;
            }

            var user = new AppUser
            {
                UserName = email,
                Email = email,
                PhoneNumber = !string.IsNullOrWhiteSpace(phone) ? phone : null,
                DisplayName = name,
                MustChangePassword = true,
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
                Label = !string.IsNullOrWhiteSpace(guestGroupLabel) ? guestGroupLabel : name,
                GuestUserId = user.Id,
                Participants = participants.Count > 0 ? participants : [user.DisplayName!]
            };
            await db.SaveChangesAsync();
        }

        if (importErrors.Count == 0)
            toastNotification.AddSuccessToastMessage("Users imported successfully");
        else toastNotification.AddWarningToastMessage("Import had issues");

        TempData[ViewDataKeys.ImportErrors] = importErrors;
        return RedirectToPage("/admin/users");
    }
}
