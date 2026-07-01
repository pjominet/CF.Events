(function () {
    'use strict';

    const DIETARY_OPTIONS = ['Vegetarian', 'Vegan', 'Pescetarian', 'GlutenIntolerant', 'DairyIntolerant', 'LactoseIntolerant'];
    const DIETARY_LABELS = { Vegetarian: 'Vegetarian', Vegan: 'Vegan', Pescetarian: 'Pescetarian', GlutenIntolerant: 'Gluten Intolerant', DairyIntolerant: 'Dairy Intolerant', LactoseIntolerant: 'Lactose Intolerant' };
    const KID_BRACKETS = { ZeroToThree: '0–3 years', FourToEight: '4–8 years', NineToFifteen: '9–15 years', SixteenOrOlder: '16+' };
    const TOTAL_STEPS = 6;

    let formData = null;
    let currentStep = 0;
    let plusOneCounter = 0;

    // ── Bootstrap ──────────────────────────────────────────────────────
    document.addEventListener('DOMContentLoaded', init);

    async function init() {
        try {
            const res = await fetch(`/rsvps/invitation/${INVITATION_ID}`);
            if (!res.ok) throw new Error(await res.text());
            formData = await res.json();
            seedFromExisting();
            renderHeader();
            renderStep(0);
            show('rsvp-app');
        } catch (e) {
            el('rsvp-error').textContent = 'Failed to load RSVP form. ' + e.message;
            el('rsvp-error').classList.remove('d-none');
        } finally {
            hide('rsvp-loading');
        }

        el('btn-next').addEventListener('click', nextStep);
        el('btn-prev').addEventListener('click', prevStep);
        el('btn-save-draft').addEventListener('click', () => submitRsvp(true));
        el('btn-submit').addEventListener('click', () => submitRsvp(false));
        el('btn-add-plusone').addEventListener('click', addPlusOne);

        document.querySelectorAll('.stepper-step').forEach(s =>
            s.addEventListener('click', () => {
                const target = +s.dataset.step;
                if (target < currentStep) goToStep(target);
            })
        );
    }

    // ── State helpers ──────────────────────────────────────────────────
    function getPeople() {
        const people = [];
        document.querySelectorAll('[data-person-idx]').forEach(card => {
            const idx = card.dataset.personIdx;
            people.push({
                id: card.dataset.personId ? +card.dataset.personId : null,
                invitedPersonId: card.dataset.invitedPersonId ? +card.dataset.invitedPersonId : null,
                name: val(`person-name-${idx}`),
                email: val(`person-email-${idx}`) || null,
                isPlusOne: card.dataset.isPlusOne === 'true',
                isPrimary: card.dataset.isPrimary === 'true',
                attending: checked(`person-attending-${idx}`),
                dietaryRestrictions: getCheckedValues(`dietary-${idx}`),
                otherDietaryDetails: val(`dietary-other-${idx}`) || null
            });
        });
        return people;
    }

    function getAttendingPeople() {
        return getPeople().filter(p => p.attending);
    }

    function getFoodPreferences() {
        const prefs = [];
        document.querySelectorAll('[data-food-person]').forEach(row => {
            const personIdx = row.dataset.foodPerson;
            const dayId = +row.dataset.foodDay;
            prefs.push({
                rsvpPersonId: +personIdx,
                eventDayId: dayId,
                joinsForBreakfast: checked(`food-breakfast-${personIdx}-${dayId}`),
                joinsForLunch: checked(`food-lunch-${personIdx}-${dayId}`),
                joinsForDinner: checked(`food-dinner-${personIdx}-${dayId}`),
                joinsForBrunch: checked(`food-brunch-${personIdx}-${dayId}`),
                notes: val(`food-notes-${personIdx}-${dayId}`) || null
            });
        });
        return prefs;
    }

    function getAccommodations() {
        const accs = [];
        document.querySelectorAll('[data-acc-person]').forEach(row => {
            const personIdx = row.dataset.accPerson;
            const dayId = +row.dataset.accDay;
            accs.push({
                rsvpPersonId: +personIdx,
                eventDayId: dayId,
                needsAccommodation: checked(`acc-needs-${personIdx}-${dayId}`),
                roomType: val(`acc-room-${personIdx}-${dayId}`) || null,
                specialRequests: val(`acc-requests-${personIdx}-${dayId}`) || null
            });
        });
        return accs;
    }

    function getCustomAnswers() {
        const answers = [];
        document.querySelectorAll('[data-cq-id]').forEach(wrapper => {
            const qId = +wrapper.dataset.cqId;
            const type = wrapper.dataset.cqType;
            const answer = { customQuestionId: qId };
            switch (type) {
                case 'Text': case 'TextArea': answer.textValue = val(`cq-${qId}`) || null; break;
                case 'Boolean': answer.booleanValue = checked(`cq-${qId}`); break;
                case 'Number': answer.numberValue = val(`cq-${qId}`) ? +val(`cq-${qId}`) : null; break;
                case 'Date': answer.dateValue = val(`cq-${qId}`) || null; break;
                case 'SingleChoice': answer.textValue = val(`cq-${qId}`) || null; break;
                case 'MultiChoice': answer.selectedOptions = getCheckedValues(`cq-${qId}`); break;
            }
            answers.push(answer);
        });
        return answers;
    }

    function buildRequest(isDraft) {
        return {
            invitationId: INVITATION_ID,
            people: getPeople(),
            foodPreferences: getFoodPreferences(),
            accommodations: getAccommodations(),
            customAnswers: getCustomAnswers(),
            comments: val('rsvp-comments') || null,
            isDraft: isDraft
        };
    }

    // ── Seed from existing RSVP ────────────────────────────────────────
    function seedFromExisting() {
        if (!formData.existingRsvp) return;
        const ex = formData.existingRsvp;
        // Map existing people onto invited persons
        formData._existingPeople = ex.people || [];
        formData._existingFood = ex.foodPreferences || [];
        formData._existingAcc = ex.accommodations || [];
        formData._existingAnswers = ex.customAnswers || [];
        formData._existingComments = ex.comments || '';
    }

    // ── Render helpers ─────────────────────────────────────────────────
    function renderHeader() {
        el('rsvp-event-name').textContent = formData.eventName;
        const start = new Date(formData.eventStartDate).toLocaleDateString();
        const end = new Date(formData.eventEndDate).toLocaleDateString();
        el('rsvp-event-dates').textContent = start === end ? start : `${start} – ${end}`;
    }

    function renderStep(step) {
        switch (step) {
            case 0: renderAttendance(); break;
            case 1: renderGroupDetails(); break;
            case 2: renderFoodPreferences(); break;
            case 3: renderAccommodation(); break;
            case 4: renderCustomQuestions(); break;
            case 5: renderReview(); break;
        }
    }

    // ── Step 0: Attendance ─────────────────────────────────────────────
    function renderAttendance() {
        const container = el('attendance-list');
        container.innerHTML = '';
        const existingPeople = formData._existingPeople || [];

        formData.invitedPersons.forEach((person, i) => {
            const existing = existingPeople.find(p => p.invitedPersonId === person.id);
            const attending = existing ? existing.attending : true;
            const div = document.createElement('div');
            div.className = `person-card${attending ? '' : ' not-attending'}`;
            div.dataset.personIdx = i;
            div.dataset.invitedPersonId = person.id;
            div.dataset.isPlusOne = 'false';
            div.dataset.isPrimary = String(person.isPrimary);
            if (existing?.id) div.dataset.personId = existing.id;
            div.innerHTML = `
                <div class="d-flex justify-content-between align-items-center">
                    <div>
                        <strong>${esc(person.name)}</strong>
                        ${person.isPrimary ? '<span class="badge bg-info ms-2">Primary</span>' : ''}
                        ${person.isUser ? '<span class="badge bg-success ms-2">You</span>' : ''}
                    </div>
                    <div class="form-check form-switch">
                        <input class="form-check-input" type="checkbox" id="person-attending-${i}" ${attending ? 'checked' : ''}>
                        <label class="form-check-label" for="person-attending-${i}">Attending</label>
                    </div>
                </div>
                <input type="hidden" id="person-name-${i}" value="${esc(person.name)}">
                <input type="hidden" id="person-email-${i}" value="${esc(person.email || '')}">
            `;
            container.appendChild(div);

            div.querySelector(`#person-attending-${i}`).addEventListener('change', function () {
                div.classList.toggle('not-attending', !this.checked);
            });
        });
    }

    // ── Step 1: Group Details (dietary + plus ones) ────────────────────
    function renderGroupDetails() {
        const container = el('group-details');
        container.innerHTML = '';
        const people = getPeople();
        const attending = people.filter(p => p.attending);

        if (attending.length === 0) {
            container.innerHTML = '<p class="text-muted">No one is marked as attending. Go back to update attendance.</p>';
            return;
        }

        attending.forEach((person, i) => {
            const idx = person._idx !== undefined ? person._idx : i;
            const existing = (formData._existingPeople || []).find(p =>
                (person.invitedPersonId && p.invitedPersonId === person.invitedPersonId) ||
                (person.isPlusOne && p.isPlusOne && p.name === person.name)
            );
            const div = document.createElement('div');
            div.className = 'person-card';
            div.innerHTML = `
                <h6>${esc(person.name)} ${person.isPlusOne ? '<span class="badge bg-warning text-dark">Plus One</span>' : ''}</h6>
                <div class="dietary-checks mb-2">
                    <label class="form-label d-block mb-1">Dietary Requirements</label>
                    ${DIETARY_OPTIONS.map(opt => {
                        const isChecked = existing?.dietaryRestrictions?.includes(opt) || (person.dietaryRestrictions && person.dietaryRestrictions.includes(opt));
                        return `<div class="form-check form-check-inline">
                            <input class="form-check-input" type="checkbox" name="dietary-${idx}" value="${opt}" id="diet-${idx}-${opt}" ${isChecked ? 'checked' : ''}>
                            <label class="form-check-label" for="diet-${idx}-${opt}">${DIETARY_LABELS[opt]}</label>
                        </div>`;
                    }).join('')}
                </div>
                <div class="mb-2">
                    <input type="text" class="form-control form-control-sm" id="dietary-other-${idx}" placeholder="Other dietary details..." value="${esc(existing?.otherDietaryDetails || person.otherDietaryDetails || '')}">
                </div>
            `;
            container.appendChild(div);
        });

        // Kids section
        if (formData.allowKids) {
            const kidsDiv = document.createElement('div');
            kidsDiv.className = 'mt-3';
            const existingKids = formData.existingRsvp?.kidsDetails || {};
            kidsDiv.innerHTML = `
                <h6>Kids</h6>
                ${Object.entries(KID_BRACKETS).map(([key, label]) => `
                    <div class="row mb-2 align-items-center">
                        <div class="col-7"><label class="form-label mb-0">${label}</label></div>
                        <div class="col-5"><input type="number" class="form-control form-control-sm" id="kids-${key}" min="0" value="${existingKids[key] || 0}"></div>
                    </div>
                `).join('')}
            `;
            container.appendChild(kidsDiv);
        }
    }

    // ── Step 2: Food Preferences ───────────────────────────────────────
    function renderFoodPreferences() {
        const container = el('food-preferences');
        container.innerHTML = '';
        const foodDays = formData.eventDays.filter(d => d.offersFood);

        if (foodDays.length === 0) {
            container.innerHTML = '<p class="text-muted">No food options available for this event.</p>';
            return;
        }

        const attending = getAttendingPeople();
        if (attending.length === 0) {
            container.innerHTML = '<p class="text-muted">No one is attending.</p>';
            return;
        }

        foodDays.forEach(day => {
            const dayDiv = document.createElement('div');
            dayDiv.className = 'day-section';
            dayDiv.innerHTML = `<h6>${esc(day.name)} <small class="text-muted">(${new Date(day.date).toLocaleDateString()})</small></h6>`;

            attending.forEach((person, pIdx) => {
                const existing = (formData._existingFood || []).find(f => f.eventDayId === day.id && f.rsvpPersonId === (person.id || pIdx));
                const row = document.createElement('div');
                row.className = 'mb-2 ps-2';
                row.dataset.foodPerson = person.id || pIdx;
                row.dataset.foodDay = day.id;
                row.innerHTML = `
                    <small class="fw-bold">${esc(person.name)}</small>
                    <div class="d-flex flex-wrap gap-3 mt-1">
                        <div class="form-check"><input class="form-check-input" type="checkbox" id="food-breakfast-${person.id || pIdx}-${day.id}" ${existing?.joinsForBreakfast ? 'checked' : ''}><label class="form-check-label" for="food-breakfast-${person.id || pIdx}-${day.id}">Breakfast</label></div>
                        <div class="form-check"><input class="form-check-input" type="checkbox" id="food-brunch-${person.id || pIdx}-${day.id}" ${existing?.joinsForBrunch ? 'checked' : ''}><label class="form-check-label" for="food-brunch-${person.id || pIdx}-${day.id}">Brunch</label></div>
                        <div class="form-check"><input class="form-check-input" type="checkbox" id="food-lunch-${person.id || pIdx}-${day.id}" ${existing?.joinsForLunch ? 'checked' : ''}><label class="form-check-label" for="food-lunch-${person.id || pIdx}-${day.id}">Lunch</label></div>
                        <div class="form-check"><input class="form-check-input" type="checkbox" id="food-dinner-${person.id || pIdx}-${day.id}" ${existing?.joinsForDinner ? 'checked' : ''}><label class="form-check-label" for="food-dinner-${person.id || pIdx}-${day.id}">Dinner</label></div>
                    </div>
                    <input type="text" class="form-control form-control-sm mt-1" id="food-notes-${person.id || pIdx}-${day.id}" placeholder="Special requests..." value="${esc(existing?.notes || '')}">
                `;
                dayDiv.appendChild(row);
            });

            container.appendChild(dayDiv);
        });
    }

    // ── Step 3: Accommodation ──────────────────────────────────────────
    function renderAccommodation() {
        const container = el('accommodation-options');
        container.innerHTML = '';

        if (!formData.showAccommodationOptions) {
            container.innerHTML = '<p class="text-muted">No accommodation options for this event.</p>';
            return;
        }

        const accDays = formData.eventDays.filter(d => d.offersAccommodation);
        if (accDays.length === 0) {
            container.innerHTML = '<p class="text-muted">No accommodation available.</p>';
            return;
        }

        if (formData.accommodationLink) {
            container.innerHTML += `<div class="alert alert-info mb-3">
                <i class="bi bi-info-circle"></i> Book accommodation: <a href="${esc(formData.accommodationLink)}" target="_blank">${esc(formData.accommodationLink)}</a>
                ${formData.assignedAccommodationCode ? `<br>Your code: <strong>${esc(formData.assignedAccommodationCode)}</strong>` : ''}
            </div>`;
        }
        if (formData.accommodationInfo) {
            container.innerHTML += `<div class="alert alert-secondary mb-3">${esc(formData.accommodationInfo)}</div>`;
        }

        const attending = getAttendingPeople();

        accDays.forEach(day => {
            const dayDiv = document.createElement('div');
            dayDiv.className = 'day-section';
            dayDiv.innerHTML = `<h6>${esc(day.name)} <small class="text-muted">(${new Date(day.date).toLocaleDateString()})</small></h6>`;

            attending.forEach((person, pIdx) => {
                const existing = (formData._existingAcc || []).find(a => a.eventDayId === day.id && a.rsvpPersonId === (person.id || pIdx));
                const row = document.createElement('div');
                row.className = 'mb-3 ps-2';
                row.dataset.accPerson = person.id || pIdx;
                row.dataset.accDay = day.id;
                row.innerHTML = `
                    <div class="d-flex justify-content-between align-items-center">
                        <small class="fw-bold">${esc(person.name)}</small>
                        <div class="form-check form-switch">
                            <input class="form-check-input" type="checkbox" id="acc-needs-${person.id || pIdx}-${day.id}" ${existing?.needsAccommodation ? 'checked' : ''}>
                            <label class="form-check-label" for="acc-needs-${person.id || pIdx}-${day.id}">Needs accommodation</label>
                        </div>
                    </div>
                    <div class="row mt-1 acc-details" ${existing?.needsAccommodation ? '' : 'style="display:none"'}>
                        <div class="col-6">
                            <select class="form-select form-select-sm" id="acc-room-${person.id || pIdx}-${day.id}">
                                <option value="">Room type...</option>
                                <option ${existing?.roomType === 'Single' ? 'selected' : ''}>Single</option>
                                <option ${existing?.roomType === 'Double' ? 'selected' : ''}>Double</option>
                                <option ${existing?.roomType === 'Family' ? 'selected' : ''}>Family</option>
                            </select>
                        </div>
                        <div class="col-6">
                            <input type="text" class="form-control form-control-sm" id="acc-requests-${person.id || pIdx}-${day.id}" placeholder="Special requests..." value="${esc(existing?.specialRequests || '')}">
                        </div>
                    </div>
                `;
                dayDiv.appendChild(row);

                // Toggle details visibility
                setTimeout(() => {
                    const cb = row.querySelector(`#acc-needs-${person.id || pIdx}-${day.id}`);
                    const details = row.querySelector('.acc-details');
                    if (cb && details) {
                        cb.addEventListener('change', () => details.style.display = cb.checked ? '' : 'none');
                    }
                });
            });

            container.appendChild(dayDiv);
        });
    }

    // ── Step 4: Custom Questions ───────────────────────────────────────
    function renderCustomQuestions() {
        const container = el('custom-questions');
        container.innerHTML = '';

        if (!formData.customQuestions || formData.customQuestions.length === 0) {
            container.innerHTML = '<p class="text-muted">No additional questions for this event.</p>';
            return;
        }

        formData.customQuestions.forEach(q => {
            const existing = (formData._existingAnswers || []).find(a => a.customQuestionId === q.id);
            const wrapper = document.createElement('div');
            wrapper.className = 'mb-3';
            wrapper.dataset.cqId = q.id;
            wrapper.dataset.cqType = q.type;

            let input = '';
            switch (q.type) {
                case 'Text':
                    input = `<input type="text" class="form-control" id="cq-${q.id}" value="${esc(existing?.textValue || q.previousAnswer || '')}" ${q.isRequired ? 'required' : ''}>`;
                    break;
                case 'TextArea':
                    input = `<textarea class="form-control" id="cq-${q.id}" rows="3" ${q.isRequired ? 'required' : ''}>${esc(existing?.textValue || q.previousAnswer || '')}</textarea>`;
                    break;
                case 'Boolean':
                    const boolVal = existing?.booleanValue ?? false;
                    input = `<div class="form-check form-switch"><input class="form-check-input" type="checkbox" id="cq-${q.id}" ${boolVal ? 'checked' : ''}><label class="form-check-label" for="cq-${q.id}">Yes</label></div>`;
                    break;
                case 'Number':
                    input = `<input type="number" class="form-control" id="cq-${q.id}" value="${existing?.numberValue ?? ''}" ${q.isRequired ? 'required' : ''}>`;
                    break;
                case 'Date':
                    input = `<input type="date" class="form-control" id="cq-${q.id}" value="${existing?.dateValue ? existing.dateValue.substring(0, 10) : ''}" ${q.isRequired ? 'required' : ''}>`;
                    break;
                case 'SingleChoice':
                    input = `<select class="form-select" id="cq-${q.id}" ${q.isRequired ? 'required' : ''}>
                        <option value="">Select...</option>
                        ${(q.options || []).map(o => `<option ${(existing?.textValue || q.previousAnswer) === o ? 'selected' : ''}>${esc(o)}</option>`).join('')}
                    </select>`;
                    break;
                case 'MultiChoice':
                    const selected = existing?.selectedOptions || [];
                    input = (q.options || []).map(o => `
                        <div class="form-check">
                            <input class="form-check-input" type="checkbox" name="cq-${q.id}" value="${esc(o)}" id="cq-${q.id}-${esc(o)}" ${selected.includes(o) ? 'checked' : ''}>
                            <label class="form-check-label" for="cq-${q.id}-${esc(o)}">${esc(o)}</label>
                        </div>
                    `).join('');
                    break;
            }

            wrapper.innerHTML = `
                <label class="form-label">${esc(q.label)} ${q.isRequired ? '<span class="text-danger">*</span>' : ''}</label>
                ${q.helpText ? `<small class="form-text text-muted d-block mb-1">${esc(q.helpText)}</small>` : ''}
                ${input}
            `;
            container.appendChild(wrapper);
        });
    }

    // ── Step 5: Review ─────────────────────────────────────────────────
    function renderReview() {
        const summary = el('review-summary');
        const people = getPeople();
        const attending = people.filter(p => p.attending);
        const notAttending = people.filter(p => !p.attending);

        let html = '<h6>Attendance</h6><ul>';
        attending.forEach(p => html += `<li><i class="bi bi-check-circle text-success"></i> ${esc(p.name)} ${p.isPlusOne ? '(Plus One)' : ''}</li>`);
        notAttending.forEach(p => html += `<li><i class="bi bi-x-circle text-danger"></i> ${esc(p.name)} – Not attending</li>`);
        html += '</ul>';

        // Dietary summary
        const withDietary = attending.filter(p => p.dietaryRestrictions && p.dietaryRestrictions.length > 0);
        if (withDietary.length > 0) {
            html += '<h6>Dietary Requirements</h6><ul>';
            withDietary.forEach(p => {
                const labels = p.dietaryRestrictions.map(d => DIETARY_LABELS[d] || d).join(', ');
                html += `<li>${esc(p.name)}: ${labels}${p.otherDietaryDetails ? ` (${esc(p.otherDietaryDetails)})` : ''}</li>`;
            });
            html += '</ul>';
        }

        // Food summary
        const foodPrefs = getFoodPreferences().filter(f => f.joinsForBreakfast || f.joinsForLunch || f.joinsForDinner || f.joinsForBrunch);
        if (foodPrefs.length > 0) {
            html += '<h6>Food Preferences</h6><p class="text-muted small">Meals selected for attending persons across event days.</p>';
        }

        // Accommodation summary
        const accNeeds = getAccommodations().filter(a => a.needsAccommodation);
        if (accNeeds.length > 0) {
            html += `<h6>Accommodation</h6><p class="text-muted small">${accNeeds.length} accommodation request(s).</p>`;
        }

        // Existing comments
        const existingComments = formData._existingComments || '';
        if (existingComments) {
            el('rsvp-comments').value = existingComments;
        }

        summary.innerHTML = html;
    }

    // ── Plus One ───────────────────────────────────────────────────────
    function addPlusOne() {
        plusOneCounter++;
        const container = el('group-details');
        const idx = `po-${plusOneCounter}`;
        const div = document.createElement('div');
        div.className = 'person-card';
        div.dataset.personIdx = idx;
        div.dataset.isPlusOne = 'true';
        div.dataset.isPrimary = 'false';
        div.innerHTML = `
            <div class="d-flex justify-content-between align-items-center mb-2">
                <h6 class="mb-0">Plus One <span class="badge bg-warning text-dark">#${plusOneCounter}</span></h6>
                <button class="btn btn-sm btn-outline-danger btn-remove-plusone" data-idx="${idx}"><i class="bi bi-trash"></i></button>
            </div>
            <div class="row g-2">
                <div class="col-6"><input type="text" class="form-control form-control-sm" id="person-name-${idx}" placeholder="Name" required></div>
                <div class="col-6"><input type="email" class="form-control form-control-sm" id="person-email-${idx}" placeholder="Email"></div>
            </div>
            <input type="hidden" id="person-attending-${idx}" value="true">
            <div class="dietary-checks mt-2">
                <label class="form-label d-block mb-1">Dietary Requirements</label>
                ${DIETARY_OPTIONS.map(opt => `
                    <div class="form-check form-check-inline">
                        <input class="form-check-input" type="checkbox" name="dietary-${idx}" value="${opt}" id="diet-${idx}-${opt}">
                        <label class="form-check-label" for="diet-${idx}-${opt}">${DIETARY_LABELS[opt]}</label>
                    </div>
                `).join('')}
            </div>
            <input type="text" class="form-control form-control-sm mt-1" id="dietary-other-${idx}" placeholder="Other dietary details...">
        `;
        container.appendChild(div);

        div.querySelector('.btn-remove-plusone').addEventListener('click', () => div.remove());
    }

    // ── Navigation ─────────────────────────────────────────────────────
    function nextStep() {
        if (currentStep < TOTAL_STEPS - 1) goToStep(currentStep + 1);
    }

    function prevStep() {
        if (currentStep > 0) goToStep(currentStep - 1);
    }

    function goToStep(step) {
        // Re-render target step to pick up latest data
        document.querySelectorAll('.step-panel').forEach(p => p.classList.add('d-none'));
        document.querySelector(`.step-panel[data-step="${step}"]`).classList.remove('d-none');

        // Update stepper nav
        document.querySelectorAll('.stepper-step').forEach(s => {
            const sStep = +s.dataset.step;
            s.classList.toggle('active', sStep === step);
            s.classList.toggle('completed', sStep < step);
            const badge = s.querySelector('.badge');
            if (sStep < step) badge.className = 'badge bg-success';
            else if (sStep === step) badge.className = 'badge bg-primary';
            else badge.className = 'badge bg-secondary';
        });

        // Update buttons
        el('btn-prev').disabled = step === 0;
        const isLast = step === TOTAL_STEPS - 1;
        el('btn-next').classList.toggle('d-none', isLast);
        el('btn-submit').classList.toggle('d-none', !isLast);

        currentStep = step;
        renderStep(step);
    }

    // ── Submit ──────────────────────────────────────────────────────────
    async function submitRsvp(isDraft) {
        const request = buildRequest(isDraft);
        const url = isDraft
            ? `/rsvps/invitation/${INVITATION_ID}/draft`
            : `/rsvps/invitation/${INVITATION_ID}/submit`;

        try {
            const btn = isDraft ? el('btn-save-draft') : el('btn-submit');
            btn.disabled = true;
            btn.innerHTML = '<span class="spinner-border spinner-border-sm"></span> Saving...';

            const res = await fetch(url, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(request)
            });

            const result = await res.json();

            if (!res.ok || !result.success) {
                const msg = result.errors?.join(', ') || result.message || 'Failed to save RSVP';
                alert(msg);
                return;
            }

            if (isDraft) {
                alert('Draft saved successfully!');
            } else {
                hide('rsvp-app');
                show('rsvp-success');
            }
        } catch (e) {
            alert('Error saving RSVP: ' + e.message);
        } finally {
            el('btn-save-draft').disabled = false;
            el('btn-save-draft').innerHTML = 'Save Draft';
            el('btn-submit').disabled = false;
            el('btn-submit').innerHTML = '<i class="bi bi-check-circle"></i> Submit RSVP';
        }
    }

    // ── Utilities ──────────────────────────────────────────────────────
    function el(id) { return document.getElementById(id); }
    function val(id) { const e = el(id); return e ? e.value : ''; }
    function checked(id) { const e = el(id); return e ? e.checked : false; }
    function show(id) { el(id)?.classList.remove('d-none'); }
    function hide(id) { el(id)?.classList.add('d-none'); }
    function esc(s) { if (!s) return ''; const d = document.createElement('div'); d.textContent = s; return d.innerHTML; }

    function getCheckedValues(name) {
        return Array.from(document.querySelectorAll(`input[name="${name}"]:checked`)).map(cb => cb.value);
    }
})();
