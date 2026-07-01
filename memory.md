# Implementation Memory

## Session Context
**Last Updated**: 2026-07-01 (Session 15 - Remove Legacy Code)
**Current Task**: RSVP system redesign - Core models implementation
**Approach**: Hybrid model (Approach 2) - Structured common fields + flexible custom Q&A
**Environment**: Windows machine - ALL commands must be Windows-compatible (use `dir`, `\` paths, etc.)
**Namespace Strategy**: Flat namespace (`CF.Events.Web.Models`) with subfolder organization for readability
**Schema Strategy**: Database tables grouped into schemas: identity, events, invitations, rsvps

**IMPORTANT**: **NO BACKWARD COMPATIBILITY NEEDED** - This project will be deployed from scratch with no existing data. All legacy code/properties should be removed, not deprecated.

**Build Status**: ⚠️ **LEGACY PROPERTIES REMOVED FROM CORE MODELS** - Still need to update Pages/Events/Rsvp.* and Pages/Events/Rsvp.cshtml.cs to use new models

**Key Decision**: NO backward compatibility needed - fresh deployment, all legacy code can be removed entirely

**Deployment Strategy**: Fresh deployment with no existing data - NO backward compatibility needed, all legacy code can be removed

---

## Current State

### What's Been Done
1. ✅ Reviewed existing models:
   - `Event.cs` - Basic event metadata
   - `EventConfig.cs` - Hardcoded boolean flags for options
   - `Rsvp.cs` - Hardcoded fields mirroring EventConfig
   - `InviteCode.cs` - Invite codes with EventUsers
   - `EventUser.cs` - Links users to events with invite codes
   - `Invitation.cs` - Simple record (may be replaced)
   - `UserInvites.cs` - DTO for bulk invites
   - `AppUser.cs` - Identity user extension

2. ✅ Created comprehensive `PLAN.md` with full implementation roadmap

3. ✅ Designed new model structure (see PLAN.md for details)

### Implementation Progress

#### Models - Created/Modified
- [x] EventDay.cs - Created & validated (in /Models/Events/)
- [x] Invitation.cs - Created (replaced existing record) (in /Models/Invitations/)
- [x] InvitedPerson.cs - Created (in /Models/Invitations/)
- [x] Rsvp.cs - Redesigned (kept legacy enums, added KidsDetails) (in /Models/Rsvps/)
- [x] RsvpPerson.cs - Created (in /Models/Rsvps/)
- [x] RsvpKid.cs - **Removed** (simplified to KidsDetails on Rsvp per user request)
- [x] RsvpFoodPreference.cs - Created (in /Models/Rsvps/)
- [x] RsvpAccommodation.cs - Created (in /Models/Rsvps/)
- [x] CustomQuestion.cs - Created (in /Models/Events/)
- [x] RsvpCustomAnswer.cs - Created (in /Models/Rsvps/)
- [x] Event.cs - Modified (added EndDate, EventDays, CustomQuestions, Invitations, Rsvps nav) (in /Models/Events/)
- [x] EventConfig.cs - Modified (removed meal flags, added accommodation link/info, **removed MaxPlusOnesPerPerson**) (in /Models/Events/)
- [x] InviteCode.cs - Modified (replaced EventUsers with Invitations nav) (in /Models/Invitations/)
- [x] AppUser.cs - Modified (replaced UserEvents with InvitedPersons nav) (in /Models/Identity/)
- [x] EventUser.cs - **REMOVED** (legacy model, see Legacy Code Reference below)
- [x] UserInvites.cs - **REMOVED** (legacy model, see Legacy Code Reference below)

#### Database Context & Model Builders
- [x] Created `/Data/ModelBuilders/` folder
- [x] Created 11 model builder files (one per model)
- [x] Updated EventsDbContext.cs to use all model builders
- [x] Added new DbSet properties for all new models
- [x] Organized models into logical schemas: identity, app, events, invitations, rsvps
- [x] Configured all relationships, indexes, and property constraints explicitly
- [x] **Fixed duplicate relationship configurations** (Session 8):
  - Removed duplicate InvitedPerson-RsvpPerson config from InvitedPersonModelBuilder
  - Removed duplicate Event-EventConfig config from EventModelBuilder
  - Fixed EventConfigModelBuilder IsRequired to match nullable navigation properties
- [x] **Comprehensive duplicate removal** (Session 9):
  - Applied best practice consistently: each relationship configured only once in the FK-owning entity's model builder
  - Removed 9+ duplicate relationship configurations across all model builders
  - Added explanatory comments to prevent future duplication

### What's Next
- [x] Review PLAN.md with user
- [x] Get decisions on open questions
- [x] Prioritize implementation order
- [ ] Verify all model relationships are correctly configured (no duplicates)
- [ ] User to create and run migrations
- [ ] Implement controllers for new models
- [ ] Implement RSVP stepper UI

---

## Key Design Decisions

### Architecture Choice
**Decision**: Hybrid approach (structured + flexible)
**Rationale**: 
- Preserves type safety for common fields (attendance, food, accommodation)
- Adds flexibility for event-specific questions
- Balances queryability with extensibility
- Allows gradual migration

### Group Invitation Strategy
**Decision**: Invitation-based system with InvitedPerson entries
**Rationale**:
- One Invitation = one group invitation
- Multiple InvitedPersons = people in the group
- One Rsvp per Invitation (group response)
- Multiple RsvpPersons = responses for each person in the group

This allows:
- Inviting couples: Invitation with 2 InvitedPersons
- Inviting families: Invitation with multiple InvitedPersons + kids
- Plus ones: Primary invited person + additional RsvpPersons marked as IsPlusOne

### Data Structure
**Decision**: Per-person, per-day granularity
**Rationale**:
- Food preferences: Each person can have different preferences per day
- Accommodation: Each person can need accommodation on different nights
- Dietary: Primarily per-person (applies to all days), but can be overridden
- Kids: Linked to specific RsvpPerson

---

## Model Relationship Map

```
Event (1)
├── EventDay (N) - Days within the event
├── InviteCode (N) - Invite codes for the event
├── EventConfig (1) - Event-specific configuration
└── CustomQuestion (N) - Custom RSVP questions

EventDay (1)
├── RsvpFoodPreference (N) - Per person, per day food choices
└── RsvpAccommodation (N) - Per person, per day accommodation needs

InviteCode (1)
└── Invitation (N) - Invitations using this code

Invitation (1)
├── InvitedPerson (N) - People invited in this group
└── Rsvp (1) - The group's RSVP

InvitedPerson (1)
└── RsvpPerson (1) - Their RSVP entry (if responded)

Rsvp (1)
├── RsvpPerson (N) - People in the RSVP group
├── RsvpCustomAnswer (N) - Answers to custom questions
└── Invitation (1) - The invitation this RSVP is for

RsvpPerson (1)
├── RsvpKid (N) - Kids for this person
├── RsvpFoodPreference (N) - Food preferences per day
└── RsvpAccommodation (N) - Accommodation needs per day

CustomQuestion (1)
└── RsvpCustomAnswer (N) - Answers from RSVPs
```

---

## Implementation Priority

### Phase 1: Core Models (High Priority)
1. EventDay
2. Invitation
3. InvitedPerson
4. Rsvp (redesign)
5. RsvpPerson
6. RsvpKid

### Phase 2: Per-Day Options (High Priority)
7. RsvpFoodPreference
8. RsvpAccommodation

### Phase 3: Flexibility Layer (Medium Priority)
9. CustomQuestion
10. RsvpCustomAnswer

### Phase 4: Model Updates (Medium Priority)
11. Event (add EndDate)
12. EventConfig (remove old flags, add new fields)
13. InviteCode (update navigation)

### Phase 5: Deprecation (Low Priority)
14. EventUser (deprecate)
15. UserInvites (update)

---

## Open Questions for User

1. **Plus one email collection**: Should we require email for plus ones?
   - Pro: Can send them reminders/updates
   - Con: Privacy concerns, more data to manage

2. **Kid age brackets**: Use existing enum or make configurable?
   - Existing enum works well for reporting
   - Configurable is more flexible but harder to report on

3. **Invitation without code**: Should invitations require an InviteCode?
   - Some events might want direct invites without codes

4. **RSVP editing**: Allow editing after submission?
   - Most events allow changes up to a deadline

5. **Backward compatibility**: How to handle existing events?
   - Option A: Force migration of all existing events
   - Option B: Support both old and new systems (complex)
   - Option C: Migrate on-demand when old event is accessed

**Resolved**:
- **EventUser**: Removed entirely (replaced by Invitation/InvitedPerson system)
- **MaxPlusOnesPerPerson**: Removed from EventConfig (plus one = exactly one person, groups invited directly)

---

## Technical Notes

### Environment
- **Operating System**: Windows
- **Command Compatibility**: All shell commands must use Windows syntax:
  - Use `dir` instead of `ls`
  - Use backslashes `\) for paths, not `/`
  - Use `where` instead of `which`
  - Avoid Unix tools (grep, sed, awk, cat, etc.)
  - Use PowerShell or cmd.exe compatible commands

