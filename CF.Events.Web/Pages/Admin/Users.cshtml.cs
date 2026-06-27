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

    [BindProperty] public AddUserViewModel AddViewModel { get; set; } = new();

    public async Task OnGetAsync()
    {
        await LoadAsync();
    }

    public async Task<IActionResult> OnPostAddAsync()
    {
        if (!ModelState.IsValid)
        {
            ViewData[ViewDataKeys.ShowAddModal] = true;
            await LoadAsync();
            return Page();
        }

        var user = new ApplicationUser
        {
            UserName = AddViewModel.Email,
            Email = AddViewModel.Email,
            DisplayName = AddViewModel.DisplayName,
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

        result = await userManager.AddToRoleAsync(user, Roles.User);
        if (result.Succeeded)
            toastNotification.AddSuccessToastMessage($"Added user {AddViewModel.Email}");
        else toastNotification.AddErrorToastMessage($"Failed to add user {AddViewModel.Email}");

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
                u.PhoneNumber ?? "n/a",
                u.DisplayName ?? "undefined",
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

    public record UserRow(string Id, string Email, string Phone, string DisplayName, bool IsActive, IList<string> Roles, bool MustChangePassword);
}
