using CF.Events.Web.Data.Comparers;
using CF.Events.Web.Data.Converters;
using CF.Events.Web.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CF.Events.Web.Data.ModelBuilders;

/// <summary>
/// Model builder configuration for RsvpPerson.
/// </summary>
public static class RsvpPersonModelBuilder
{
    public static void Configure(EntityTypeBuilder<RsvpPerson> builder)
    {
        builder.ToTable("RsvpPersons", "rsvps");

        builder.HasKey(rp => rp.Id);

        builder.Property(rp => rp.RsvpId)
            .IsRequired();

        builder.Property(rp => rp.InvitedPersonId)
            .IsRequired(false);

        builder.Property(rp => rp.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(rp => rp.Email)
            .HasMaxLength(255)
            .IsRequired(false);

        builder.Property(rp => rp.IsPlusOne)
            .HasDefaultValue(false);

        builder.Property(rp => rp.IsPrimary)
            .HasDefaultValue(false);

        builder.Property(rp => rp.Attending)
            .HasDefaultValue(true);

        // Navigation: Rsvp
        builder.HasOne(rp => rp.Rsvp)
            .WithMany(r => r.People)
            .HasForeignKey(rp => rp.RsvpId)
            .OnDelete(DeleteBehavior.Cascade);

        // Navigation: InvitedPerson (1:1, optional)
        builder.HasOne(rp => rp.InvitedPerson)
            .WithOne(ip => ip.RsvpPerson)
            .HasForeignKey<RsvpPerson>(rp => rp.InvitedPersonId)
            .OnDelete(DeleteBehavior.NoAction)
            .IsRequired(false);

        // Navigation: FoodPreferences - Configured in RsvpFoodPreferenceModelBuilder (owns FK)
        // Do not duplicate relationship configuration

        // Navigation: Accommodations - Configured in RsvpAccommodationModelBuilder (owns FK)
        // Do not duplicate relationship configuration
    }
}