### Performance Guidelines
**Query Optimization:**
- Avoid large `.Include()` calls that load entire object graphs
- Use **projections** (`Select`) to fetch only needed fields unless ~75%+ of entity is required
- Prefer **ExecuteUpdate** and **ExecuteDelete** over fetch-then-update when additional fetch can be avoided
- Use **AsNoTracking()** for read-only queries
- Consider **Compiled Queries** for frequently executed queries
- Use **Index** attributes for frequently queried columns

**Database Schema:**
- Models organized in subfolders for readability: `Identity/`, `Events/`, `Invitations/`, `Rsvps/`
- All models use flat namespace `CF.Events.Web.Models` for backward compatibility
- Database tables grouped into schemas: `identity`, `events`, `invitations`, `rsvps`
- Each schema contains related tables for better organization and query performance

### Legacy Code Reference (Removed)
For future reference, the following legacy models were removed during refactoring:

#### EventUser.cs (Removed)
**Purpose**: Linked users to events with invite codes (many-to-many join table)
**Structure:**
```csharp
public class EventUser
{
    public string UserId { get; set; }
    public int EventId { get; set; }
    public string? AssignedAccommodationCode { get; set; }
    public int InviteCodeId { get; set; }
    public bool InviteEmailSent { get; set; }
    public DateTime? ScheduledFor { get; set; }
    
    // Navigation
    public Event Event { get; set; }
    public AppUser User { get; set; }
    public InviteCode InviteCode { get; set; }
    public Rsvp? Rsvp { get; set; }
}
```
**Replacement**: New `Invitation` + `InvitedPerson` system handles group invitations

