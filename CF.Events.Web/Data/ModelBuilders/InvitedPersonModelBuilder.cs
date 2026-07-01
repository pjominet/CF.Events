using CF.Events.Web.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CF.Events.Web.Data.ModelBuilders;

/// <summary>
/// Model builder configuration for InvitedPerson.
/// </summary>
public static class InvitedPersonModelBuilder
{
    public static void Configure(EntityTypeBuilder<InvitedPerson> builder)
    {
        builder.ToTable("InvitedPersons", "invitations");

        builder.HasKey(ip => ip.Id);

        builder.Property(ip => ip.InvitationId)
            .IsRequired();

        builder.Property(ip => ip.UserId)
            .HasMaxLength(450)
            .IsRequired(false);

        builder.Property(ip => ip.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(ip => ip.Email)
            .HasMaxLength(255)
            .IsRequired(false);

        builder.Property(ip => ip.IsPrimary)
            .HasDefaultValue(false);

        builder.Property(ip => ip.Status)
            .HasDefaultValue(PersonInviteStatus.Pending);

        builder.Property(ip => ip.AssignedAccommodationCode)
            .HasMaxLength(100)
            .IsRequired(false);

        builder.Property(ip => ip.InvitationToken)
            .HasMaxLength(128)
            .IsRequired(false);

        builder.Property(ip => ip.InvitationTokenExpiresAt)
            .IsRequired(false);

        builder.HasIndex(ip => ip.InvitationToken)
            .IsUnique()
            .HasFilter("[InvitationToken] IS NOT NULL");

        // Navigation: Invitation
        builder.HasOne(ip => ip.Invitation)
            .WithMany(i => i.InvitedPersons)
            .HasForeignKey(ip => ip.InvitationId)
            .OnDelete(DeleteBehavior.Cascade);

        // Navigation: User
        builder.HasOne(ip => ip.User)
            .WithMany(u => u.InvitedPersons)
            .HasForeignKey(ip => ip.UserId)
            .OnDelete(DeleteBehavior.NoAction)
            .IsRequired(false);

        // Navigation: RsvpPerson (1:1) - Configured in RsvpPersonModelBuilder (owns FK)
        // Do not duplicate relationship configuration
    }
}
