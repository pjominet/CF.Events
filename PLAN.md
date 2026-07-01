# RSVP System Redesign - Implementation Plan

## Overview
Redesign the event invitation and RSVP system to support flexible event types, group invitations, multi-day events, and per-person/per-day options while maintaining structured data for common fields.

**Approach**: Hybrid model (Approach 2) - Keep structured fields for common options, add flexible custom question/answer system for event-specific needs.

---

## Requirements Summary

### Keep (Structured)
- [ ] Attendance check (per person)
- [ ] Kids options (per person)
- [ ] Accommodation options (per person, per day)
- [ ] General comments (per RSVP group)

### Change/Remove
- [ ] Remove hardcoded meal flags from EventConfig (`OfferDinner`, `OfferLunch`, `OfferBreakfast`, `OfferBrunch`)
- [ ] Replace with per-day food offering system

### Add (Structured)
- [ ] Event duration: `EndDate` on Event (derive duration from Start/End dates)
- [ ] Plus one support: Option to add plus one name for solo invites
- [ ] **Group invitations**: Ability to invite couples/groups with a single RSVP
- [ ] Per-day food options (link food to EventDay, not just boolean flags)
- [ ] Dietary options: Linked to each person AND each day
- [ ] Accommodation: Per person, per day
- [ ] Accommodation reservation link in EventConfig (when AccommodationCode exists)
- [ ] **Stepper UI**: Multi-step form with logical grouping

### Add (Flexible - Hybrid)
- [ ] Custom question/answer system for event-specific fields

---

## Model Changes

### New Models to Create

#### 1. EventDay.cs
```csharp
// Represents a day within a multi-day event
public class EventDay
{
    public int Id { get; set; }
    public int EventId { get; set; }
    public DateTime Date { get; set; }
    public string? Name { get; set; } // Optional: "Day 1", "Wedding Day", etc.
    public bool OffersFood { get; set; } = true;
    public bool OffersAccommodation { get; set; } = true;
    
    // Navigation
    public Event Event { get; set; } = null!;
    public HashSet<RsvpFoodPreference> FoodPreferences { get; set; } = [];
    public HashSet<RsvpAccommodation> Accommodations { get; set; } = [];
}
```

**Status**: ⬜ Not Started

---

#### 2. Invitation.cs (Modified/Replaced)
**Current**: Simple record with event/user info
**New**: Full entity representing an invitation group

```csharp
public class Invitation
{
    public int Id { get; set; }
    
    [Required]
    public int EventId { get; set; }
    
    public int? InviteCodeId { get; set; } // Can be null for direct invites
    
    [StringLength(100)]
    public string? GroupName { get; set; } // "The Smith Family", "John & Jane"
    
    [StringLength(500)]
    public string? Notes { get; set; } // Internal notes for organizer
    
    public InvitationStatus Status { get; set; } = InvitationStatus.Pending;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    
    // Navigation
    public Event Event { get; set; } = null!;
    public InviteCode? InviteCode { get; set; }
    public HashSet<InvitedPerson> InvitedPersons { get; set; } = [];
    public Rsvp? Rsvp { get; set; }
}

public enum InvitationStatus
{
    Pending,
    Sent,
    Viewed,
    Responded,
    Cancelled
}
```

**Status**: ⬜ Not Started

---

#### 3. InvitedPerson.cs (New)
```csharp
public class InvitedPerson
{
    public int Id { get; set; }
    
    [Required]
    public int InvitationId { get; set; }
    
    [StringLength(450)]
    public string? UserId { get; set; } // Null for non-registered guests
    
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;
    
    [StringLength(255)]
    public string? Email { get; set; }
    
    public bool IsPrimary { get; set; } = false; // Main contact for the group
    
    public PersonInviteStatus Status { get; set; } = PersonInviteStatus.Pending;
    
    // Navigation
    public Invitation Invitation { get; set; } = null!;
    public AppUser? User { get; set; }
    public RsvpPerson? RsvpPerson { get; set; }
}

public enum PersonInviteStatus
{
    Pending,
    Invited,
    Responded,
    Declined,
    Cancelled
}
```