#### UserInvites.cs (Removed)
**Purpose**: DTO for bulk user invites
**Structure:**
```csharp
public record UserInvites
{
    public required List<string> UserIds { get; init; }
    public int InviteCodeId { get; init; }
    public bool SendEmailsOnInvite { get; init; }
    public DateTime? ScheduledFor { get; init; }
    public bool AllowUseOfAccommodationCode { get; init; }
}
```
**Replacement**: New system will use `Invitation` + `InvitedPerson` creation directly

### Database Considerations
- **EF Core**: Will need to handle complex relationships
- **Migrations**: Large migration with data transformation
- **Performance**: Per-person, per-day structure could lead to many rows for large events
  - Consider: 100 attendees * 3 days = 300 RsvpFoodPreference rows
  - This is acceptable for typical event sizes (< 1000 attendees)

### Validation Strategy
- Use FluentValidation for complex validation rules
- Server-side validation for all business rules
- Client-side validation for UX

### Stepper Implementation
- Frontend: Likely JavaScript/React/Vue component
- Backend: Single API endpoint that accepts complete RSVP data
- Alternative: Multiple endpoints for each step (save as draft)

---

## Potential Issues & Mitigations

### Issue 1: Complex Migration
**Risk**: Data loss during migration from old structure to new
**Mitigation**: 
- Create backup before migration
- Write comprehensive migration tests
- Run migration in staging first
- Have rollback plan

