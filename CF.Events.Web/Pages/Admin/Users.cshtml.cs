using System.ComponentModel.DataAnnotations;
using CF.Events.Web.Infrastructure;
using CF.Events.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace CF.Events.Web.Pages.Admin;

[Authorize(Roles = Constants.Roles.Admin)]
public class UsersModel(UserManager<ApplicationUser> userManager) : PageModel
{
    public List<UserRow> AllUsers { get; private set; } = [];

    public bool ShowInviteModal { get; private set; }

    [BindProperty]
    public InputModel Invite { get; set; } = new();

    public async Task OnGetAsync()
    {
        Invite.Password = GeneratePassword();
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
            MustChangePassword = true
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

        await userManager.AddToRoleAsync(user, Constants.Roles.User);

        TempData["Toast"] = $"Invitation created for {Invite.Email}. Temporary password: {Invite.Password}";
        TempData["ToastType"] = "success";
        return RedirectToPage();
    }

    private async Task LoadAsync()
    {
        var users = await userManager.Users.ToListAsync();
        AllUsers = [];
        foreach (var u in users)
        {
            var roles = await userManager.GetRolesAsync(u);
            AllUsers.Add(new UserRow(u.Id, u.Email ?? "", u.DisplayName ?? "", roles));
        }
    }

    private static string GeneratePassword() => Guid.NewGuid().ToString("N")[..10];

    public record UserRow(string Id, string Email, string DisplayName, IList<string> Roles);

    public sealed class InputModel
    {
        [Required]
        [Display(Name = "Display Name")]
        public string DisplayName { get; set; } = "";

        [Required]
        [EmailAddress]
        [Display(Name = "Email Address")]
        public string Email { get; set; } = "";

        [Required]
        [Display(Name = "Temporary Password")]
        public string Password { get; set; } = "";
    }
}
