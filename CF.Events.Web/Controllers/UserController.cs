using CF.Events.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using NToastNotify;
using static CF.Events.Web.Infrastructure.Constants;

namespace CF.Events.Web.Controllers;

[Area("admin")]
[Route("[area]/users")]
[Authorize(Roles = Roles.Admin)]
public class UserController(
    UserManager<AppUser> userManager,
    IToastNotification toastNotification) : Controller
{
    private readonly string[] _allowedFileExtensions = [".csv", ".txt"];

    [HttpPost("import")]
    public async Task<IActionResult> ImportUsers([FromForm] IFormFile? userList, [FromForm] int skipRows = 0, [FromForm] string delimiter = ",")
    {
        if (userList is null || userList.Length == 0)
            return BadRequest("No file uploaded");

        // Ensure it's a valid file type
        var extension = Path.GetExtension(userList.FileName).ToLowerInvariant();
        if (!_allowedFileExtensions.Contains(extension))
            return BadRequest("Only CSV files are allowed");

        if (string.IsNullOrEmpty(delimiter)) delimiter = ",";

        // Read all lines from the file
        var users = new List<AppUser>();
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

            var email = parts.Length > 1 ? parts[1].Trim() : null;
            // Skip if email is invalid
            if (string.IsNullOrEmpty(email) || !email.Contains('@'))
                continue;

            var name = parts.Length > 0 ? parts[0].Trim() : null;
            var phone = parts.Length > 2 ? parts[2].Trim() : null;

            users.Add(new AppUser
            {
                UserName = email,
                Email = email,
                PhoneNumber = phone,
                DisplayName = name,
                MustChangePassword = true,
                EmailConfirmed = true,
                IsActive = true
            });
        }

        // Create users in database
        List<string> importErrors = [];
        foreach (var user in users)
        {
            var result = await userManager.CreateAsync(user);
            if (!result.Succeeded)
            {
                if (result.Errors.Any(error => error.Code is "DuplicateUserName" or "DuplicateEmail"))
                    continue;

                importErrors.AddRange(result.Errors.Select(error => $"Error creating user {user.Email}: {error.Description}"));
                continue;
            }

            await userManager.AddToRoleAsync(user, Roles.User);
        }

        if (importErrors.Count == 0)
            toastNotification.AddSuccessToastMessage("Users imported successfully");
        else toastNotification.AddWarningToastMessage("Import had issues");

        TempData[ViewDataKeys.ImportErrors] = importErrors;
        return RedirectToPage("/admin/users");
    }
}
