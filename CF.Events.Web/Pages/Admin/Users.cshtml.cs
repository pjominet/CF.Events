using CF.Events.Web.Infrastructure;
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
    UserManager<ApplicationUser> userManager,
    IToastNotification toastNotification) : PageModel
{
    public List<UserRow> AllUsers { get; private set; } = [];

    public bool ShowInviteModal { get; private set; }

    [BindProperty]
    public InviteUserInput Invite { get; set; } = new();

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

        await userManager.AddToRoleAsync(user, Roles.User);

        toastNotification.AddSuccessToastMessage($"Invitation created for {Invite.Email}. Temporary password: {Invite.Password}");
        return RedirectToPage();
    }

    private async Task LoadAsync()
    {
        var users = await userManager.Users.ToListAsync();
        AllUsers = [];
        foreach (var u in users)
        {
            var roles = await userManager.GetRolesAsync(u);
            AllUsers.Add(new UserRow(u.Id, u.Email ?? "undefined", u.DisplayName ?? "undefined", roles));
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

        await userManager.AddToRoleAsync(user, Roles.Admin);
        toastNotification.AddSuccessToastMessage("User promotion successfully");
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

        await userManager.RemoveFromRoleAsync(user, Roles.Admin);
        toastNotification.AddSuccessToastMessage("User demotion successfully");
        return RedirectToPage();
    }

    public record UserRow(string Id, string Email, string DisplayName, IList<string> Roles);
}
