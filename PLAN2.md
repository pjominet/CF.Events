# Improvement Plan

## 🔴 High Priority

### 1. ✅ Insecure Invitation Callback — Per-User Tokens (#16, #17)
- [x] Generate a per-user, single-use, time-limited invitation token instead of reusing the shared invite code
- [x] Remove plain-text email from the callback URL; use the opaque token to look up the user
- [x] Add token expiry validation and one-time-use enforcement
- **Files:** `EventController.cs`, `InvitationService.cs`, `InvitedPerson.cs`, `InvitedPersonModelBuilder.cs`, `InviteEmailRequest.cs`, `RsvpModelBuilder.cs`, DB migration

### 2. ✅ RSVP Stepper — Move HTML Generation to Razor Partials (#30)
- [x] Create Razor partial views for each RSVP step (Attendance, GroupDetails, FoodPreferences, Accommodation, CustomQuestions, Review)
- [x] Load form data server-side in `Rsvp.cshtml.cs` and render partials inline (no AJAX needed)
- [x] Refactor `rsvp-stepper.js` from 614→367 lines — no HTML generation, only navigation/data-collection/submission
- [x] Remove hardcoded dietary options and kid brackets from JS — now server-rendered from enums (#31)
- [x] Make step count dynamic based on event config — steps conditionally rendered (#32)
- **Files:** `Rsvp.cshtml`, `Rsvp.cshtml.cs`, `rsvp-stepper.js`, `RsvpSteps/_Attendance.cshtml`, `_GroupDetails.cshtml`, `_FoodPreferences.cshtml`, `_Accommodation.cshtml`, `_CustomQuestions.cshtml`, `_Review.cshtml`

---

## 🟡 Medium Priority

### 3. ✅ SQL Efficiency — Events LoadAsync (#1)
- [x] Replace in-memory `InvitedPersons` count with server-side `GroupBy` count in `Events.cshtml.cs`
- **Files:** `Events.cshtml.cs`

### 4. ✅ SQL Efficiency — RsvpService Split Queries (#2, #3)
- [x] Add `.AsSplitQuery()` to `GetRsvpFormAsync` Include chains
- [ ] Consolidate extra food preference / accommodation queries into the initial query (deferred — lower risk)
- **Files:** `RsvpService.cs`

### 5. ✅ SQL Efficiency — RsvpService SaveRsvpAsync Double Save (#4)
- [x] Consolidate two `SaveChangesAsync()` calls into one
- **Files:** `RsvpService.cs`

### 6. ✅ SQL Efficiency — EventController Duplicate Queries (#5, #6, #7)
- [x] Simplified `ResendInvite` to single query (done during Task #1 refactor)
- [x] Removed synchronous `FirstOrDefault` invite code lookup (no longer needed with token approach)
- **Files:** `EventController.cs`

### 7. ✅ SQL Efficiency — EventInvitees Double Save (#8)
- [x] Combine removal logic into a single `SaveChangesAsync` call
- **Files:** `EventInvitees.cshtml.cs`

### 8. SQL Efficiency — Upsert Instead of Delete-Recreate (#9)
- [ ] Replace delete-and-recreate pattern for food preferences and accommodations with upsert/diff
- **Files:** `RsvpService.cs`

### 9. ✅ CSRF Protection on Form-Based Controllers (#18)
- [x] Add `[AutoValidateAntiforgeryToken]` to `EventController` and `UserController`
- [x] Add `[ValidateAntiForgeryToken]` to `AccountController.Logout`
- [ ] JSON API controllers (`CustomQuestions`, `EventDays`, `Rsvp`) deferred — requires custom header approach
- **Files:** `EventController.cs`, `UserController.cs`, `AccountController.cs`

### 10. ✅ Logout Should Be POST (#19)
- [x] Change `AccountController.Logout` from GET to POST
- [x] Update logout form in `_Layout.cshtml` to use `method="post"`
- **Files:** `AccountController.cs`, `_Layout.cshtml`

---

## 🟢 Low Priority

### 11. DRY — Extract Redirect URL Helper (#10)
- [ ] Extract `$"/admin/events/{eventId}/invitees"` to a helper method
- **Files:** `EventController.cs`

### 12. DRY — Extract Invite Code Lookup (#11)
- [ ] Centralize invite code lookup logic into a shared method
- **Files:** `EventController.cs`

### 13. DRY — Centralize Authorization Check (#12)
- [ ] Unify "is user invited to this invitation" check between `RsvpController` and `RsvpService`
- **Files:** `RsvpController.cs`, `RsvpService.cs`

### 14. DRY — Extract Invitation Image Path Logic (#13)
- [ ] Extract invitation image path building to a shared helper/service
- **Files:** `Events.cshtml.cs`, `EventController.cs`

### 15. DRY — Generic CRUD Helper for JS (#14)
- [ ] Create shared JS CRUD helper for `custom-questions.js` and `event-days.js`
- **Files:** JS files

### 16. Architecture — File Storage Service (#15)
- [ ] Extract file handling from `Events.cshtml.cs` into `IFileStorageService`
- **Files:** `Events.cshtml.cs`, new service

### 17. Architecture — Event Service Layer (#24)
- [ ] Extract event CRUD logic to `IEventService`
- **Files:** `Events.cshtml.cs`, `EventController.cs`, new service

### 18. Architecture — Move DTOs (#25, #26)
- [ ] Move `EventDayRequest` and `InputModel` to `Models/Requests`
- **Files:** Controllers, page models

### 19. Architecture — Split IRsvpService Interface (#27)
- [ ] Move `IRsvpService` to its own file
- **Files:** `RsvpService.cs`

### 20. Security — Exception Logging (#20, #21)
- [ ] Add logging to swallowed exceptions in `ResendInvite` and `DeleteInvitationImage`
- **Files:** `EventController.cs`, `Events.cshtml.cs`

### 21. Security — File Upload Validation (#22, #23)
- [ ] Add file size limit on invitation image upload
- [ ] Add file size limit on user import CSV
- **Files:** `Events.cshtml.cs`, `UserController.cs`

### 22. Data Model — Navigation Property Types (#39)
- [ ] Change `HashSet<T>` to `List<T>` for navigation properties on `Event`
- **Files:** `Event.cs` and related models

### 23. Performance — Batch Email Status Update (#40)
- [ ] Batch `ExecuteUpdateAsync` after all emails sent instead of per-email
- **Files:** `InvitationService.cs`

### 24. Performance — Projection for InviteCodes (#41)
- [ ] Use projection instead of `Include` for invite codes in `Events.cshtml.cs`
- **Files:** `Events.cshtml.cs`

### 25. Performance — Database Indexes (#42)
- [ ] Add indexes on `InvitedPerson.UserId`, `Invitation.EventId+InviteEmailSent`, `InviteCode.Code`
- **Files:** Model builders, migration

### 26. JS — Consistent Error Handling (#33, #34, #35)
- [ ] Replace `alert()` with toast notifications in `custom-questions.js` and `event-days.js`
- [ ] Replace `location.reload()` with partial HTML updates
- [ ] Create shared `fetchWithErrorHandling` wrapper
- **Files:** JS files

### 27. JS — Remove Unused rsvp.js (#36)
- [ ] Verify and remove `rsvp.js` if `rsvp-stepper.js` is the current implementation
- **Files:** `rsvp.js`

### 28. Data Model — EndDate Nullable (#37)
- [ ] Consider making `EndDate` nullable or computed for single-day events
- **Files:** `Event.cs`, migration

### 29. Data Model — Accommodation Code Duplication (#38)
- [ ] Verify `AssignedAccommodationCode` isn't duplicated between `Invitation` and `InvitedPerson`
- **Files:** Models

---

## 🔴 High Priority (New — RSVP Fixes)

### 30. ✅ Fix NullRef in SubmitRsvp/SaveRsvpDraft
- [x] Add null check on `request` parameter in `SubmitRsvp`, `SaveRsvpDraft`, and `SaveRsvp`
- **Files:** `RsvpController.cs`

### 31. ✅ (partial) Add Couple Linking via LinkedPersonId
- [x] Add nullable `LinkedPersonId` FK on `InvitedPerson` (self-referencing)
- [x] Update model builder and generate migration
- [ ] Add admin UI to link/unlink two people as a couple on the invitees page
- **Files:** `InvitedPerson.cs`, `InvitedPersonModelBuilder.cs`, `EventInvitees.cshtml`, `EventController.cs`, migration

### 32. Hide +1 Option for Linked Users; Limit to Max 1 +1
- [ ] If user has `LinkedPersonId`, hide the "+1" option in RSVP form
- [ ] After adding a +1, hide the "Add +1" button
- **Files:** `_GroupDetails.cshtml`, `rsvp-stepper.js`
- **Note:** `LinkedPersonId` is now exposed in `InvitedPersonResponse` and `_GroupDetails.cshtml` renders `data-has-linked-partner`

### 33. Fix Duplicate Person Rendering in RSVP Form
- [ ] Filter `InvitedPersons` to only show persons relevant to the current user
- **Files:** `RsvpService.cs`, RSVP partial views

### 33b. ✅ Fix FK Error on RSVP Submit
- [x] Split `SaveChangesAsync` — save people first to get IDs, then save food/accommodation/custom answers
- [x] Remove `Id = prefRequest.Id ?? 0` from new food/accommodation entities
- **Files:** `RsvpService.cs`

### 33c. ✅ Remove Draft Button, Auto-Save on Step Navigation
- [x] Remove draft button from `Rsvp.cshtml` and `rsvp-stepper.js`
- [x] Auto-save as draft on each "Next" step navigation
- **Files:** `Rsvp.cshtml`, `rsvp-stepper.js`

---

## 🟡 Medium Priority (New — RSVP Simplification)

### 34. ✅ Move Dietary Options from GroupDetails to FoodPreferences Step
- [x] Remove dietary checkboxes from `_GroupDetails.cshtml`
- [x] Add dietary dropdown + notes per person per day in `_FoodPreferences.cshtml`
- **Files:** `_GroupDetails.cshtml`, `_FoodPreferences.cshtml`

### 35. ✅ Simplify Food: Remove Meal Booleans, Just Dietary Dropdown + Notes Per Day
- [x] Remove `JoinsForBreakfast`, `JoinsForLunch`, `JoinsForDinner`, `JoinsForBrunch` from `RsvpFoodPreference`
- [x] Add `DietaryOption` enum field and keep `SpecialRequests` for special requests
- [x] Remove `DietaryRestrictions`/`OtherDietaryDetails` from `RsvpPerson`
- [x] Update `RsvpService`, model builders, DTOs, and JS
- **Files:** `RsvpFoodPreference.cs`, `RsvpPerson.cs`, `RsvpService.cs`, `_FoodPreferences.cshtml`, `rsvp-stepper.js`, migration

### 36. ✅ Simplify Accommodation: Remove Bed/Room Fields, Show Reservation Links + Codes
- [x] Simplify accommodation step to display reservation links, codes, and a simple "I've booked" checkbox
- [x] Remove `NeedsAccommodation`, `RoomType`, `SpecialRequests` from `RsvpAccommodation`, replaced with `HasBooked`
- [x] Update model builder, DTOs, `RsvpService`, and JS
- **Files:** `RsvpAccommodation.cs`, `_Accommodation.cshtml`, `RsvpService.cs`, `rsvp-stepper.js`, migration
