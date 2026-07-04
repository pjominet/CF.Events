using CF.Events.Web.Data.Comparers;
using CF.Events.Web.Data.Converters;
using CF.Events.Web.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CF.Events.Web.Data;

public class EventsDbContext(DbContextOptions<EventsDbContext> options) : IdentityDbContext<AppUser>(options)
{
    public DbSet<Event> Events => Set<Event>();
    public DbSet<InviteCode> InviteCodes => Set<InviteCode>();
    public DbSet<EventUser> EventUsers => Set<EventUser>();
    public DbSet<Rsvp> Rsvps => Set<Rsvp>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
#if DEBUG
        optionsBuilder.EnableSensitiveDataLogging();
        optionsBuilder.EnableDetailedErrors();
#endif
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.HasDefaultSchema("app");

        builder.Entity<AppUser>().ToTable("Users", "identity");
        builder.Entity<IdentityRole>().ToTable("Roles", "identity");
        builder.Entity<IdentityUserRole<string>>().ToTable("UserRoles", "identity");
        builder.Entity<IdentityUserClaim<string>>().ToTable("UserClaims", "identity");
        builder.Entity<IdentityUserLogin<string>>().ToTable("UserLogins", "identity");
        builder.Entity<IdentityRoleClaim<string>>().ToTable("RoleClaims", "identity");
        builder.Entity<IdentityUserToken<string>>().ToTable("UserTokens", "identity");

        builder.Entity<Rsvp>(e =>
        {
            e.HasKey(r => new { r.EventId, r.UserId });

            e.Property(r => r.CommonDietaryOptions)
                .HasConversion(new ArrayConverter<DietaryOptions>()!, new ArrayComparer<DietaryOptions>())
                .HasMaxLength(4000)
                .IsRequired(false);

            e.HasOne(r => r.EventUser)
                .WithOne(u => u.Rsvp)
                .HasForeignKey<Rsvp>(r => new { r.EventId, r.UserId })
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired(false);
        });

        builder.Entity<EventUser>(e =>
        {
            e.HasKey(r => new { r.EventId, r.UserId });

            e.HasOne(r => r.User)
                .WithMany(r => r.UserEvents)
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();

            e.HasOne(r => r.Event)
                .WithMany(r => r.EventUsers)
                .HasForeignKey(r => r.EventId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();

            e.HasOne(r => r.InviteCode)
                .WithMany(r => r.EventUsers)
                .HasForeignKey(r => r.InviteCodeId)
                .OnDelete(DeleteBehavior.NoAction)
                .IsRequired();
        });

        builder.Entity<InviteCode>(e =>
        {
            e.HasIndex(r => r.Code).IsUnique();

            e.Property(r => r.Label).IsRequired();

            e.HasOne(r => r.Event)
                .WithMany(r => r.InviteCodes)
                .HasForeignKey(r => r.EventId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();
        });
    }
}
