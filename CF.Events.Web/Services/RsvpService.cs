using CF.Events.Web.Data;
using CF.Events.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace CF.Events.Web.Services;

public interface IRsvpService
{
    /// <summary>
    /// Gets the RSVP form data for an invitation.
    /// </summary>
    Task<RsvpFormResponse?> GetRsvpFormAsync(int invitationId, string userId);

    /// <summary>
    /// Saves or updates an RSVP as draft or submitted.
    /// </summary>
    Task<RsvpSubmissionResponse> SaveRsvpAsync(RsvpRequest request, string userId);

    /// <summary>
    /// Gets the current RSVP status for an invitation.
    /// </summary>
    Task<Rsvp?> GetCurrentRsvpAsync(int invitationId);
}

public class RsvpService(
    EventsDbContext db,
    ILogger<RsvpService> logger) : IRsvpService
{
    public async Task<RsvpFormResponse?> GetRsvpFormAsync(int invitationId, string userId)
    {
        // Get the invitation with event and invited persons
        var invitation = await db.Invitations
            .Include(i => i.Event)
                .ThenInclude(e => e.EventConfig)
            .Include(i => i.Event)
                .ThenInclude(e => e.EventDays)
            .Include(i => i.Event)
                .ThenInclude(e => e.CustomQuestions)
            .Include(i => i.InvitedPersons)
                .ThenInclude(ip => ip.User)
            .Include(i => i.Rsvp)
                .ThenInclude(r => r!.People)
            .Include(i => i.Rsvp)
                .ThenInclude(r => r!.CustomAnswers)
            .AsSplitQuery()
            .FirstOrDefaultAsync(i => i.Id == invitationId);

        if (invitation?.Event is null)
        {
            logger.LogWarning("Invitation {InvitationId} not found or has no event", invitationId);
            return null;
        }

        var eventObj = invitation.Event;
        var now = DateTime.UtcNow;

        // Build the response
        var response = new RsvpFormResponse
        {
            EventId = eventObj.Id,
            EventName = eventObj.Name,
            EventDescription = eventObj.Description ?? string.Empty,
            EventStartDate = eventObj.StartDate,
            EventEndDate = eventObj.EndDate,
            Location = eventObj.Location,
            InvitationId = invitation.Id,
            InvitationGroupName = invitation.GroupName,
            AssignedAccommodationCode = invitation.AssignedAccommodationCode,
            ShowAccommodationOptions = invitation.Event.EventConfig?.ShowAccommodationOptions ?? false,
            AccommodationLink = invitation.Event.EventConfig?.AccommodationLink,
            AccommodationInfo = invitation.Event.EventConfig?.AccommodationInfo,
            AllowComments = invitation.Event.EventConfig?.AllowComments ?? true,
            AllowKids = invitation.Event.EventConfig?.AllowKids ?? true,
            // Map event days
            EventDays = eventObj.EventDays
                .OrderBy(ed => ed.Date)
                .Select(ed => new EventDayResponse
                {
                    Id = ed.Id,
                    Date = ed.Date,
                    Name = ed.Name,
                    OffersFood = ed.OffersFood,
                    OffersAccommodation = ed.OffersAccommodation
                })
                .ToList(),
            // Map invited persons
            InvitedGuests = invitation.InvitedPersons
                .Select(ip => new InvitedPersonResponse
                {
                    Id = ip.Id,
                    Name = ip.Name,
                    Email = ip.Email,
                    IsPrimary = ip.IsPrimary,
                    IsUser = ip.UserId == userId,
                    LinkedPersonId = ip.LinkedPersonId
                })
                .ToList(),
            // Map custom questions
            CustomQuestions = eventObj.CustomQuestions
                .OrderBy(cq => cq.SortOrder)
                .Select(cq => new CustomQuestionResponse
                {
                    Id = cq.Id,
                    QuestionId = cq.QuestionId,
                    Label = cq.Label,
                    HelpText = cq.HelpText,
                    Type = cq.Type,
                    Options = cq.Options,
                    IsRequired = cq.IsRequired,
                    StepGroup = cq.FormStep,
                    StepOrder = cq.StepOrder,
                    ShowIf = cq.ShowIf
                })
                .ToList()
        };

        // If there's an existing RSVP, map it to the response
        if (invitation.Rsvp is not null)
        {
            response.ExistingRsvp = new ExistingRsvpData
            {
                RsvpId = invitation.Rsvp.Id,
                GroupName = invitation.Rsvp.GroupName,
                Status = invitation.Rsvp.Status,
                Comments = invitation.Rsvp.Comments,
                KidsDetails = invitation.Rsvp.KidsDetails,
                People = invitation.Rsvp.People
                    .Select(rp => new ExistingRsvpPersonData
                    {
                        Id = rp.Id,
                        InvitedPersonId = rp.InvitedPersonId,
                        Name = rp.Name,
                        Email = rp.Email,
                        IsPlusOne = rp.IsPlusOne,
                        IsPrimary = rp.IsPrimary,
                        Attending = rp.Attending,
                    })
                    .ToList(),
                FoodPreferences = [], // Will be loaded separately if needed
                Accommodations = [], // Will be loaded separately if needed
                CustomAnswers = invitation.Rsvp.CustomAnswers
                    .Select(ca => new ExistingRsvpCustomAnswerData
                    {
                        Id = ca.Id,
                        CustomQuestionId = ca.CustomQuestionId,
                        TextValue = ca.TextValue,
                        BooleanValue = ca.BooleanValue,
                        NumberValue = ca.NumberValue,
                        DateValue = ca.DateValue,
                        SelectedOptions = ca.SelectedOptions
                    })
                    .ToList()
            };

            // Load food preferences and accommodations for existing RSVP
            if (invitation.Rsvp.People.Count > 0 && response.EventDays.Count > 0)
            {
                var eventDayIds = response.EventDays.Select(ed => ed.Id).ToList();
                var rsvpPersonIds = invitation.Rsvp.People.Select(rp => rp.Id).ToList();

                // Load food preferences
                var foodPrefs = await db.RsvpFoodPreferences
                    .Where(f => rsvpPersonIds.Contains(f.RsvpPersonId) && eventDayIds.Contains(f.EventDayId))
                    .ToListAsync();

                response.ExistingRsvp.FoodPreferences = foodPrefs
                    .Select(f => new ExistingRsvpFoodPreferenceData
                    {
                        Id = f.Id,
                        RsvpPersonId = f.RsvpPersonId,
                        EventDayId = f.EventDayId,
                        DietaryOption = f.DietaryOption,
                        SpecialRequests = f.SpecialRequests
                    })
                    .ToList();

                // Load accommodations
                var accommodations = await db.RsvpAccommodations
                    .Where(a => rsvpPersonIds.Contains(a.RsvpPersonId) && eventDayIds.Contains(a.EventDayId))
                    .ToListAsync();

                response.ExistingRsvp.Accommodations = accommodations
                    .Select(a => new ExistingRsvpAccommodationData
                    {
                        Id = a.Id,
                        RsvpPersonId = a.RsvpPersonId,
                        EventDayId = a.EventDayId,
                        HasBooked = a.HasBooked
                    })
                    .ToList();
            }
        }

        // Set image URL if available
        if (!string.IsNullOrEmpty(eventObj.InvitationFileName))
        {
            response.ImageUrl = $"/events/{eventObj.Id}/asset";
        }

        return response;
    }

    public async Task<RsvpSubmissionResponse> SaveRsvpAsync(RsvpRequest request, string userId)
    {
        var response = new RsvpSubmissionResponse
        {
            Success = false,
            SubmittedAt = DateTime.UtcNow
        };

        try
        {
            // Validate the invitation exists and user is authorized
            var invitation = await db.Invitations
                .Include(i => i.Event)
                .Include(i => i.InvitedPersons)
                .FirstOrDefaultAsync(i => i.Id == request.InvitationId);

            if (invitation is null)
            {
                response.Errors.Add("Invitation not found");
                return response;
            }

            // Check if user is authorized to RSVP for this invitation
            var isAuthorized = invitation.InvitedPersons.Any(ip => ip.PrimaryGroupUserId == userId);
            if (!isAuthorized)
            {
                response.Errors.Add("You are not authorized to RSVP for this invitation");
                return response;
            }

            // Check if RSVP is allowed (event is active, etc.)
            if (invitation.Event is null || !invitation.Event.IsActive)
            {
                response.Errors.Add("This event is no longer accepting RSVPs");
                return response;
            }

            // Get existing RSVP or create new one
            var existingRsvp = await db.Rsvps
                .Include(r => r.People)
                .Include(r => r.CustomAnswers)
                .FirstOrDefaultAsync(r => r.InvitationId == request.InvitationId);

            Rsvp rsvp;
            var isNewRsvp = existingRsvp is null;

            if (isNewRsvp)
            {
                rsvp = new Rsvp
                {
                    EventId = invitation.EventId,
                    InvitationId = request.InvitationId,
                    Status = RsvpStatus.InProgress,
                    CreatedAt = DateTime.UtcNow
                };
                db.Rsvps.Add(rsvp);
            }
            else
            {
                rsvp = existingRsvp!;
                rsvp.UpdatedAt = DateTime.UtcNow;
            }

            // Update RSVP fields
            rsvp.GroupName = request.GroupName ?? invitation.GroupName;
            rsvp.Comments = request.Comments;
            rsvp.KidsDetails = request.KidsDetails;

            // Update RSVP people
            await UpdateRsvpPeopleAsync(rsvp, request.People, invitation);

            // Save people first so they get IDs assigned (needed for food/accommodation FKs)
            await db.SaveChangesAsync();

            // Update food preferences (requires valid RsvpPerson IDs)
            await UpdateFoodPreferencesAsync(rsvp, request.FoodPreferences);

            // Update accommodations (requires valid RsvpPerson IDs)
            await UpdateAccommodationsAsync(rsvp, request.Accommodations);

            // Update custom answers
            await UpdateCustomAnswersAsync(rsvp, request.CustomAnswers);

            // If this is a final submission (not draft), update status and submitted date
            if (!request.IsDraft)
            {
                rsvp.Status = RsvpStatus.Submitted;
                rsvp.SubmittedAt = DateTime.UtcNow;
            }

            // Save food, accommodation, custom answers, and status
            await db.SaveChangesAsync();

            response.Success = true;
            response.RsvpId = rsvp.Id;
            response.Status = rsvp.Status;
            response.Message = isNewRsvp
                ? (request.IsDraft ? "RSVP draft saved successfully" : "RSVP submitted successfully")
                : (request.IsDraft ? "RSVP draft updated successfully" : "RSVP updated successfully");

            logger.LogInformation("RSVP {Action} for invitation {InvitationId} by user {UserId}",
                request.IsDraft ? "saved as draft" : "submitted",
                request.InvitationId,
                userId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error saving RSVP for invitation {InvitationId}", request.InvitationId);
            response.Errors.Add("An error occurred while saving your RSVP. Please try again.");
        }

        return response;
    }

    public async Task<Rsvp?> GetCurrentRsvpAsync(int invitationId)
    {
        return await db.Rsvps
            .Include(r => r.People)
            .Include(r => r.CustomAnswers)
            .FirstOrDefaultAsync(r => r.InvitationId == invitationId);
    }

    private async Task UpdateRsvpPeopleAsync(Rsvp rsvp, List<RsvpPersonRequest> personRequests, Invitation invitation)
    {
        var existingPersonIds = rsvp.People.Select(p => p.Id).ToHashSet();
        var requestPersonIds = personRequests.Where(p => p.Id.HasValue).Select(p => p.Id!.Value).ToHashSet();

        // Remove people that are no longer in the request
        var peopleToRemove = rsvp.People.Where(p => !requestPersonIds.Contains(p.Id)).ToList();
        foreach (var person in peopleToRemove)
        {
            db.RsvpPersons.Remove(person);
        }

        // Update or add people
        foreach (var personRequest in personRequests)
        {
            if (personRequest.Id.HasValue && existingPersonIds.Contains(personRequest.Id.Value))
            {
                // Update existing person
                var existingPerson = rsvp.People.First(p => p.Id == personRequest.Id.Value);
                UpdateRsvpPerson(existingPerson, personRequest, invitation);
            }
            else
            {
                // Add new person
                var newPerson = new RsvpPerson
                {
                    RsvpId = rsvp.Id,
                    InvitedPersonId = personRequest.InvitedPersonId,
                    Name = personRequest.Name,
                    Email = personRequest.Email,
                    IsPlusOne = personRequest.IsPlusOne,
                    IsPrimary = personRequest.IsPrimary,
                    Attending = personRequest.Attending,
                };
                rsvp.People.Add(newPerson);
                db.RsvpPersons.Add(newPerson);
            }
        }
    }

    private void UpdateRsvpPerson(RsvpPerson person, RsvpPersonRequest request, Invitation invitation)
    {
        person.Name = request.Name;
        person.Email = request.Email;
        person.IsPlusOne = request.IsPlusOne;
        person.IsPrimary = request.IsPrimary;
        person.Attending = request.Attending;

        // If InvitedPersonId is null and this was previously linked to an invited person,
        // we might need to unlink it (for plus ones that were converted from invited persons)
        if (request.InvitedPersonId.HasValue)
        {
            person.InvitedPersonId = request.InvitedPersonId.Value;
        }
        else if (person.InvitedPersonId.HasValue && request.InvitedPersonId is null)
        {
            // Unlink from invited person (converting to a true plus one)
            person.InvitedPersonId = null;
        }
    }

    private async Task UpdateFoodPreferencesAsync(Rsvp rsvp, List<RsvpFoodPreferenceRequest> preferenceRequests)
    {
        // First, remove all existing food preferences for this RSVP's people
        var rsvpPersonIds = rsvp.People.Select(p => p.Id).ToList();
        var existingPreferences = await db.RsvpFoodPreferences
            .Where(f => rsvpPersonIds.Contains(f.RsvpPersonId))
            .ToListAsync();

        db.RsvpFoodPreferences.RemoveRange(existingPreferences);

        // Add new food preferences
        foreach (var prefRequest in preferenceRequests)
        {
            var newPref = new RsvpFoodPreference
            {
                RsvpPersonId = prefRequest.RsvpPersonId,
                EventDayId = prefRequest.EventDayId,
                DietaryOption = prefRequest.DietaryOption,
                SpecialRequests = prefRequest.SpecialRequests
            };
            db.RsvpFoodPreferences.Add(newPref);
        }
    }

    private async Task UpdateAccommodationsAsync(Rsvp rsvp, List<RsvpAccommodationRequest> accommodationRequests)
    {
        // Accommodation is now event-level (simple "has booked" flag).
        // We store one record per primary person to persist the flag.
        var rsvpPersonIds = rsvp.People.Select(p => p.Id).ToList();
        var existingAccommodations = await db.RsvpAccommodations
            .Where(a => rsvpPersonIds.Contains(a.RsvpPersonId))
            .ToListAsync();

        db.RsvpAccommodations.RemoveRange(existingAccommodations);

        // Only create a record if there's a booking confirmation and a primary person
        var hasBooked = accommodationRequests.Any(a => a.HasBooked);
        var primaryPerson = rsvp.People.FirstOrDefault(p => p.IsPrimary) ?? rsvp.People.FirstOrDefault();
        if (primaryPerson != null && hasBooked)
        {
            // Use any event day as a placeholder FK (accommodation is event-level now)
            var eventDayId = accommodationRequests.FirstOrDefault(a => a.EventDayId > 0)?.EventDayId ?? 0;
            if (eventDayId == 0)
            {
                // Look up the first event day for this event
                eventDayId = await db.EventDays
                    .Where(ed => ed.EventId == rsvp.EventId)
                    .Select(ed => ed.Id)
                    .FirstOrDefaultAsync();
            }

            if (eventDayId > 0)
            {
                db.RsvpAccommodations.Add(new RsvpAccommodation
                {
                    RsvpPersonId = primaryPerson.Id,
                    EventDayId = eventDayId,
                    HasBooked = true
                });
            }
        }
    }

    private async Task UpdateCustomAnswersAsync(Rsvp rsvp, List<RsvpCustomAnswerRequest> answerRequests)
    {
        // Remove existing answers for this RSVP
        db.RsvpCustomAnswers.RemoveRange(rsvp.CustomAnswers);
        rsvp.CustomAnswers.Clear();

        // Add new answers
        foreach (var answerRequest in answerRequests)
        {
            var newAnswer = new RsvpCustomAnswer
            {
                RsvpId = rsvp.Id,
                CustomQuestionId = answerRequest.CustomQuestionId,
                TextValue = answerRequest.TextValue,
                BooleanValue = answerRequest.BooleanValue,
                NumberValue = answerRequest.NumberValue,
                DateValue = answerRequest.DateValue,
                SelectedOptions = answerRequest.SelectedOptions
            };
            rsvp.CustomAnswers.Add(newAnswer);
            db.RsvpCustomAnswers.Add(newAnswer);
        }
    }
}
