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
    public DbSet<EventConfig> EventConfigs => Set<EventConfig>();

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
                .HasConversion(new EnumArrayConverter<DietaryOptions>()!)
                .HasMaxLength(4000)
                .IsRequired(false);

            e.Property(r => r.KidsDetails)
                .HasConversion(new DictionaryConverter<KidAgeBracket, int>()!)
                .IsRequired(false);

            e.HasOne(r => r.EventUser)
                .WithOne(u => u.Rsvp)
                .HasForeignKey<Rsvp>(r => new { r.EventId, r.UserId })
                .IsRequired(false);
        });

        builder.Entity<EventUser>(e =>
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
            e.HasIndex(r => r.Code).IsUnique();

            e.HasOne(r => r.Event)
                .WithMany(r => r.InviteCodes)
                .HasForeignKey(r => r.EventId)
                .IsRequired();
        });
    }
}