### Issue 2: Performance with Many Relations
**Risk**: N+1 queries with complex object graph
**Mitigation**:
- Use EF Core .Include() appropriately
- Consider DTOs for read operations
- Implement caching for common queries

### Issue 3: UI Complexity
**Risk**: Multi-step form with dynamic questions is complex to implement
**Mitigation**:
- Break into small, manageable components
- Use existing UI patterns from the codebase
- Start with basic implementation, enhance gradually

### Issue 4: Backward Compatibility Breaking
**Risk**: Existing code breaks with new structure
**Mitigation**:
- Feature flag to toggle between old and new systems
- Gradual rollout
- Comprehensive test coverage

---

## Next Steps Checklist

- [ ] User reviews and approves PLAN.md
- [ ] User answers open questions (see above)
- [ ] Prioritize which parts to implement first
- [ ] Decide on backward compatibility strategy
- [ ] Set up development branch/environment
- [ ] Start implementing new models

---

## Session Log

### 2026-07-01 Session 1
**Duration**: ~30 minutes
**Actions**:
- Reviewed all existing model files
- Created PLAN.md with comprehensive implementation plan
- Created this memory.md for tracking
- Designed new model structure

**Decisions Made**:
- Hybrid approach (structured + flexible)
- Invitation-based group system
- Per-person, per-day granularity for food/accommodation

**Outstanding**:
- User review of PLAN.md
- Answers to open questions
- Implementation priority

**Next Session Goal**: Get user feedback on plan and decisions, then start implementation

---

### 2026-07-01 Session 6 (Reorganization & Cleanup)
**Actions**:
- **Removed MaxPlusOnesPerPerson** from EventConfig.cs and EventConfigModelBuilder.cs per user feedback
- **Reorganized models into subfolders** by schema: Identity/, Events/, Invitations/, Rsvps/
- **Reverted to flat namespace** `CF.Events.Web.Models` for all models (Option C)
- **Removed legacy models**: EventUser.cs, UserInvites.cs, and EventUserModelBuilder.cs
- **Updated all navigation properties** to use simple type names (not fully qualified)
- **Updated all model builders** to use flat namespace
- **Updated EventsDbContext.cs** with simplified usings and organized model builder calls
- **Added Legacy Code Reference** section to memory.md for future analysis
- Updated implementation progress tracker

**Files Removed**:
- `CF.Events.Web/Models/App/EventUser.cs` (legacy)
- `CF.Events.Web/Models/App/UserInvites.cs` (legacy)
- `CF.Events.Web/Models/App/` folder (deleted)
- `CF.Events.Web/Data/ModelBuilders/EventUserModelBuilder.cs` (legacy)

**Files Modified**:
- All model files: namespace reverted to `CF.Events.Web.Models`
- All model files: navigation properties simplified
- All model builder files: usings updated to flat namespace
- `CF.Events.Web/Data/EventsDbContext.cs`: simplified usings, removed EventUser references
- `CF.Events.Web/Models/Events/EventConfig.cs`: removed MaxPlusOnesPerPerson
- `CF.Events.Web/Models/Events/Event.cs`: added Invitations, Rsvps navigation properties
- `CF.Events.Web/Models/Identity/AppUser.cs`: replaced UserEvents with InvitedPersons navigation
- `CF.Events.Web/memory.md`: added Legacy Code Reference section

