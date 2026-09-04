using System.ComponentModel.DataAnnotations;
using CF.Events.Web.Data;
using CF.Events.Web.Infrastructure.Extensions;
using CF.Events.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
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

        await db.Feedbacks.AddAsync(new Feedback
        {
            UserId = User.GetId(),
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
        public string Text { get; set; }
    }
}
