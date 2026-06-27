using CF.Events.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using static CF.Events.Web.Infrastructure.Constants;

namespace CF.Events.Web.Controllers;

[Route("users")]
[Authorize(Roles = Roles.Admin)]
public class UserController(
    UserManager<ApplicationUser> userManager,
    ILogger<UserController> logger) : Controller
{
    private readonly string[] allowedFileExtensions = { ".csv", ".txt" };

    [HttpPost("import")]
    public async Task<IActionResult> ImportUsers([FromBody] IFormFile? userList)
    {
        if (userList is null || userList.Length == 0)
            return BadRequest("No file uploaded");

        // Ensure it's a valid file type
        var extension = Path.GetExtension(userList.FileName).ToLowerInvariant();
        if (!allowedFileExtensions.Contains(extension))
            return BadRequest("Only CSV files are allowed");

        // Read all lines from the file
        using var reader = new StreamReader(userList.OpenReadStream());
        var users = new List<ApplicationUser>();

        while (await reader.ReadLineAsync() is { } line)
        {
            // Skip empty lines
            if (string.IsNullOrWhiteSpace(line))
                continue;

            var parts = line.Split(',');

            var name = parts[0].Trim();
            var email = parts[1].Trim();

            // Skip if email is invalid
            if (string.IsNullOrEmpty(email) || !email.Contains('@'))
                continue;

            users.Add(new ApplicationUser
            {
                UserName = email,
                Email = email,
                DisplayName = name,
                MustChangePassword = true,
                EmailConfirmed = true,
                IsActive = true
            });
        }

        // Create users in database
        foreach (var user in users)
        {
            var result = await userManager.CreateAsync(user);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    logger.LogError("Error creating user {Email}: {Description}", user.Email, error.Description);
                }
                continue;
            }

            await userManager.AddToRoleAsync(user, Roles.User);
        }

        return RedirectToPage("/admin/users");
    }
}