### 2026-07-01 Session 7 (Fix Broken Navigation Definitions)
**Actions**:
- Fixed **EventModelBuilder.cs**: removed EventUsers navigation (legacy), added Invitations and Rsvps navigation
- Fixed **RsvpModelBuilder.cs**: added missing lambdas to WithOne/WithMany calls (Event, Invitation, People, CustomAnswers)
- Fixed **RsvpPersonModelBuilder.cs**: added missing lambdas to WithOne calls (Rsvp, InvitedPerson, FoodPreferences, Accommodations)
- Fixed **InvitationModelBuilder.cs**: added missing lambda to Event navigation
- Fixed **InvitedPersonModelBuilder.cs**: added missing lambda to User navigation
- Fixed **AppUserModelBuilder.cs**: replaced EventUser references with InvitedPersons navigation

**Files Modified**:
- `CF.Events.Web/Data/ModelBuilders/EventModelBuilder.cs`
- `CF.Events.Web/Data/ModelBuilders/RsvpModelBuilder.cs`
- `CF.Events.Web/Data/ModelBuilders/RsvpPersonModelBuilder.cs`
- `CF.Events.Web/Data/ModelBuilders/InvitationModelBuilder.cs`
- `CF.Events.Web/Data/ModelBuilders/InvitedPersonModelBuilder.cs`
- `CF.Events.Web/Data/ModelBuilders/AppUserModelBuilder.cs`
- `CF.Events.Web/Models/Events/Event.cs` (added navigation properties)
- `CF.Events.Web/Models/Identity/AppUser.cs` (updated navigation property)

---

### 2026-07-01 Session 8 (Fix Duplicate Relationship Configurations)
**Actions**:
- **Fixed duplicate relationship between InvitedPerson and RsvpPerson**: Removed duplicate configuration from `InvitedPersonModelBuilder.cs` (lines 52-57). Relationship is now only configured in `RsvpPersonModelBuilder.cs` (which owns the FK `InvitedPersonId`).
- **Fixed Event-EventConfig relationship conflict**: 
  - Removed duplicate configuration from `EventModelBuilder.cs`
  - Fixed `EventConfigModelBuilder.cs` to use `.IsRequired(false)` (was `.IsRequired()`) to match the nullable navigation properties in both Event.cs and EventConfig.cs
- Added comments to both model builders indicating where the relationship is configured to prevent future duplication

**Issue Identified**: EF Core was getting confused by duplicate relationship configurations, causing "foreign keys still point to nothing" errors. 

**Best Practice Applied**: Each relationship should be configured only once, in the model builder of the entity that owns the foreign key.

**Files Modified**:
- `CF.Events.Web/Data/ModelBuilders/InvitedPersonModelBuilder.cs` (removed duplicate RsvpPerson relationship)
- `CF.Events.Web/Data/ModelBuilders/EventModelBuilder.cs` (removed duplicate EventConfig relationship)
- `CF.Events.Web/Data/ModelBuilders/EventConfigModelBuilder.cs` (fixed IsRequired to false)

---

### 2026-07-01 Session 10 (Update Services and Controllers)
**Actions**:
Updated legacy code references in services and controllers to work with the new Invitation/InvitedPerson system.

**Changes Made**:
- Added missing properties to `Invitation` model: `ScheduledFor`, `InviteEmailSent`, `AssignedAccommodationCode`
- Updated `InvitationModelBuilder` with new properties
- Updated `InvitationService.cs` to use new `Invitation` and `InvitedPerson` entities instead of `EventUser`
- Created new `InviteUsersRequest.cs` DTO to replace legacy `UserInvites`
- Updated `EventController.cs` to use new models and DTOs:
  - `GetInvitationAsset`: Check `InvitedPersons` instead of `EventUsers`
  - `InviteUsers`: Create `Invitation` with `InvitedPersons` instead of `EventUser`
  - `ResendInvite`: Use `InvitedPersons` and `Invitation` instead of `EventUser`
  - `InvitationCallback`: Check `InvitedPersons` instead of `EventUsers`