**Status**: ⬜ Not Started

---

#### 4. RsvpPerson.cs (New)
```csharp
public class RsvpPerson
{
    public int Id { get; set; }
    
    [Required]
    public int RsvpId { get; set; }
    
    public int? InvitedPersonId { get; set; } // Links to invited person, null for ad-hoc plus ones
    
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;
    
    [StringLength(255)]
    public string? Email { get; set; }
    
    public bool IsPlusOne { get; set; } = false;
    public bool IsPrimary { get; set; } = false; // Primary invitee in the group
    
    public bool Attending { get; set; } = true;
    
    // Dietary restrictions (applies across all days)
    public DietaryOptions[]? DietaryRestrictions { get; set; }
    
    [StringLength(500)]
    public string? OtherDietaryDetails { get; set; }
    
    // Navigation
    public Rsvp Rsvp { get; set; } = null!;
    public InvitedPerson? InvitedPerson { get; set; }
    public HashSet<RsvpKid> Kids { get; set; } = [];
    public HashSet<RsvpFoodPreference> FoodPreferences { get; set; } = [];
    public HashSet<RsvpAccommodation> Accommodations { get; set; } = [];
}
```

**Status**: ⬜ Not Started

---

#### 5. RsvpKid.cs (New)
```csharp
public class RsvpKid
{
    public int Id { get; set; }
    
    [Required]
    public int RsvpPersonId { get; set; }
    
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;
    
    public int Age { get; set; }
    
    [StringLength(100)]
    public string? AgeBracketCustom { get; set; } // Optional custom bracket name
    
    public bool Attending { get; set; } = true;
    
    // Navigation
    public RsvpPerson RsvpPerson { get; set; } = null!;
}
```

**Status**: ⬜ Not Started

---

#### 6. RsvpFoodPreference.cs (New)
```csharp
public class RsvpFoodPreference
{
    public int Id { get; set; }
    
    [Required]
    public int RsvpPersonId { get; set; }
    
    [Required]
    public int EventDayId { get; set; }
    
    public bool JoinsForBreakfast { get; set; }
    public bool JoinsForLunch { get; set; }
    public bool JoinsForDinner { get; set; }
    public bool JoinsForBrunch { get; set; }
    
    [StringLength(500)]
    public string? Notes { get; set; } // Special requests for this day
    
    // Navigation
    public RsvpPerson RsvpPerson { get; set; } = null!;
    public EventDay EventDay { get; set; } = null!;
}
```

**Status**: ⬜ Not Started

---

#### 7. RsvpAccommodation.cs (New)
```csharp
public class RsvpAccommodation
{
    public int Id { get; set; }
    
    [Required]
    public int RsvpPersonId { get; set; }
    
    [Required]
    public int EventDayId { get; set; } // Which night (stays the night of this day)
    
    public bool NeedsAccommodation { get; set; }
    
    [StringLength(100)]
    public string? RoomType { get; set; } // Single, Double, Family, etc.
    
    public int? NumberOfNights { get; set; } // Can derive from dates, but explicit is clearer
    
    [StringLength(500)]
    public string? SpecialRequests { get; set; }
    
    // Navigation
    public RsvpPerson RsvpPerson { get; set; } = null!;
    public EventDay EventDay { get; set; } = null!;
}
```

**Status**: ⬜ Not Started

---

