using CF.Events.Web.Data;
using CF.Events.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using static CF.Events.Web.Infrastructure.Constants;

namespace CF.Events.Web.Controllers;

[Route("events/{eventId:int}/custom-questions")]
[Authorize(Roles = Roles.Admin)]
public class CustomQuestionsController(
    EventsDbContext db,
    ILogger<CustomQuestionsController> logger) : Controller
{
    /// <summary>
    /// Gets all custom questions for an event, ordered by StepGroup then SortOrder.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(int eventId)
    {
        var questions = await db.CustomQuestions
            .Where(q => q.EventId == eventId)
            .OrderBy(q => q.StepGroup)
            .ThenBy(q => q.SortOrder)
            .Select(q => new
            {
                q.Id,
                q.EventId,
                q.QuestionId,
                q.Label,
                q.HelpText,
                q.Type,
                q.Options,
                q.IsRequired,
                q.SortOrder,
                q.StepGroup,
                q.StepOrder,
                q.ShowIf
            })
            .ToListAsync();

        return Ok(questions);
    }

    /// <summary>
    /// Gets a single custom question by ID.
    /// </summary>
    [HttpGet("{questionId:int}")]
    public async Task<IActionResult> Get(int eventId, int questionId)
    {
        var question = await db.CustomQuestions
            .Where(q => q.EventId == eventId && q.Id == questionId)
            .Select(q => new
            {
                q.Id,
                q.EventId,
                q.QuestionId,
                q.Label,
                q.HelpText,
                q.Type,
                q.Options,
                q.IsRequired,
                q.SortOrder,
                q.StepGroup,
                q.StepOrder,
                q.ShowIf
            })
            .FirstOrDefaultAsync();

        if (question is null)
            return NotFound();

        return Ok(question);
    }

    /// <summary>
    /// Creates a new custom question for an event.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create(int eventId, [FromBody] CustomQuestionRequest request)
    {
        var eventExists = await db.Events.AnyAsync(e => e.Id == eventId);
        if (!eventExists)
            return NotFound("Event not found");

        if (request.Type is CustomQuestionType.SingleChoice or CustomQuestionType.MultiChoice
            && (request.Options is null || request.Options.Count == 0))
        {
            return BadRequest("Choice-type questions must have at least one option");
        }

        // Auto-assign sort order if not provided
        if (request.SortOrder == 0)
        {
            var maxOrder = await db.CustomQuestions
                .Where(q => q.EventId == eventId && q.StepGroup == request.StepGroup)
                .MaxAsync(q => (int?)q.SortOrder) ?? 0;

            request.SortOrder = maxOrder + 1;
        }

        var question = new CustomQuestion
        {
            EventId = eventId,
            Label = request.Label,
            HelpText = request.HelpText,
            Type = request.Type,
            Options = request.Options,
            IsRequired = request.IsRequired,
            SortOrder = request.SortOrder,
            StepGroup = request.StepGroup,
            StepOrder = request.StepOrder,
            ShowIf = request.ShowIf
        };

        db.CustomQuestions.Add(question);
        await db.SaveChangesAsync();

        logger.LogInformation("Created custom question {QuestionId} for event {EventId}", question.Id, eventId);

        return CreatedAtAction(nameof(Get), new { eventId, questionId = question.Id }, new
        {
            question.Id,
            question.EventId,
            question.QuestionId,
            question.Label,
            question.HelpText,
            question.Type,
            question.Options,
            question.IsRequired,
            question.SortOrder,
            question.StepGroup,
            question.StepOrder,
            question.ShowIf
        });
    }

    /// <summary>
    /// Updates an existing custom question.
    /// </summary>
    [HttpPut("{questionId:int}")]
    public async Task<IActionResult> Update(int eventId, int questionId, [FromBody] CustomQuestionRequest request)
    {
        var question = await db.CustomQuestions
            .FirstOrDefaultAsync(q => q.EventId == eventId && q.Id == questionId);

        if (question is null)
            return NotFound();

        if (request.Type is CustomQuestionType.SingleChoice or CustomQuestionType.MultiChoice
            && (request.Options is null || request.Options.Count == 0))
        {
            return BadRequest("Choice-type questions must have at least one option");
        }

        question.Label = request.Label;
        question.HelpText = request.HelpText;
        question.Type = request.Type;
        question.Options = request.Options;
        question.IsRequired = request.IsRequired;
        question.SortOrder = request.SortOrder;
        question.StepGroup = request.StepGroup;
        question.StepOrder = request.StepOrder;
        question.ShowIf = request.ShowIf;

        await db.SaveChangesAsync();

        logger.LogInformation("Updated custom question {QuestionId} for event {EventId}", questionId, eventId);

        return Ok(new
        {
            question.Id,
            question.EventId,
            question.QuestionId,
            question.Label,
            question.HelpText,
            question.Type,
            question.Options,
            question.IsRequired,
            question.SortOrder,
            question.StepGroup,
            question.StepOrder,
            question.ShowIf
        });
    }

    /// <summary>
    /// Deletes a custom question and its related answers.
    /// </summary>
    [HttpDelete("{questionId:int}")]
    public async Task<IActionResult> Delete(int eventId, int questionId)
    {
        var question = await db.CustomQuestions
            .FirstOrDefaultAsync(q => q.EventId == eventId && q.Id == questionId);

        if (question is null)
            return NotFound();

        // Cascade delete will handle related RsvpCustomAnswer records
        db.CustomQuestions.Remove(question);
        await db.SaveChangesAsync();

        logger.LogInformation("Deleted custom question {QuestionId} for event {EventId}", questionId, eventId);

        return NoContent();
    }

    /// <summary>
    /// Reorders custom questions within a step group.
    /// </summary>
    [HttpPost("reorder")]
    public async Task<IActionResult> Reorder(int eventId, [FromBody] ReorderRequest request)
    {
        var questions = await db.CustomQuestions
            .Where(q => q.EventId == eventId && request.QuestionIds.Contains(q.Id))
            .ToListAsync();

        if (questions.Count != request.QuestionIds.Count)
            return BadRequest("One or more question IDs are invalid");

        for (var i = 0; i < request.QuestionIds.Count; i++)
        {
            var question = questions.First(q => q.Id == request.QuestionIds[i]);
            question.SortOrder = i + 1;
        }

        await db.SaveChangesAsync();

        logger.LogInformation("Reordered {Count} custom questions for event {EventId}", questions.Count, eventId);

        return Ok(new { Message = $"Reordered {questions.Count} question(s)" });
    }
}

public class CustomQuestionRequest
{
    public string Label { get; set; } = string.Empty;
    public string? HelpText { get; set; }
    public CustomQuestionType Type { get; set; }
    public List<string>? Options { get; set; }
    public bool IsRequired { get; set; }
    public int SortOrder { get; set; }
    public string StepGroup { get; set; } = "Extras";
    public int StepOrder { get; set; }
    public string? ShowIf { get; set; }
}

public class ReorderRequest
{
    public List<int> QuestionIds { get; set; } = [];
}
