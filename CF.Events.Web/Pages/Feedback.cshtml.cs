using System.ComponentModel.DataAnnotations;
using CF.Events.Web.Data;
using CF.Events.Web.Infrastructure.Extensions;
using CF.Events.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using NToastNotify;

namespace CF.Events.Web.Pages;

public class FeedbackModel(
    EventsDbContext db,
    IToastNotification toastNotification) : PageModel
{
    [BindProperty]
    public FeedbackInput Feedback { get; set; } = null!;

    public void OnGet()
    {
        Feedback = new FeedbackInput();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();

        var userId = User.GetId();
        var isActive = await db.Users.AnyAsync(u => u.Id == userId && u.IsActive);
        if (!isActive)
        {
            toastNotification.AddErrorToastMessage("Your account is inactive.");
            return Redirect("/");
        }

        await db.Feedbacks.AddAsync(new Feedback
        {
            UserId = userId,
            Text = Feedback.Text
        });
        var result = await db.SaveChangesAsync();

        if (result > 0)
        {
            toastNotification.AddSuccessToastMessage("Thank you for submitting your feedback!");
            return Redirect("/");
        }

        toastNotification.AddErrorToastMessage("Something went wrong!");
        return Page();
    }

    public class FeedbackInput
    {
        [Required]
        [StringLength(1000)]
        public string Text { get; set; } = null!;
    }
}
