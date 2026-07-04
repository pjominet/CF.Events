using CF.Events.Web.Data;
using CF.Events.Web.Infrastructure;
using CF.Events.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace CF.Events.Web.Pages.Admin;

[Authorize(Roles = Constants.Roles.Admin)]
public class CustomQuestionsModel(EventsDbContext db) : PageModel
{
    public Event? EventData { get; private set; }
    public List<CustomQuestion> Questions { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync(int id)
    {
        EventData = await db.Events.FindAsync(id);
        if (EventData is null)
            return NotFound();

        Questions = await db.CustomQuestions
            .Where(q => q.EventId == id)
            .OrderBy(q => q.FormStep)
            .ThenBy(q => q.SortOrder)
            .ToListAsync();

        return Page();
    }
}
