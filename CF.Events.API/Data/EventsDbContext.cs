using CF.Events.API.Models;
using CF.Events.Shared.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CF.Events.API.Data;

public class EventsDbContext(DbContextOptions<EventsDbContext> options) : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<Rsvp> Rsvps => Set<Rsvp>();
    public DbSet<RevokedToken> RevokedTokens => Set<RevokedToken>();
}
