using CF.Events.Web.Data.Comparers;
using CF.Events.Web.Data.Converters;
using CF.Events.Web.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CF.Events.Web.Data.ModelBuilders;

/// <summary>
/// Model builder configuration for Rsvp.
/// </summary>
public static class RsvpModelBuilder
{
    public static void Configure(EntityTypeBuilder<Rsvp> builder)
    {
        builder.ToTable("Rsvps", "rsvps");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.EventId)
            .IsRequired();

        builder.Property(r => r.InvitationId)
            .IsRequired();

        builder.Property(r => r.Status)
            .HasDefaultValue(RsvpStatus.InProgress);

        builder.Property(r => r.Comments)
            .HasMaxLength(500)
            .IsRequired(false);

        builder.Property(r => r.GroupName)
            .HasMaxLength(100)
            .IsRequired(false);

        builder.Property(r => r.CreatedAt)
            .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(r => r.UpdatedAt)
            .IsRequired(false);

        builder.Property(r => r.SubmittedAt)
            .IsRequired(false);

        // Kids count per age bracket
        builder.Property(r => r.KidsDetails)
            .HasConversion(new DictionaryConverter<KidAgeBracket, int>()!, new DictionaryComparer<KidAgeBracket, int>())
            .IsRequired(false);

        // Navigation: Event
        builder.HasOne(r => r.Event)
            .WithMany(e => e.Rsvps)
            .HasForeignKey(r => r.EventId)
            .OnDelete(DeleteBehavior.NoAction);

        // Navigation: Invitation (1:1)
        builder.HasOne(r => r.Invitation)
            .WithOne(i => i.Rsvp)
            .HasForeignKey<Rsvp>(r => r.InvitationId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
