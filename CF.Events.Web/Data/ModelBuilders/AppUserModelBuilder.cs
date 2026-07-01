using CF.Events.Web.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CF.Events.Web.Data.ModelBuilders;

/// <summary>
/// Model builder configuration for AppUser (Identity user extension).
/// </summary>
public static class AppUserModelBuilder
{
    public static void Configure(EntityTypeBuilder<AppUser> builder)
    {
        builder.ToTable("Users", "identity");

        builder.Property(u => u.DisplayName)
            .HasMaxLength(100)
            .IsRequired(false);

        builder.Property(u => u.MustChangePassword)
            .HasDefaultValue(false);

        builder.Property(u => u.IsActive)
            .HasDefaultValue(true);

        // Navigation: InvitedPersons - Configured in InvitedPersonModelBuilder (owns FK)
        // Do not duplicate relationship configuration
    }
}