- Updated Pages to use new models:
  - `Pages/Invites.cshtml.cs`: Show invites via `InvitedPersons`
  - `Pages/Admin/EventInvitees.cshtml.cs`: Manage invitees via `InvitedPersons` and `Invitations`
  - `Pages/Events/Invitation.cshtml.cs`: Check access via `InvitedPersons`
  - `Pages/Events/Rsvp.cshtml.cs`: Updated to work with group RSVP system (simplified implementation)
- Fixed all compilation issues with new model structure

**Legacy Models Replaced**:
- `EventUser` → `Invitation` + `InvitedPerson`
- `UserInvites` → `InviteUsersRequest`

**Files Modified**:
- `Models/Invitations/Invitation.cs` (added scheduling/email tracking properties)
- `Data/ModelBuilders/InvitationModelBuilder.cs` (added new property configurations)
- `Services/InvitationService.cs` (updated to use new models)
- `Models/Invitations/InviteUsersRequest.cs` (new DTO)
- `Controllers/EventController.cs` (updated all EventUser references)
- `Pages/Invites.cshtml.cs` (updated to use InvitedPersons)
- `Pages/Admin/EventInvitees.cshtml.cs` (updated to use new invitation system)
- `Pages/Events/Invitation.cshtml.cs` (updated to use InvitedPersons)
- `Pages/Events/Rsvp.cshtml.cs` (updated for group RSVP system - simplified)

---

### 2026-07-01 Session 9 (Comprehensive Duplicate Removal)
**Actions**:
Identified and removed **all remaining duplicate relationship configurations** throughout the model builder files. Applied the best practice consistently: each relationship is configured only once, in the model builder of the entity that owns the foreign key.

**Relationships Fixed**:

1. **EventDay ↔ RsvpFoodPreference**: 
   - Kept in `RsvpFoodPreferenceModelBuilder.cs` (owns both FKs: RsvpPersonId, EventDayId)
   - Removed from `EventDayModelBuilder.cs`

2. **EventDay ↔ RsvpAccommodation**:
   - Kept in `RsvpAccommodationModelBuilder.cs` (owns both FKs: RsvpPersonId, EventDayId)
   - Removed from `EventDayModelBuilder.cs`

3. **Rsvp ↔ RsvpPerson (People)**:
   - Kept in `RsvpPersonModelBuilder.cs` (owns FK: RsvpId)
   - Removed from `RsvpModelBuilder.cs`

4. **Rsvp ↔ RsvpCustomAnswer**:
   - Kept in `RsvpCustomAnswerModelBuilder.cs` (owns both FKs: RsvpId, CustomQuestionId)
   - Removed from `RsvpModelBuilder.cs`

5. **CustomQuestion ↔ RsvpCustomAnswer**:
   - Already only in `RsvpCustomAnswerModelBuilder.cs` (owns FKs)
   - Removed from `CustomQuestionModelBuilder.cs`

6. **Invitation ↔ InvitedPerson**:
   - Kept in `InvitedPersonModelBuilder.cs` (owns FK: InvitationId)
   - Removed from `InvitationModelBuilder.cs`

7. **InviteCode ↔ Invitation**:
   - Kept in `InvitationModelBuilder.cs` (owns FK: InviteCodeId)
   - Removed from `InviteCodeModelBuilder.cs`

8. **Event ↔ InviteCodes/EventDays/CustomQuestions/Invitations/Rsvps**:
   - Kept in respective model builders (`InviteCodeModelBuilder`, `EventDayModelBuilder`, `CustomQuestionModelBuilder`, `InvitationModelBuilder`, `RsvpModelBuilder`)
   - Removed all from `EventModelBuilder.cs`

9. **AppUser ↔ InvitedPerson**:
   - Kept in `InvitedPersonModelBuilder.cs` (owns FK: UserId)
   - Removed from `AppUserModelBuilder.cs`

