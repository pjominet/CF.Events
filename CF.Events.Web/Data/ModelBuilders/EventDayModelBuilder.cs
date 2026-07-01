using CF.Events.Web.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CF.Events.Web.Data.ModelBuilders;

/// <summary>
/// Model builder configuration for EventDay.
/// </summary>
public static class EventDayModelBuilder
{
    public static void Configure(EntityTypeBuilder<EventDay> builder)
    {
        builder.ToTable("EventDays", "events");

        builder.HasKey(ed => ed.Id);

        builder.Property(ed => ed.EventId)
            .IsRequired();

        builder.Property(ed => ed.Date)
            .IsRequired();

        builder.Property(ed => ed.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(ed => ed.OffersFood)
            .HasDefaultValue(true);

        builder.Property(ed => ed.OffersAccommodation)
            .HasDefaultValue(true);

        // Composite unique index: one day per event per date
        builder.HasIndex(ed => new { ed.EventId, ed.Date })
            .IsUnique();

        // Navigation: Event
        builder.HasOne(ed => ed.Event)
            .WithMany(e => e.EventDays)
            .HasForeignKey(ed => ed.EventId)
            .OnDelete(DeleteBehavior.Cascade);

        // Navigation: FoodPreferences - Configured in RsvpFoodPreferenceModelBuilder (owns FK)
        // Do not duplicate relationship configuration

        // Navigation: Accommodations - Configured in RsvpAccommodationModelBuilder (owns FK)
        // Do not duplicate relationship configuration
    }
}
