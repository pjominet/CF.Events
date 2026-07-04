using CF.Events.Web.Data;
using CF.Events.Web.Infrastructure.Extensions;
using CF.Events.Web.Models;
using CF.Events.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace CF.Events.Web.Pages.Events;

[Authorize]
public class RsvpModel(EventsDbContext db, IRsvpService rsvpService) : PageModel
{
    public int InvitationId { get; private set; }
    public RsvpFormResponse? FormData { get; private set; }
    public bool NotFoundOrForbidden { get; private set; }

    /// <summary>
    /// Dietary option labels for display in the form.
    /// </summary>
    public static readonly Dictionary<DietaryOptions, string> DietaryLabels = new()
    {
        { DietaryOptions.None, "No restrictions" },
        { DietaryOptions.Vegetarian, "Vegetarian" },
        { DietaryOptions.Vegan, "Vegan" },
        { DietaryOptions.Pescetarian, "Pescetarian" },
        { DietaryOptions.GlutenIntolerant, "Gluten Intolerant" },
        { DietaryOptions.DairyIntolerant, "Dairy Intolerant" },
        { DietaryOptions.LactoseIntolerant, "Lactose Intolerant" }
    };

    /// <summary>
    /// Kid age bracket labels for display in the form.
    /// </summary>
    public static readonly Dictionary<KidAgeBracket, string> KidBracketLabels = new()
    {
        { KidAgeBracket.ZeroToThree, "0\u20133 years" },
        { KidAgeBracket.FourToEight, "4\u20138 years" },
        { KidAgeBracket.NineToFifteen, "9\u201315 years" },
        { KidAgeBracket.SixteenOrOlder, "16+" }
    };

    public async Task<IActionResult> OnGetAsync(int eventId)
    {
        var userId = User.GetId();

        // Find the user's invitation for this event
        var invitation = await db.InvitedPersons
            .Where(ip => ip.Invitation.EventId == eventId && ip.PrimaryGroupUserId == userId)
            .Select(ip => new { ip.InvitationId })
            .FirstOrDefaultAsync();

        if (invitation is null)
        {
            NotFoundOrForbidden = true;
            return Page();
        }

        InvitationId = invitation.InvitationId;
        FormData = await rsvpService.GetRsvpFormAsync(InvitationId, userId);

        if (FormData is null)
        {
            NotFoundOrForbidden = true;
            return Page();
        }

        return Page();
    }
}
