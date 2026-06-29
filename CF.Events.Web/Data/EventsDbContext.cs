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
    public DbSet<UserEvent> UserEvents => Set<UserEvent>();
    public DbSet<Rsvp> Rsvps => Set<Rsvp>();
    public DbSet<EventConfig> RsvpConfigs => Set<EventConfig>();

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

        builder.Entity<EventConfig>(e =>
        {
            e.HasKey(r => r.EventId);

            e.HasOne(r => r.Event)
                .WithOne(r => r.EventConfig)
                .HasForeignKey<EventConfig>(r => r.EventId)
                .IsRequired(false);
        });

        builder.Entity<Rsvp>(e =>
        {
            e.HasKey(r => new { r.EventId, r.UserId });

            e.Property(r => r.CommonDietaryOptions)
                .HasConversion(new EnumArrayConverter<DietaryOptions>())
                .HasMaxLength(4000);

            e.HasOne(r => r.UserEvent)
                .WithOne()
                .HasForeignKey<Rsvp>(r => new { r.EventId, r.UserId })
                .IsRequired(false);
        });

        builder.Entity<UserEvent>(e =>
        {
            e.HasKey(r => new { r.EventId, r.UserId });

            e.HasOne(r => r.User)
                .WithMany(r => r.UserEvents)
                .HasForeignKey(r => r.UserId)
                .IsRequired();

            e.HasOne(r => r.Event)
                .WithMany(r => r.EventUsers)
                .HasForeignKey(r => r.EventId)
                .IsRequired();
        });

        builder.Entity<InviteCode>(e =>
        {
            e.HasOne(r => r.Event)
                .WithMany(r => r.InviteCodes)
                .HasForeignKey(r => r.EventId)
                .IsRequired();
        });
    }
}