#### 8. CustomQuestion.cs (New)
```csharp
public class CustomQuestion
{
    public int Id { get; set; }
    
    [Required]
    public int EventId { get; set; }
    
    [Required]
    public string QuestionId { get; set; } = Guid.NewGuid().ToString(); // For easy reference
    
    [Required]
    [StringLength(200)]
    public string Label { get; set; } = string.Empty;
    
    [StringLength(500)]
    public string? HelpText { get; set; }
    
    public CustomQuestionType Type { get; set; }
    
    // For choice types
    public List<string>? Options { get; set; }
    
    public bool IsRequired { get; set; } = false;
    
    public int SortOrder { get; set; }
    
    // For stepper grouping
    [StringLength(50)]
    public string StepGroup { get; set; } = "Extras"; // "Attendance", "Food", "Accommodation", "Extras", "Custom"
    
    public int StepOrder { get; set; } // Order within the step
    
    // Conditional display
    [StringLength(100)]
    public string? ShowIf { get; set; } // Expression: "Attending == true", "Kids.Count > 0"
    
    // Navigation
    public Event Event { get; set; } = null!;
    public HashSet<RsvpCustomAnswer> Answers { get; set; } = [];
}

public enum CustomQuestionType
{
    Text,
    TextArea,
    Boolean,
    SingleChoice,
    MultiChoice,
    Number,
    Date
}
```

**Status**: ⬜ Not Started

---

#### 9. RsvpCustomAnswer.cs (New)
```csharp
public class RsvpCustomAnswer
{
    public int Id { get; set; }
    
    [Required]
    public int RsvpId { get; set; }
    
    [Required]
    public int CustomQuestionId { get; set; }
    
    // Store answer based on type
    [StringLength(1000)]
    public string? TextValue { get; set; }
    
    public bool? BooleanValue { get; set; }
    
    public int? NumberValue { get; set; }
    
    public DateTime? DateValue { get; set; }
    
    // For MultiChoice
    public List<string>? SelectedOptions { get; set; }
    
    // Navigation
    public Rsvp Rsvp { get; set; } = null!;
    public CustomQuestion Question { get; set; } = null!;
}
```

**Status**: ⬜ Not Started

---

### Modified Models

#### 10. Event.cs
**Changes:**
- Add `EndDate` property
- Add navigation to `EventDays`
- Remove redundant properties if any

**Status**: ⬜ Not Started

---

#### 11. EventConfig.cs
**Changes:**
- Remove: `OfferDinner`, `OfferLunch`, `OfferBreakfast`, `OfferBrunch`
- Add: `AccommodationLink` (string, nullable) - URL to reservation pages
- Add: `AccommodationInfo` (string, nullable) - Additional text about accommodation
- Keep: `ShowAccommodationOptions`, `AllowComments`, `AllowPartners`, `AllowKids`
- Add: `MaxPlusOnesPerPerson` (int, nullable) - null = unlimited
- Add navigation to `CustomQuestions`

**Status**: ⬜ Not Started

---

#### 12. Rsvp.cs (Complete Redesign)
**Current structure will be replaced:**

```csharp
public class Rsvp
{
    public int Id { get; set; }
    
    [Required]
    public int EventId { get; set; }
    
    [Required]
    public int InvitationId { get; set; } // Links to the invitation group
    
    public RsvpStatus Status { get; set; } = RsvpStatus.InProgress;
    
    [StringLength(500)]
    public string? Comments { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public DateTime? SubmittedAt { get; set; }
    
    // Group info
    [StringLength(100)]
    public string? GroupName { get; set; } // Optional override from Invitation
    
    // Navigation
    public Event Event { get; set; } = null!;
    public Invitation Invitation { get; set; } = null!;
    public HashSet<RsvpPerson> People { get; set; } = [];
    public HashSet<RsvpCustomAnswer> CustomAnswers { get; set; } = [];
}

public enum RsvpStatus
{
    InProgress,
    Submitted,
    Updated,
    Cancelled
}
```

**Note**: Remove all hardcoded RSVP fields (Attending, BringsPlusOne, BringsKids, KidsDetails, JoinsForDinner, etc.) - these move to RsvpPerson and related models.

**Status**: ⬜ Not Started

---

#### 13. InviteCode.cs
**Changes:**
- Remove: `EventUsers` navigation property
- Add: `Invitations` navigation property

**Status**: ⬜ Not Started