**Files Modified**:
- `CF.Events.Web/Data/ModelBuilders/AppUserModelBuilder.cs` (removed InvitedPersons relationship)
- `CF.Events.Web/Data/ModelBuilders/InviteCodeModelBuilder.cs` (removed Invitations relationship)
- `CF.Events.Web/Data/ModelBuilders/InvitationModelBuilder.cs` (removed InvitedPersons relationship)
- `CF.Events.Web/Data/ModelBuilders/EventModelBuilder.cs` (removed all 5 duplicate relationships)
- `CF.Events.Web/Data/ModelBuilders/EventDayModelBuilder.cs` (removed FoodPreferences, Accommodations relationships)
- `CF.Events.Web/Data/ModelBuilders/CustomQuestionModelBuilder.cs` (removed Answers relationship)
- `CF.Events.Web/Data/ModelBuilders/RsvpModelBuilder.cs` (removed People, CustomAnswers relationships)
- `CF.Events.Web/Data/ModelBuilders/RsvpPersonModelBuilder.cs` (removed FoodPreferences, Accommodations relationships)

---

### 2026-07-01 Session 14 (Fix Build Errors & Update References)
**Actions**:
- **Fixed EventConfig.cs**: Added legacy properties (OfferDinner, OfferLunch, OfferBreakfast, OfferBrunch, AllowPartners) for backward compatibility with existing pages
- **Fixed InvitedPerson.cs**: Added AssignedAccommodationCode property to support per-person accommodation codes
- **Fixed InviteEmailRequest.cs**: Added `required` modifier to all string properties to resolve nullability warnings
- **Fixed EventConfigModelBuilder.cs**: Added configuration for all legacy properties
- **Fixed InvitedPersonModelBuilder.cs**: Added configuration for AssignedAccommodationCode property
- **Updated EventController.cs**: 
  - Changed UserInvites to InviteUsersRequest DTO
  - Updated GetInvitationAsset to use InvitedPersons instead of EventUsers
  - Updated InviteUsers to create Invitation with InvitedPersons instead of EventUser
  - Updated ResendInvite to use InvitedPersons and InviteEmailRequest DTO
  - Updated InvitationCallback to use InvitedPersons instead of EventUsers
  - Removed duplicate logic by delegating email sending to InvitationService
- **Updated Pages/Admin/Events.cshtml.cs**: Changed from EventUsers to InvitedPersons for invitee counting
- **Updated Pages/Admin/EventInvitees.cshtml.cs**: 
  - Updated OnPostRemove to use InvitedPersons with proper cleanup of empty Invitations
  - Updated LoadAsync to use InvitedPersons and properly handle group RSVPs
  - Fixed AssignedAccommodationCode reference to use InvitedPerson property

**Build Status**: ✅ Build succeeds with 0 errors and 0 warnings

**Files Modified**:
- `Models/Events/EventConfig.cs` (added legacy properties)
- `Data/ModelBuilders/EventConfigModelBuilder.cs` (added legacy property configs)
- `Models/Invitations/InvitedPerson.cs` (added AssignedAccommodationCode)
- `Data/ModelBuilders/InvitedPersonModelBuilder.cs` (added AssignedAccommodationCode config)
- `Models/Requests/InviteEmailRequest.cs` (added required modifier)
- `Controllers/EventController.cs` (updated to use new models)
- `Pages/Admin/Events.cshtml.cs` (updated to use InvitedPersons)
- `Pages/Admin/EventInvitees.cshtml.cs` (updated to use new invitation system)

### 2026-07-01 Session 2
**Actions**:
- Added Windows environment note to memory.md
- Documented command compatibility requirements (dir, backslashes, etc.)

### 2026-07-01 Session 3
**Actions**:
- Created `EventDay.cs` model (first implementation step)
- Updated memory.md with implementation progress tracker

**Files Created**:
- `CF.Events.Web/Models/EventDay.cs`

