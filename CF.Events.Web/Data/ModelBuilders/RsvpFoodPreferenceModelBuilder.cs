using CF.Events.Web.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CF.Events.Web.Data.ModelBuilders;

/// <summary>
/// Model builder configuration for RsvpFoodPreference.
/// </summary>
public static class RsvpFoodPreferenceModelBuilder
{
    public static void Configure(EntityTypeBuilder<RsvpFoodPreference> builder)
    {
        builder.ToTable("RsvpFoodPreferences", "rsvps");

        builder.HasKey(f => f.Id);

        builder.Property(f => f.RsvpPersonId)
            .IsRequired();

        builder.Property(f => f.EventDayId)
            .IsRequired();

        builder.Property(f => f.JoinsForBreakfast)
            .HasDefaultValue(false);

        builder.Property(f => f.JoinsForLunch)
            .HasDefaultValue(false);

        builder.Property(f => f.JoinsForDinner)
            .HasDefaultValue(false);

        builder.Property(f => f.JoinsForBrunch)
            .HasDefaultValue(false);

        builder.Property(f => f.Notes)
            .HasMaxLength(500)
            .IsRequired(false);

        // Composite unique index to prevent duplicate entries
        builder.HasIndex(f => new { f.RsvpPersonId, f.EventDayId })
            .IsUnique();

        // Navigation: RsvpPerson
        builder.HasOne(f => f.RsvpPerson)
            .WithMany(rp => rp.FoodPreferences)
            .HasForeignKey(f => f.RsvpPersonId)
            .OnDelete(DeleteBehavior.Cascade);

        // Navigation: EventDay
        builder.HasOne(f => f.EventDay)
            .WithMany(ed => ed.FoodPreferences)
            .HasForeignKey(f => f.EventDayId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
