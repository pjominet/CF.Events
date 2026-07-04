using CF.Events.Web.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CF.Events.Web.Data.ModelBuilders;

/// <summary>
/// Model builder configuration for Event.
/// </summary>
public static class EventModelBuilder
{
    public static void Configure(EntityTypeBuilder<Event> builder)
    {
        builder.ToTable("Events", "events");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.StartDate)
            .IsRequired();

        builder.Property(e => e.EndDate)
            .IsRequired();

        builder.Property(e => e.Description)
            .HasMaxLength(500)
            .IsRequired(false);

        builder.Property(e => e.Location)
            .HasMaxLength(100)
            .IsRequired(false);

        builder.Property(e => e.InvitationFileName)
            .HasMaxLength(255)
            .IsRequired(false);

        builder.Property(e => e.OriginalInvitationFileName)
            .HasMaxLength(255)
            .IsRequired(false);

        builder.Property(e => e.IsActive)
            .HasDefaultValue(true);

        builder.Property(e => e.CreatedAt)
            .HasDefaultValueSql("GETUTCDATE()");
    }
}
