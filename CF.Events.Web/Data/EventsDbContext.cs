using CF.Events.Web.Data.ModelBuilders;
using CF.Events.Web.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CF.Events.Web.Data;

public class EventsDbContext(DbContextOptions<EventsDbContext> options) : IdentityDbContext<AppUser>(options)
{
    public DbSet<Event> Events => Set<Event>();
    public DbSet<EventConfig> EventConfigs => Set<EventConfig>();
    public DbSet<EventDay> EventDays => Set<EventDay>();
    public DbSet<CustomQuestion> CustomQuestions => Set<CustomQuestion>();

    public DbSet<InviteToken> InviteCodes => Set<InviteToken>();
    public DbSet<Invitation> Invitations => Set<Invitation>();
    public DbSet<InviteGroup> InvitedPersons => Set<InviteGroup>();

    public DbSet<Rsvp> Rsvps => Set<Rsvp>();
    public DbSet<RsvpPerson> RsvpPersons => Set<RsvpPerson>();
    public DbSet<RsvpFoodPreference> RsvpFoodPreferences => Set<RsvpFoodPreference>();
    public DbSet<RsvpAccommodation> RsvpAccommodations => Set<RsvpAccommodation>();
    public DbSet<RsvpCustomAnswer> RsvpCustomAnswers => Set<RsvpCustomAnswer>();

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

        // Default schema - most tables will override this
        builder.HasDefaultSchema("app");

        // ===== Model Builders =====
        // Identity
        AppUserModelBuilder.Configure(builder.Entity<AppUser>());

        // Identity framework models (these don't have model builders)
        builder.Entity<IdentityRole>().ToTable("Roles", "identity");
        builder.Entity<IdentityUserRole<string>>().ToTable("UserRoles", "identity");
        builder.Entity<IdentityUserClaim<string>>().ToTable("UserClaims", "identity");
        builder.Entity<IdentityUserLogin<string>>().ToTable("UserLogins", "identity");
        builder.Entity<IdentityRoleClaim<string>>().ToTable("RoleClaims", "identity");
        builder.Entity<IdentityUserToken<string>>().ToTable("UserTokens", "identity");

        // Events
        EventModelBuilder.Configure(builder.Entity<Event>());
        EventConfigModelBuilder.Configure(builder.Entity<EventConfig>());
        EventDayModelBuilder.Configure(builder.Entity<EventDay>());
        CustomQuestionModelBuilder.Configure(builder.Entity<CustomQuestion>());

        // Invitations
        InviteCodeModelBuilder.Configure(builder.Entity<InviteToken>());
        InvitationModelBuilder.Configure(builder.Entity<Invitation>());
        InvitedPersonModelBuilder.Configure(builder.Entity<InviteGroup>());

        // RSVPs
        RsvpModelBuilder.Configure(builder.Entity<Rsvp>());
        RsvpPersonModelBuilder.Configure(builder.Entity<RsvpPerson>());
        RsvpFoodPreferenceModelBuilder.Configure(builder.Entity<RsvpFoodPreference>());
        RsvpAccommodationModelBuilder.Configure(builder.Entity<RsvpAccommodation>());
        RsvpCustomAnswerModelBuilder.Configure(builder.Entity<RsvpCustomAnswer>());
    }
}