---

#### 14. EventUser.cs (Deprecation)
**Decision**: This model becomes redundant with the new Invitation/InvitedPerson system.

**Options:**
1. **Keep for backward compatibility**: Add `[Obsolete]` attribute, keep existing data, create migration path
2. **Remove entirely**: Requires data migration

**Recommendation**: Keep with Obsolete attribute initially, create migration to new system, remove in future version.

**Status**: ⬜ Not Started (Pending decision)

---

#### 15. UserInvites.cs
**Changes**: Update to work with new Invitation system instead of EventUser.

**Status**: ⬜ Not Started

---

## Database Migration Plan

### Phase 1: Schema Changes
- [ ] Create new tables: EventDays, Invitations, InvitedPersons, RsvpPeople, RsvpKids, RsvpFoodPreferences, RsvpAccommodations, CustomQuestions, RsvpCustomAnswers
- [ ] Add columns to existing tables (Event.EndDate, EventConfig new fields)
- [ ] Create relationships/foreign keys

### Phase 2: Data Migration
- [ ] Migrate existing Event data (add EndDate based on Date + 1 day as default)
- [ ] Migrate existing EventConfig data (map old flags to new structure)
- [ ] Migrate existing EventUser -> Invitation/InvitedPerson
- [ ] Migrate existing Rsvp -> new Rsvp/RsvpPerson structure
- [ ] Handle dietary options migration
- [ ] Handle kids data migration

### Phase 3: Code Updates
- [ ] Update all controllers to use new models
- [ ] Update all services to use new models
- [ ] Update all views/pages to use new models
- [ ] Create new admin UI for custom questions
- [ ] Create new RSVP stepper UI

---

## UI/UX Changes

### RSVP Stepper Flow
Proposed steps:

1. **Attendance** (Step 1)
   - Group name display
   - For each invited person: Attending Yes/No
   - Plus one options (if allowed)
   - Plus one name fields

2. **Group Composition** (Step 2)
   - Add/edit group members
   - Add plus ones with names
   - Manage kids for each person

3. **Food Preferences** (Step 3 - per day)
   - For each event day that offers food:
     - For each person: which meals they'll attend
     - Dietary restrictions per person

4. **Accommodation** (Step 4 - per day)
   - For each event day that offers accommodation:
     - For each person: needs accommodation Yes/No
     - Link to accommodation booking page (if EventConfig.AccommodationLink exists)
     - Accommodation code display

5. **Custom Questions** (Step 5)
   - Dynamic form based on Event.CustomQuestions
   - Grouped by StepGroup

6. **Review & Comments** (Step 6)
   - Summary of all selections
   - Comments field
   - Submit button

---

## API/Controller Changes

### New Controllers Needed
- [ ] `InvitationsController` - CRUD for invitations
- [ ] `InvitedPersonsController` - Manage invited people within invitation
- [ ] `EventDaysController` - Manage event days
- [ ] `CustomQuestionsController` - Manage custom questions per event

### Modified Controllers
- [ ] `EventsController` - Update to use new Event structure
- [ ] `RsvpsController` - Complete rewrite for new RSVP structure

---

## Validation Rules

### Rsvp Validation
- [ ] At least one person in the group must be attending (if group RSVP)
- [ ] Primary invitee must have name
- [ ] Email required for non-registered plus ones
- [ ] Kid ages must be positive
- [ ] Food preferences only for days that offer food
- [ ] Accommodation only for days that offer accommodation
- [ ] Plus one count must not exceed EventConfig.MaxPlusOnesPerPerson
- [ ] Custom question required fields must be validated

### Event Validation
- [ ] EndDate must be >= Date (start)
- [ ] EventDays must be within Event.Date to Event.EndDate range
- [ ] Custom questions must have valid StepGroup values

---

## Backward Compatibility Considerations

### For Existing Events
- [ ] Migration script to convert old Rsvp data to new structure
- [ ] Fallback UI for old events (if needed)
- [ ] API versioning considerations

