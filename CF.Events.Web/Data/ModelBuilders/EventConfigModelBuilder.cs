using CF.Events.Web.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CF.Events.Web.Data.ModelBuilders;

/// <summary>
/// Model builder configuration for EventConfig.
/// </summary>
public static class EventConfigModelBuilder
{
    public static void Configure(EntityTypeBuilder<EventConfig> builder)
    {
        builder.ToTable("EventConfigs", "events");

        builder.HasKey(ec => ec.EventId);

        builder.Property(ec => ec.EventId)
            .ValueGeneratedNever();

        builder.Property(ec => ec.ShowAccommodationOptions)
            .HasDefaultValue(false);

        builder.Property(ec => ec.AccommodationLink)
            .HasMaxLength(500)
            .IsRequired(false);

        builder.Property(ec => ec.AccommodationInfo)
            .HasMaxLength(1000)
            .IsRequired(false);

        builder.Property(ec => ec.AllowComments)
            .HasDefaultValue(true);

        builder.Property(ec => ec.AllowKids)
            .HasDefaultValue(true);

        // Navigation: Event (1:1)
        builder.HasOne(ec => ec.Event)
            .WithOne(e => e.EventConfig)
            .HasForeignKey<EventConfig>(ec => ec.EventId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired(false);
    }
}
