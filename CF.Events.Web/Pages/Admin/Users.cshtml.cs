using System.ComponentModel.DataAnnotations;
using CF.Events.Web.Data;
using CF.Events.Web.Infrastructure.Extensions;
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

    public async Task<IActionResult> OnGetFeedbackAsync(string userId)
    {
        var feedbacks = await db.Feedbacks
            .Where(f => f.UserId == userId)
            .OrderByDescending(f => f.SubmittedAt)
            .Select(f => new
            {
                f.Text,
                SubmittedAt = f.SubmittedAt.ToString("g")
            })
            .ToListAsync();

        return new JsonResult(feedbacks);
    }

    public async Task<IActionResult> OnPostAddAsync()
    {
        if (NewUser.SelectedRoles.Contains(Roles.Guest) && string.IsNullOrWhiteSpace(NewUser.GuestGroup))
            ModelState.AddModelError("NewUser.GuestGroup", "Guest Group Label is required for guests.");

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
            PhoneNumber = NewUser.PhoneNumber,
            MustChangePassword = !NewUser.SelectedRoles.Contains(Roles.Guest),
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
                var participants = (NewUser.GuestGroup ?? "").Split("&").Select(p => p.Trim()).ToList();
                var guestGroup = await db.GuestGroups.FirstOrDefaultAsync(g => g.GuestUserId == user.Id);

                if (guestGroup is null)
                {
                    guestGroup = new GuestGroup
                    {
                        Label = NewUser.GuestGroup ?? NewUser.DisplayName!,
                        GuestUserId = user.Id,
                        Participants = participants.Count == 0 ? [user.DisplayName!] : participants,
                        MaxPeople = NewUser.MaxPeople ?? 4
                    };
                    db.GuestGroups.Add(guestGroup);
                }
                else
                {
                    guestGroup.Label = NewUser.GuestGroup ?? NewUser.DisplayName!;
                    guestGroup.MaxPeople = NewUser.MaxPeople ?? 4;
                    guestGroup.Participants = participants.Count == 0 ? [user.DisplayName!] : participants;
                }

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

    public async Task<IActionResult> OnPostEditAsync()
    {
        if (NewUser.SelectedRoles.Contains(Roles.Guest) && string.IsNullOrWhiteSpace(NewUser.GuestGroup))
            ModelState.AddModelError("NewUser.GuestGroup", "Guest Group Label is required for guests.");

        if (!ModelState.IsValid)
        {
            ViewData[ViewDataKeys.ShowAddModal] = true;
            await LoadAsync();
            return Page();
        }

        var user = await userManager.FindByIdAsync(NewUser.Id!);
        if (user is null)
        {
            toastNotification.AddErrorToastMessage("User not found");
            return RedirectToPage();
        }

        if (await userManager.IsInRoleAsync(user, Roles.Sudo) && !User.IsSudo())
        {
            toastNotification.AddErrorToastMessage("You cannot modify a Sudo user");
            return RedirectToPage();
        }

        user.Email = NewUser.Email;
        user.UserName = NewUser.Email;
        user.DisplayName = NewUser.DisplayName;
        user.PhoneNumber = NewUser.PhoneNumber;

        var result = await userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);
            ViewData[ViewDataKeys.ShowAddModal] = true;
            await LoadAsync();
            return Page();
        }

        var currentRoles = await userManager.GetRolesAsync(user);
        var wasSudo = currentRoles.Contains(Roles.Sudo);
        await userManager.RemoveFromRolesAsync(user, currentRoles);
        var newRoles = NewUser.SelectedRoles is { Count: > 0 } ? NewUser.SelectedRoles : [Roles.Guest];
        if (wasSudo && !newRoles.Contains(Roles.Sudo))
        {
            newRoles.Add(Roles.Sudo);
            if (!newRoles.Contains(Roles.Admin))
                newRoles.Add(Roles.Admin);
        }
        await userManager.AddToRolesAsync(user, newRoles);

        var updatedRoles = await userManager.GetRolesAsync(user);
        if (updatedRoles.Contains(Roles.Guest))
        {
            var participants = (NewUser.GuestGroup ?? "").Split("&", StringSplitOptions.RemoveEmptyEntries).Select(p => p.Trim()).ToList();
            var guestGroup = await db.GuestGroups.FirstOrDefaultAsync(g => g.GuestUserId == user.Id);

            if (guestGroup is null)
            {
                guestGroup = new GuestGroup
                {
                    Label = NewUser.GuestGroup ?? NewUser.DisplayName,
                    GuestUserId = user.Id,
                    Participants = participants.Count == 0 ? [user.DisplayName!] : participants,
                    MaxPeople = NewUser.MaxPeople ?? 4
                };
                db.GuestGroups.Add(guestGroup);
                await db.SaveChangesAsync();
                user.GuestGroupId = guestGroup.Id;
                await userManager.UpdateAsync(user);
            }
            else
            {
                guestGroup.Label = NewUser.GuestGroup ?? NewUser.DisplayName;
                guestGroup.MaxPeople = NewUser.MaxPeople ?? 4;

                // Update participants if they were changed
                if (participants.Count > 0)
                    guestGroup.Participants = participants;

                if (user.GuestGroupId is null)
                {
                    user.GuestGroupId = guestGroup.Id;
                    await userManager.UpdateAsync(user);
                }

                await db.SaveChangesAsync();
            }
        }

        toastNotification.AddSuccessToastMessage($"Updated user {NewUser.Email}");
        return RedirectToPage();
    }

    private async Task LoadAsync()
    {
        var users = await userManager.Users
            .Include(u => u.GuestGroup)
            .Include(u => u.GivenFeedbacks)
            .ToListAsync();
        AllUsers = [];
        foreach (var u in users)
        {
            var roles = await userManager.GetRolesAsync(u);
            var passwordSet = !string.IsNullOrWhiteSpace(u.PasswordHash) && !u.MustChangePassword;
            AllUsers.Add(new UserRow(
                u.Id,
                u.Email ?? "undefined",
                u.PhoneNumber ?? "-",
                u.DisplayName ?? "undefined",
                u.GuestGroup?.Label ?? "-",
                u.GuestGroup?.MaxPeople ?? 0,
                u.IsActive,
                roles,
                passwordSet ? u.MustChangePassword : null,
                u.GivenFeedbacks.Count > 0));
        }
        AllUsers = [.. AllUsers.OrderBy(u => u.DisplayName)];
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

        if (await userManager.IsInRoleAsync(user, Roles.Sudo) && !User.IsSudo())
        {
            toastNotification.AddErrorToastMessage("You cannot demote a Sudo user");
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

        if (await userManager.IsInRoleAsync(user, Roles.Sudo) && !User.IsSudo())
        {
            toastNotification.AddErrorToastMessage("You cannot deactivate a Sudo user");
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

        if (await userManager.IsInRoleAsync(user, Roles.Sudo) && !User.IsSudo())
        {
            toastNotification.AddErrorToastMessage("You cannot delete a Sudo user");
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
            if (user is null) continue;

            if (user.IsActive || (await userManager.IsInRoleAsync(user, Roles.Sudo) && !User.IsSudo()))
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

    public record UserRow(string Id, string Email, string Phone, string DisplayName, string GuestGroup, int MaxPeople, bool IsActive, IList<string> UserRoles, bool? MustChangePassword, bool HasFeedback)
    {
        public IEnumerable<string> GetOrderedRoles(bool isCurrentViewerSudo)
        {
            return UserRoles
                .Where(r => isCurrentViewerSudo || r != Roles.Sudo)
                .OrderBy(GetRoleOrder);
        }

        private static short GetRoleOrder(string role) => role switch
        {
            Roles.Sudo => (short)RoleOrder.Sudo,
            Roles.Admin => (short)RoleOrder.Admin,
            Roles.User => (short)RoleOrder.User,
            Roles.Guest => (short)RoleOrder.Guest,
            _ => short.MaxValue
        };
    }

    public sealed class InputModel
    {
        public string? Id { get; set; }

        [Required]
        public string DisplayName { get; set; } = string.Empty;

        public string? GuestGroup { get; set; }
        public int? MaxPeople { get; set; } = 4;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Phone]
        public string? PhoneNumber { get; set; }

        public List<string> SelectedRoles { get; set; } = [];
    }

}
