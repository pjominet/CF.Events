using System.ComponentModel.DataAnnotations;
using CF.Events.Web.Data;
using CF.Events.Web.Infrastructure;
using CF.Events.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace CF.Events.Web.Pages.Admin;

[Authorize(Roles = Constants.Roles.Admin)]
public class EventsModel(
    EventsDbContext db,
    UserManager<ApplicationUser> userManager,
    IWebHostEnvironment env) : PageModel
{
    public List<Event> AllEvents { get; private set; } = [];
    public List<string> InvitationFiles { get; private set; } = [];

    public bool ShowCreateModal { get; private set; }

    [BindProperty]
    public InputModel NewEvent { get; set; } = new() { Date = DateTime.Today.AddMonths(1) };

    public async Task OnGetAsync()
    {
        await LoadAsync();
    }

    public async Task<IActionResult> OnPostCreateAsync()
    {
        if (!ModelState.IsValid)
        {
            await LoadAsync();
            ShowCreateModal = true;
            return Page();
        }

        db.Events.Add(new Event
        {
            Name = NewEvent.Name,
            Type = NewEvent.Type,
            Date = NewEvent.Date,
            Location = NewEvent.Location,
            Description = NewEvent.Description,
            InvitationFileName = string.IsNullOrWhiteSpace(NewEvent.InvitationFileName) ? null : NewEvent.InvitationFileName,
            IsActive = true
        });
        await db.SaveChangesAsync();

        SetToast("Event created successfully!", "success");
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostToggleAsync(int id)
    {
        var ev = await db.Events.FindAsync(id);
        if (ev is null)
        {
            SetToast("Event not found", "error");
            return RedirectToPage();
        }

        ev.IsActive = !ev.IsActive;
        await db.SaveChangesAsync();
        SetToast($"Event {(ev.IsActive ? "activated" : "deactivated")} successfully", "success");
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostInviteAsync(int id, string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            SetToast("Please provide an email", "info");
            return RedirectToPage();
        }

        var ev = await db.Events.FindAsync(id);
        if (ev is null)
        {
            SetToast("Event not found", "error");
            return RedirectToPage();
        }

        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            SetToast("User not found", "error");
            return RedirectToPage();
        }

        var alreadyInvited = await db.Rsvps.AnyAsync(r => r.EventId == id && r.UserId == user.Id);
        if (alreadyInvited)
        {
            SetToast("User already invited", "error");
            return RedirectToPage();
        }

        db.Rsvps.Add(new Rsvp
        {
            EventId = id,
            UserId = user.Id,
            Attending = false,
            SubmittedAt = DateTime.MinValue
        });
        await db.SaveChangesAsync();

        if (!await userManager.IsInRoleAsync(user, Constants.Roles.User))
            await userManager.AddToRoleAsync(user, Constants.Roles.User);

        SetToast("User invited successfully", "success");
        return RedirectToPage();
    }

    private async Task LoadAsync()
    {
        AllEvents = await db.Events.OrderByDescending(e => e.Date).ToListAsync();
        InvitationFiles = GetInvitationFiles();
    }

    private List<string> GetInvitationFiles()
    {
        var invitationsPath = Path.Combine(env.ContentRootPath, "Resources", "Invitations");
        if (!Directory.Exists(invitationsPath))
            return [];

        return Directory.GetDirectories(invitationsPath)
            .Select(Path.GetFileName)
            .Where(f => f is not null && System.IO.File.Exists(Path.Combine(invitationsPath, f, "index.html")))
            .Select(f => f!)
            .ToList();
    }

    private void SetToast(string message, string type)
    {
        TempData["Toast"] = message;
        TempData["ToastType"] = type;
    }

    public sealed class InputModel
    {
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = "";

        [Required]
        [StringLength(20)]
        public string Type { get; set; } = "Wedding";

        public DateTime Date { get; set; }

        public string? Location { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }

        public string? InvitationFileName { get; set; }
    }
}
