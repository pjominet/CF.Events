using CF.Events.API.Models;
using CF.Events.Shared.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CF.Events.API.Data;

public class EventsDbContext(DbContextOptions<EventsDbContext> options) : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<Event> Events => Set<Event>();
    public DbSet<Rsvp> Rsvps => Set<Rsvp>();
    public DbSet<RevokedToken> RevokedTokens => Set<RevokedToken>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Rsvp>()
            .HasIndex(r => new { r.EventId, r.UserId })
            .IsUnique();
    }
}