### For Existing Code
- [ ] Gradual migration: keep old models temporarily
- [ ] Dual-write during transition period
- [ ] Feature flags to enable new system

---

## Testing Plan

### Unit Tests
- [ ] Model validation tests
- [ ] Migration tests (data integrity)
- [ ] Business logic tests (RSVP calculations)
- [ ] Custom question rendering tests

### Integration Tests
- [ ] RSVP submission flow
- [ ] Group invitation flow
- [ ] Multi-day event handling
- [ ] Stepper navigation

### UI Tests
- [ ] Stepper form rendering
- [ ] Dynamic custom question display
- [ ] Mobile responsiveness
- [ ] Accessibility

---

## Deployment Plan

1. **Phase 1**: Deploy schema changes with migration scripts (downtime possible)
2. **Phase 2**: Deploy code changes with feature flag disabled
3. **Phase 3**: Run data migration in staging
4. **Phase 4**: Enable feature flag in staging, test thoroughly
5. **Phase 5**: Deploy to production with feature flag disabled
6. **Phase 6**: Run production data migration during low-traffic period
7. **Phase 7**: Enable new system, monitor closely
8. **Phase 8**: Remove old code paths after verification

---

## Files to Create/Modify

### New Files
- [ ] `Models/EventDay.cs`
- [ ] `Models/Invitation.cs`
- [ ] `Models/InvitedPerson.cs`
- [ ] `Models/RsvpPerson.cs`
- [ ] `Models/RsvpKid.cs`
- [ ] `Models/RsvpFoodPreference.cs`
- [ ] `Models/RsvpAccommodation.cs`
- [ ] `Models/CustomQuestion.cs`
- [ ] `Models/RsvpCustomAnswer.cs`
- [ ] `Migrations/[timestamp]_RedesignRsvpSystem.cs`

### Modified Files
- [ ] `Models/Event.cs`
- [ ] `Models/EventConfig.cs`
- [ ] `Models/Rsvp.cs`
- [ ] `Models/InviteCode.cs`
- [ ] `Models/EventUser.cs` (deprecate)
- [ ] `Models/UserInvites.cs`

### New Controllers
- [ ] `Controllers/InvitationsController.cs`
- [ ] `Controllers/InvitedPersonsController.cs`
- [ ] `Controllers/EventDaysController.cs`
- [ ] `Controllers/CustomQuestionsController.cs`

### Modified Controllers
- [ ] `Controllers/EventsController.cs`
- [ ] `Controllers/RsvpsController.cs`

### New Views
- [ ] RSVP stepper views (6 steps)
- [ ] Admin: Custom questions management
- [ ] Admin: Invitation management (groups)

---

## Open Questions

1. **EventUser deprecation**: Should we keep EventUser for backward compatibility or remove it entirely?
   - *Impact*: Existing code may reference EventUser
   
2. **Plus one registration**: Should plus ones without accounts be able to receive updates/reminders?
   - *Impact*: Email collection and notification system
   
3. **Kid age brackets**: Keep the existing enum (ZeroToThree, FourToEight, etc.) or make it configurable?
   - *Recommendation*: Keep enum for structured reporting, but also store age for flexibility
   
4. **Invitation codes**: Should invitation codes still be required, or can invitations be created without them?
   - *Recommendation*: Make InviteCodeId nullable on Invitation
   
5. **RSVP editing**: Should attendees be able to edit their RSVP after submission?
   - *Recommendation*: Yes, with audit trail (UpdatedAt timestamp)

---

## Success Criteria

- [ ] All existing functionality is preserved or improved
- [ ] Group invitations work correctly (couples, families, groups)
- [ ] Multi-day events with per-day food/accommodation options work
- [ ] Custom questions can be added to any event
- [ ] RSVP stepper provides good UX
- [ ] Data migration preserves all existing RSVP data
- [ ] Performance is acceptable with the new structure
- [ ] Code is maintainable and well-documented
