using CF.Events.Web.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CF.Events.Web.Data.ModelBuilders;

/// <summary>
/// Model builder configuration for Invitation.
/// </summary>
public static class InvitationModelBuilder
{
    public static void Configure(EntityTypeBuilder<Invitation> builder)
    {
        builder.ToTable("Invitations", "invitations");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.EventId)
            .IsRequired();

        builder.Property(i => i.InviteCodeId)
            .IsRequired(false);

        builder.Property(i => i.GroupName)
            .HasMaxLength(100)
            .IsRequired(false);

        builder.Property(i => i.Notes)
            .HasMaxLength(500)
            .IsRequired(false);

        builder.Property(i => i.Status)
            .HasDefaultValue(InvitationStatus.Pending);

        builder.Property(i => i.CreatedAt)
            .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(i => i.UpdatedAt)
            .IsRequired(false);

        builder.Property(i => i.ScheduledFor)
            .IsRequired(false);

        builder.Property(i => i.InviteEmailSent)
            .HasDefaultValue(false);

        builder.Property(i => i.AssignedAccommodationCode)
            .HasMaxLength(100)
            .IsRequired(false);

        // Navigation: Event
        builder.HasOne(i => i.Event)
            .WithMany(e => e.Invitations)
            .HasForeignKey(i => i.EventId)
            .OnDelete(DeleteBehavior.Cascade);

        // Navigation: InviteCode
        builder.HasOne(i => i.InviteCode)
            .WithMany(ic => ic.Invitations)
            .HasForeignKey(i => i.InviteCodeId)
            .OnDelete(DeleteBehavior.NoAction)
            .IsRequired(false);

        // Navigation: InvitedPersons - Configured in InvitedPersonModelBuilder (owns FK)
        // Do not duplicate relationship configuration

        // Navigation: Rsvp (1:1) - Configured in RsvpModelBuilder
        // builder.HasOne(i => i.Rsvp)
        //     .WithOne(r => r.Invitation)
        //     .HasForeignKey<Rsvp>(r => r.InvitationId)
        //     .OnDelete(DeleteBehavior.Cascade)
        //     .IsRequired(false);
    }
}
