using CF.Events.Web.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CF.Events.Web.Data;

public class EventsDbContext(DbContextOptions<EventsDbContext> options) : IdentityDbContext<AppUser>(options)
{
    public DbSet<Event> Events => Set<Event>();
    public DbSet<AuthCode> AuthCodes => Set<AuthCode>();
    public DbSet<EventUser> EventUsers => Set<EventUser>();
    public DbSet<Rsvp> Rsvps => Set<Rsvp>();
    public DbSet<BookingLink> BookingLinks => Set<BookingLink>();
    public DbSet<ParticipantDiet> ParticipantsDiets => Set<ParticipantDiet>();
    public DbSet<ParticipantAttendance> ParticipantsAttendance => Set<ParticipantAttendance>();
    public DbSet<GuestGroup> GuestGroups => Set<GuestGroup>();
    public DbSet<EventFaqItem> EventFaq => Set<EventFaqItem>();
    public DbSet<EventScheduleStep> EventSchedule => Set<EventScheduleStep>();
    public DbSet<EventImage> EventImages => Set<EventImage>();
    public DbSet<LoginAudit> LoginAudits => Set<LoginAudit>();

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

            e.HasMany(r => r.ParticipantsDiets)
                .WithOne(o => o.Rsvp)
                .HasForeignKey(o => new { o.EventId, o.UserId })
                .OnDelete(DeleteBehavior.Cascade);

            e.HasMany(r => r.ParticipantsAttendance)
                .WithOne(o => o.Rsvp)
                .HasForeignKey(o => new { o.EventId, o.UserId })
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(r => r.EventUser)
                .WithOne(u => u.Rsvp)
                .HasForeignKey<Rsvp>(r => new { r.EventId, r.UserId })
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Event>(e =>
        {
            e.Property(r => r.AccommodationCodes).HasMaxLength(1000);
        });

        builder.Entity<EventFaqItem>(e =>
        {
            e.HasOne(r => r.Event)
                .WithMany(r => r.EventFaq)
                .HasForeignKey(r => r.EventId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();
        });

        builder.Entity<EventScheduleStep>(e =>
        {
            e.HasOne(r => r.Event)
                .WithMany(r => r.EventSchedule)
                .HasForeignKey(r => r.EventId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();
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
        });

        builder.Entity<AuthCode>(e =>
        {
            e.ToTable("AuthCodes", "identity");

            e.HasIndex(r => r.Value).IsUnique();

            e.HasOne(r => r.User)
                .WithMany(r => r.InviteCodes)
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();

            e.HasOne(r => r.Event)
                .WithMany(r => r.InviteCodes)
                .HasForeignKey(r => r.EventId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<EventImage>(e =>
        {
            e.HasOne(r => r.Event)
                .WithMany(r => r.EventImages)
                .HasForeignKey(r => r.EventId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();

            e.HasIndex(r => new { r.EventId, r.FileName }).IsUnique();
        });

        builder.Entity<LoginAudit>(e =>
        {
            e.ToTable("LoginAudits", "identity");

            e.Property(r => r.IpAddress).HasMaxLength(50);
            e.Property(r => r.UserAgent).HasMaxLength(500);
            e.Property(r => r.AuthMethod).HasMaxLength(50);

            e.HasOne(r => r.User)
                .WithMany(r => r.LoginAudits)
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();
        });

        builder.Entity<GuestGroup>(e =>
        {
            e.HasKey(g => g.Id);
            e.Property(g => g.Participants)
                .HasMaxLength(1000);

            e.HasOne(u => u.GuestUser)
                .WithOne(g => g.GuestGroup)
                .HasForeignKey<GuestGroup>(g => g.GuestUserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<ParticipantDiet>(e =>
        {
            e.HasKey(o => o.Id);
            e.Property(o => o.Restrictions)
                .HasMaxLength(1000);
        });

        builder.Entity<ParticipantAttendance>(e =>
        {
            e.HasKey(o => o.Id);
            e.Property(o => o.AttendingDays)
                .HasMaxLength(1000);
        });
    }
}
