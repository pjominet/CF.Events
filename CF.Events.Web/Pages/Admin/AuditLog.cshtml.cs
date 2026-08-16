using CF.Events.Web.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using static CF.Events.Web.Infrastructure.Constants;

namespace CF.Events.Web.Pages.Admin;

[Authorize(Roles = Roles.Admin)]
public class AuditLogModel(EventsDbContext db) : PageModel
{
    private const int PageSize = 50;
    public List<AuditRow> Audits { get; private set; } = [];

    public async Task OnGetAsync()
    {
        Audits = await GetAuditsQuery(0).ToListAsync();
    }

    public async Task<JsonResult> OnGetLoadMoreAsync(int page)
    {
        var audits = await GetAuditsQuery(page).ToListAsync();
        return new JsonResult(audits);
    }

    private IQueryable<AuditRow> GetAuditsQuery(int page)
    {
        return db.LoginAudits
            .Include(a => a.User)
            .OrderByDescending(a => a.LoginAt)
            .Skip(page * PageSize)
            .Take(PageSize)
            .Select(a => new AuditRow(
                a.LoginAt,
                a.User.DisplayName ?? a.User.Email ?? "Unknown",
                a.User.Email ?? "N/A",
                a.IpAddress ?? "Unknown",
                a.AuthMethod ?? "Unknown",
                a.UserAgent ?? "Unknown"
            ));
    }

    public record AuditRow(DateTime LoginAt, string DisplayName, string Email, string IpAddress, string AuthMethod, string UserAgent);
}
