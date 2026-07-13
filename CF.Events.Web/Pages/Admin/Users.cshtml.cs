using System.ComponentModel.DataAnnotations;
using CF.Events.Web.Data;
using CF.Events.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using NToastNotify;
using static CF.Events.Web.Infrastructure.Constants;

namespace CF.Events.Web.Pages.Admin;

[Authorize(Roles = Roles.Admin)]
public class UsersModel(
    UserManager<AppUser> userManager,
    IToastNotification toastNotification,
    EventsDbContext db) : PageModel
{
    public List<UserRow> AllUsers { get; private set; } = [];

    [BindProperty] public InputModel NewUser { get; set; } = new();

    public async Task OnGetAsync()
    {
        await LoadAsync();
    }

    public async Task<IActionResult> OnPostAddAsync()
    {
        if (NewUser.SelectedRoles.Contains(Roles.Guest) && string.IsNullOrWhiteSpace(NewUser.GuestGroup))
        {
            ModelState.AddModelError("NewUser.GuestGroup", "Guest Group Label is required for guests.");
        }

        if (!ModelState.IsValid)
        {
            ViewData[ViewDataKeys.ShowAddModal] = true;
            await LoadAsync();
            return Page();
        }

        var user = new AppUser
        {
            UserName = NewUser.Email,
            Email = NewUser.Email,
            DisplayName = NewUser.DisplayName,
            MustChangePassword = true,
            EmailConfirmed = true
        };

        var result = await userManager.CreateAsync(user);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);
            ViewData[ViewDataKeys.ShowAddModal] = true;
            await LoadAsync();
            return Page();
        }

        if (NewUser.SelectedRoles is { Count: > 0 })
            result = await userManager.AddToRolesAsync(user, NewUser.SelectedRoles);
        else result = await userManager.AddToRoleAsync(user, Roles.Guest);

        if (result.Succeeded)
        {
            var userRoles = await userManager.GetRolesAsync(user);
            if (userRoles.Contains(Roles.Guest))
            {
                var guestGroup = new GuestGroup
                {
                    Label = NewUser.GuestGroup,
                    GuestUserId = user.Id,
                    Participants = [user.DisplayName ?? user.Email]
                };
                db.GuestGroups.Add(guestGroup);
                await db.SaveChangesAsync();

                user.GuestGroupId = guestGroup.Id;
                await userManager.UpdateAsync(user);
            }

            toastNotification.AddSuccessToastMessage($"Added user {NewUser.Email}");
        }
        else
            toastNotification.AddErrorToastMessage($"Failed to add roles for user {NewUser.Email}");

        return RedirectToPage();
    }

    private async Task LoadAsync()
    {
        var users = await userManager.Users
            .Include(u => u.GuestGroup)
            .ToListAsync();
        AllUsers = [];
        foreach (var u in users)
        {
            var roles = await userManager.GetRolesAsync(u);
            AllUsers.Add(new UserRow(
                u.Id,
                u.Email ?? "undefined",
                u.PhoneNumber ?? "n/a",
                u.DisplayName ?? "undefined",
                u.GuestGroup?.Label ?? "n/a",
                u.IsActive,
                roles,
                u.MustChangePassword));
        }
        AllUsers = AllUsers.OrderBy(u => u.DisplayName).ToList();
    }

    public async Task<IActionResult> OnPostPromoteAsync(string userId)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
        {
            toastNotification.AddWarningToastMessage("User not found");
            return RedirectToPage();
        }

        var result = await userManager.AddToRoleAsync(user, Roles.Admin);
        if (result.Succeeded)
            toastNotification.AddSuccessToastMessage("User promotion successfully");
        else toastNotification.AddErrorToastMessage("User promotion failed");

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDemoteAsync(string userId)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
        {
            toastNotification.AddWarningToastMessage("User not found");
            return RedirectToPage();
        }

        var result = await userManager.RemoveFromRoleAsync(user, Roles.Admin);
        if (result.Succeeded)
            toastNotification.AddSuccessToastMessage("User demotion successfully");
        else toastNotification.AddErrorToastMessage("User demotion failed");

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostToggleAsync(string userId)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
        {
            toastNotification.AddWarningToastMessage("User not found");
            return RedirectToPage();
        }

        user.IsActive = !user.IsActive;
        var result = await userManager.UpdateAsync(user);
        if (result.Succeeded)
            toastNotification.AddSuccessToastMessage("User toggled successfully");
        else toastNotification.AddErrorToastMessage("User toggle failed");

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(string userId)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
        {
            toastNotification.AddWarningToastMessage("User not found");
            return RedirectToPage();
        }

        if (user.IsActive)
        {
            toastNotification.AddErrorToastMessage("Only deactivated users can be deleted");
            return RedirectToPage();
        }

        var result = await userManager.DeleteAsync(user);
        if (result.Succeeded)
            toastNotification.AddSuccessToastMessage($"Deleted user {user.Email}");
        else toastNotification.AddErrorToastMessage($"Failed to delete user {user.Email}");

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostBulkDeleteAsync(string userIds)
    {
        if (string.IsNullOrEmpty(userIds))
            return RedirectToPage();

        var ids = userIds.Split(',', StringSplitOptions.RemoveEmptyEntries);
        var count = 0;
        var failed = 0;

        foreach (var id in ids)
        {
            var user = await userManager.FindByIdAsync(id);
            if (user == null) continue;

            if (user.IsActive)
            {
                failed++;
                continue;
            }

            var result = await userManager.DeleteAsync(user);
            if (result.Succeeded) count++;
            else failed++;
        }

        if (count > 0)
            toastNotification.AddSuccessToastMessage($"Successfully deleted {count} users");

        if (failed > 0)
            toastNotification.AddErrorToastMessage($"Failed to delete {failed} users (they might be active or system protected)");

        return RedirectToPage();
    }

    public record UserRow(string Id, string Email, string Phone, string DisplayName, string GuestGroup, bool IsActive, IList<string> Roles, bool MustChangePassword);

    public sealed class InputModel
    {
        [Required]
        public string DisplayName { get; set; } = string.Empty;

        public string GuestGroup { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        public List<string> SelectedRoles { get; set; } = [];
    }

}
