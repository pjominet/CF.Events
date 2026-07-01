# Improvement Plan

## 🔴 High Priority

### 1. ✅ Insecure Invitation Callback — Per-User Tokens (#16, #17)
- [x] Generate a per-user, single-use, time-limited invitation token instead of reusing the shared invite code
- [x] Remove plain-text email from the callback URL; use the opaque token to look up the user
- [x] Add token expiry validation and one-time-use enforcement
- **Files:** `EventController.cs`, `InvitationService.cs`, `InvitedPerson.cs`, `InvitedPersonModelBuilder.cs`, `InviteEmailRequest.cs`, `RsvpModelBuilder.cs`, DB migration

### 2. RSVP Stepper — Move HTML Generation to Razor Partials (#30)
- [ ] Create Razor partial views for each RSVP step (personal info, food, accommodation, custom questions, summary)
- [ ] Add controller/page endpoints to serve each partial via AJAX
- [ ] Refactor `rsvp-stepper.js` to load server-rendered HTML instead of generating it in JS
- [ ] Remove hardcoded dietary options and kid brackets from JS (#31)
- [ ] Make step count dynamic based on event config (#32)
- **Files:** `rsvp-stepper.js`, new partial views, `RsvpController.cs`

---

## 🟡 Medium Priority

### 3. SQL Efficiency — Events LoadAsync (#1)
- [ ] Replace in-memory `InvitedPersons` count with server-side `GroupBy` count in `Events.cshtml.cs`
- **Files:** `Events.cshtml.cs`

### 4. SQL Efficiency — RsvpService Split Queries (#2, #3)
- [ ] Add `.AsSplitQuery()` to `GetRsvpFormAsync` Include chains or replace with projection
- [ ] Consolidate extra food preference / accommodation queries into the initial query
- **Files:** `RsvpService.cs`

### 5. SQL Efficiency — RsvpService SaveRsvpAsync Double Save (#4)
- [ ] Consolidate two `SaveChangesAsync()` calls into one
- **Files:** `RsvpService.cs`

### 6. SQL Efficiency — EventController Duplicate Queries (#5, #6, #7)
- [ ] Combine duplicate queries in `ResendInvite`
- [ ] Use `FirstOrDefaultAsync` instead of `FirstOrDefault` in `InviteUsers`
- [ ] Reuse `newInvitation` object instead of re-querying
- **Files:** `EventController.cs`

### 7. SQL Efficiency — EventInvitees Double Save (#8)
- [ ] Combine removal logic into a single `SaveChangesAsync` call
- **Files:** `EventInvitees.cshtml.cs`

### 8. SQL Efficiency — Upsert Instead of Delete-Recreate (#9)
- [ ] Replace delete-and-recreate pattern for food preferences and accommodations with upsert/diff
- **Files:** `RsvpService.cs`

### 9. CSRF Protection on API Endpoints (#18)
- [ ] Add `[ValidateAntiForgeryToken]` to `CustomQuestionsController`, `EventDaysController`, `RsvpController`
- [ ] Update JS fetch calls to include anti-forgery tokens
- **Files:** Controllers, JS files, `_Layout.cshtml`

### 10. Logout Should Be POST (#19)
- [ ] Change `AccountController.Logout` from GET to POST
- [ ] Update logout links/buttons to use a form with POST
- **Files:** `AccountController.cs`, layout/nav views

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
