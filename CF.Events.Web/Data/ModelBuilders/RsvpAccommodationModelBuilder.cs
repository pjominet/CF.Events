using CF.Events.Web.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CF.Events.Web.Data.ModelBuilders;

/// <summary>
/// Model builder configuration for RsvpAccommodation.
/// </summary>
public static class RsvpAccommodationModelBuilder
{
    public static void Configure(EntityTypeBuilder<RsvpAccommodation> builder)
    {
        builder.ToTable("RsvpAccommodations", "rsvps");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.RsvpPersonId)
            .IsRequired();

        builder.Property(a => a.EventDayId)
            .IsRequired();

        builder.Property(a => a.NeedsAccommodation)
            .HasDefaultValue(false);

        builder.Property(a => a.RoomType)
            .HasMaxLength(100)
            .IsRequired(false);

        builder.Property(a => a.SpecialRequests)
            .HasMaxLength(500)
            .IsRequired(false);

        // Composite unique index to prevent duplicate entries
        builder.HasIndex(a => new { a.RsvpPersonId, a.EventDayId })
            .IsUnique();

        // Navigation: RsvpPerson
        builder.HasOne(a => a.RsvpPerson)
            .WithMany(rp => rp.Accommodations)
            .HasForeignKey(a => a.RsvpPersonId)
            .OnDelete(DeleteBehavior.Cascade);

        // Navigation: EventDay
        builder.HasOne(a => a.EventDay)
            .WithMany(ed => ed.Accommodations)
            .HasForeignKey(a => a.EventDayId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