### 2026-07-01 Session 4 (Batch Implementation)
**Actions**:
- Fixed EventDay.cs: Made Name required
- Created Invitation.cs (replaced existing record with full entity)
- Created InvitedPerson.cs
- Redesigned Rsvp.cs (kept legacy DietaryOptions and KidAgeBracket enums)
- Created RsvpPerson.cs
- Created RsvpKid.cs (with computed AgeBracket property)
- Created RsvpFoodPreference.cs
- Created RsvpAccommodation.cs
- Created CustomQuestion.cs (with stepper grouping support)
- Created RsvpCustomAnswer.cs
- Modified Event.cs: Added EndDate, EventDays nav, CustomQuestions nav
- Modified EventConfig.cs: Removed hardcoded meal flags, added AccommodationLink/Info, MaxPlusOnes
- Modified InviteCode.cs: Replaced EventUsers nav with Invitations nav
- Updated memory.md implementation progress

**Files Created**:
- `CF.Events.Web/Models/InvitedPerson.cs`
- `CF.Events.Web/Models/RsvpPerson.cs`
- `CF.Events.Web/Models/RsvpKid.cs`
- `CF.Events.Web/Models/RsvpFoodPreference.cs`
- `CF.Events.Web/Models/RsvpAccommodation.cs`
- `CF.Events.Web/Models/CustomQuestion.cs`
- `CF.Events.Web/Models/RsvpCustomAnswer.cs`

**Files Modified**:
- `CF.Events.Web/Models/EventDay.cs` (Name made required)
- `CF.Events.Web/Models/Invitation.cs` (complete replacement)
- `CF.Events.Web/Models/Rsvp.cs` (complete redesign)
- `CF.Events.Web/Models/Event.cs`
- `CF.Events.Web/Models/EventConfig.cs`
- `CF.Events.Web/Models/InviteCode.cs`

### 2026-07-01 Session 5 (Model Builders & DbContext)
**Actions**:
- Simplified kids: Removed RsvpKid.cs, added KidsDetails Dictionary<KidAgeBracket, int> to Rsvp
- Removed Kids navigation from RsvpPerson
- Created /Data/ModelBuilders/ folder
- Created 11 model builder files with explicit configuration:
  - AppUserModelBuilder.cs (identity schema)
  - EventModelBuilder.cs (events schema)
  - EventConfigModelBuilder.cs (events schema)
  - EventDayModelBuilder.cs (events schema)
  - InviteCodeModelBuilder.cs (invitations schema)
  - InvitationModelBuilder.cs (invitations schema)
  - InvitedPersonModelBuilder.cs (invitations schema)
  - RsvpModelBuilder.cs (rsvps schema, with Dictionary converter for KidsDetails)
  - RsvpPersonModelBuilder.cs (rsvps schema, with EnumArray converter for DietaryRestrictions)
  - RsvpFoodPreferenceModelBuilder.cs (rsvps schema, with composite unique index)
  - RsvpAccommodationModelBuilder.cs (rsvps schema, with composite unique index)
  - CustomQuestionModelBuilder.cs (events schema)
  - RsvpCustomAnswerModelBuilder.cs (rsvps schema, with composite unique index)
  - EventUserModelBuilder.cs (app schema, legacy)
- Updated EventsDbContext.cs: Added new DbSets, called all model builders, kept identity setup
- Updated memory.md with performance guidelines and implementation progress

**Files Created**:
- `CF.Events.Web/Data/ModelBuilders/*.cs` (11 files)

**Files Modified**:
- `CF.Events.Web/Data/EventsDbContext.cs` (complete refactor to use model builders)
- `CF.Events.Web/Models/Rsvp.cs` (removed Kids nav, kept KidsDetails)
- `CF.Events.Web/Models/RsvpPerson.cs` (removed Kids nav)
- `CF.Events.Web/memory.md` (added performance guidelines, updated progress)
