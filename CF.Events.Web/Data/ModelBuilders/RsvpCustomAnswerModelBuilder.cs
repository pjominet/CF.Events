using CF.Events.Web.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CF.Events.Web.Data.ModelBuilders;

/// <summary>
/// Model builder configuration for RsvpCustomAnswer.
/// </summary>
public static class RsvpCustomAnswerModelBuilder
{
    public static void Configure(EntityTypeBuilder<RsvpCustomAnswer> builder)
    {
        builder.ToTable("RsvpCustomAnswers", "rsvps");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.RsvpId)
            .IsRequired();

        builder.Property(a => a.CustomQuestionId)
            .IsRequired();

        builder.Property(a => a.TextValue)
            .HasMaxLength(1000)
            .IsRequired(false);

        builder.Property(a => a.BooleanValue)
            .IsRequired(false);

        builder.Property(a => a.NumberValue)
            .IsRequired(false);

        builder.Property(a => a.DateValue)
            .IsRequired(false);

        // For MultiChoice - store as JSON array
        builder.Property(a => a.SelectedOptions)
            .IsRequired(false);

        // Composite unique index to prevent duplicate answers
        builder.HasIndex(a => new { a.RsvpId, a.CustomQuestionId })
            .IsUnique();

        // Navigation: Rsvp
        builder.HasOne(a => a.Rsvp)
            .WithMany(r => r.CustomAnswers)
            .HasForeignKey(a => a.RsvpId)
            .OnDelete(DeleteBehavior.Cascade);

        // Navigation: Question
        builder.HasOne(a => a.Question)
            .WithMany(cq => cq.Answers)
            .HasForeignKey(a => a.CustomQuestionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
