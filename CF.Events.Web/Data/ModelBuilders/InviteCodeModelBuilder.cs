using CF.Events.Web.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CF.Events.Web.Data.ModelBuilders;

/// <summary>
/// Model builder configuration for InviteCode.
/// </summary>
public static class InviteCodeModelBuilder
{
    public static void Configure(EntityTypeBuilder<InviteCode> builder)
    {
        builder.ToTable("InviteCodes", "invitations");

        builder.HasKey(ic => ic.Id);

        builder.Property(ic => ic.Code)
            .HasMaxLength(100)
            .IsRequired();

        builder.HasIndex(ic => ic.Code)
            .IsUnique();

        builder.Property(ic => ic.Label)
            .HasMaxLength(100)
            .IsRequired(false);

        builder.Property(ic => ic.EventId)
            .IsRequired();

        builder.Property(ic => ic.ValidUntil)
            .IsRequired();

        builder.Property(ic => ic.CreatedAt)
            .HasDefaultValueSql("GETUTCDATE()");

        // Navigation: Event
        builder.HasOne(ic => ic.Event)
            .WithMany(e => e.InviteCodes)
            .HasForeignKey(ic => ic.EventId)
            .OnDelete(DeleteBehavior.Cascade);

        // Navigation: Invitations - Configured in InvitationModelBuilder (owns FK)
        // Do not duplicate relationship configuration
    }
}
