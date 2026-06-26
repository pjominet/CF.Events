using CF.Events.Web.Infrastructure;
using CF.Events.Web.Models;
using CF.Events.Web.ViewModels;
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
    UserManager<ApplicationUser> userManager,
    IToastNotification toastNotification) : PageModel
{
    public List<UserRow> AllUsers { get; private set; } = [];

    public bool ShowInviteModal { get; private set; }
    public bool ShowRegenPasswordModal { get; private set; }

    [BindProperty] public InviteUserInput Invite { get; set; } = new();

    public async Task OnGetAsync()
    {
        Invite.Password = TempPasswordGenerator.Generate();
        await LoadAsync();
    }

    public async Task<IActionResult> OnPostInviteAsync()
    {
        if (!ModelState.IsValid)
        {
            ShowInviteModal = true;
            await LoadAsync();
            return Page();
        }

        var user = new ApplicationUser
        {
            UserName = Invite.Email,
            Email = Invite.Email,
            DisplayName = Invite.DisplayName,
            MustChangePassword = true,
            EmailConfirmed = true
        };

        var result = await userManager.CreateAsync(user, Invite.Password);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);
            ShowInviteModal = true;
            await LoadAsync();
            return Page();
        }

        result = await userManager.AddToRoleAsync(user, Roles.User);
        if (result.Succeeded)
            toastNotification.AddSuccessToastMessage($"Invitation created for {Invite.Email}. Temporary password: {Invite.Password}");
        else toastNotification.AddErrorToastMessage($"Invitation failed for {Invite.Email}");

        return RedirectToPage();
    }

    private async Task LoadAsync()
    {
        var users = await userManager.Users.ToListAsync();
        AllUsers = [];
        foreach (var u in users)
        {
            var roles = await userManager.GetRolesAsync(u);
            AllUsers.Add(new UserRow(
                u.Id,
                u.Email ?? "undefined",
                u.DisplayName ?? "undefined",
                u.IsActive,
                roles,
                u.MustChangePassword));
        }
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

    public async Task<IActionResult> OnPostRegeneratePasswordAsync(string userId, string tempPassword)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
        {
            ShowRegenPasswordModal = true;
            toastNotification.AddWarningToastMessage("User not found");
            return RedirectToPage();
        }

        var newPassword = TempPasswordGenerator.Generate();
        user.MustChangePassword = true;

        var result = await userManager.RemovePasswordAsync(user);
        if (!result.Succeeded)
        {
            ShowRegenPasswordModal = true;
            toastNotification.AddErrorToastMessage("Failed to reset password");
            return RedirectToPage();
        }

        result = await userManager.AddPasswordAsync(user, newPassword);
        if (!result.Succeeded)
        {
            ShowRegenPasswordModal = true;
            toastNotification.AddErrorToastMessage("Failed to set new password");
            return RedirectToPage();
        }

        result = await userManager.UpdateAsync(user);
        if (result.Succeeded)
            toastNotification.AddSuccessToastMessage("Password regenerated successfully");
        else
        {
            ShowRegenPasswordModal = true;
            toastNotification.AddErrorToastMessage("Failed to update user");
        }

        return RedirectToPage();
    }

    public record UserRow(string Id, string Email, string DisplayName, bool IsActive, IList<string> Roles, bool MustChangePassword);
}
