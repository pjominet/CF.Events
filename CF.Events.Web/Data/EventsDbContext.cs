using CF.Events.Web.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CF.Events.Web.Data;

public class EventsDbContext(DbContextOptions<EventsDbContext> options) : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<Event> Events => Set<Event>();
    public DbSet<Rsvp> Rsvps => Set<Rsvp>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.HasDefaultSchema("app");

        builder.Entity<ApplicationUser>().ToTable("Users", "identity");
        builder.Entity<IdentityRole>().ToTable("Roles", "identity");
        builder.Entity<IdentityUserRole<string>>().ToTable("UserRoles", "identity");
        builder.Entity<IdentityUserClaim<string>>().ToTable("UserClaims", "identity");
        builder.Entity<IdentityUserLogin<string>>().ToTable("UserLogins", "identity");
        builder.Entity<IdentityRoleClaim<string>>().ToTable("RoleClaims", "identity");
        builder.Entity<IdentityUserToken<string>>().ToTable("UserTokens", "identity");

        builder.Entity<Rsvp>(e =>
        {
            e.HasIndex(r => new { r.EventId, r.UserId }).IsUnique();
        });

        builder.Entity<UserEvent>(e =>
        {
            e.HasKey(r => new { r.EventId, r.UserId });

            e.HasOne(r => r.User)
                .WithMany(r => r.UserEvents)
                .HasForeignKey(r => r.UserId);

            e.HasOne(r => r.Event)
                .WithMany(r => r.EventUsers)
                .HasForeignKey(r => r.EventId);
        });
    }
}
