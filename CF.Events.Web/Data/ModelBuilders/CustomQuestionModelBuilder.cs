using CF.Events.Web.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CF.Events.Web.Data.ModelBuilders;

/// <summary>
/// Model builder configuration for CustomQuestion.
/// </summary>
public static class CustomQuestionModelBuilder
{
    public static void Configure(EntityTypeBuilder<CustomQuestion> builder)
    {
        builder.ToTable("CustomQuestions", "events");

        builder.HasKey(cq => cq.Id);

        builder.Property(cq => cq.EventId)
            .IsRequired();

        builder.Property(cq => cq.QuestionId)
            .HasMaxLength(36) // GUID length
            .IsRequired();

        builder.Property(cq => cq.Label)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(cq => cq.HelpText)
            .HasMaxLength(500)
            .IsRequired(false);

        builder.Property(cq => cq.Type)
            .HasDefaultValue(CustomQuestionType.Text);

        // Options for choice types - store as JSON
        builder.Property(cq => cq.Options)
            .IsRequired(false);

        builder.Property(cq => cq.IsRequired)
            .HasDefaultValue(false);

        builder.Property(cq => cq.SortOrder)
            .HasDefaultValue(0);

        builder.Property(cq => cq.StepGroup)
            .HasMaxLength(50)
            .HasDefaultValue("Extras");

        builder.Property(cq => cq.StepOrder)
            .HasDefaultValue(0);

        builder.Property(cq => cq.ShowIf)
            .HasMaxLength(100)
            .IsRequired(false);

        // Navigation: Event
        builder.HasOne(cq => cq.Event)
            .WithMany(e => e.CustomQuestions)
            .HasForeignKey(cq => cq.EventId)
            .OnDelete(DeleteBehavior.Cascade);

        // Navigation: Answers - Configured in RsvpCustomAnswerModelBuilder (owns FK)
        // Do not duplicate relationship configuration
    }
}
