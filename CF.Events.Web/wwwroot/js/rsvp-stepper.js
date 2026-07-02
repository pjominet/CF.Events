(function () {
    'use strict';

    let currentStep = 0;
    let plusOneCounter = 0;

    // ── Bootstrap ──────────────────────────────────────────────────────
    document.addEventListener('DOMContentLoaded', init);

    function init() {
        // Bind attendance toggle styling
        document.querySelectorAll('.attendance-toggle').forEach(cb => {
            cb.addEventListener('change', function () {
                this.closest('.person-card').classList.toggle('not-attending', !this.checked);
            });
        });

        // Bind accommodation toggle visibility
        document.querySelectorAll('.acc-needs-toggle').forEach(cb => {
            const details = cb.closest('.acc-person-row')?.querySelector('.acc-details');
            if (details) {
                cb.addEventListener('change', () => details.style.display = cb.checked ? '' : 'none');
            }
        });

        // Navigation buttons
        el('btn-next').addEventListener('click', nextStep);
        el('btn-prev').addEventListener('click', prevStep);
        el('btn-submit').addEventListener('click', () => submitRsvp(false));
        el('btn-add-plusone')?.addEventListener('click', addPlusOne);

        // Stepper nav click
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

        // Invited persons from attendance step
        document.querySelectorAll('#attendance-list [data-person-idx]').forEach(card => {
            const idx = card.dataset.personIdx;
            people.push({
                id: card.dataset.personId ? +card.dataset.personId : null,
                invitedPersonId: card.dataset.invitedPersonId ? +card.dataset.invitedPersonId : null,
                name: val(`person-name-${idx}`),
                email: val(`person-email-${idx}`) || null,
                isPlusOne: false,
                isPrimary: card.dataset.isPrimary === 'true',
                attending: checked(`person-attending-${idx}`)
            });
        });

        // Plus ones from group details step
        document.querySelectorAll('#group-details [data-is-plus-one="true"]').forEach(card => {
            const nameInput = card.querySelector('.plusone-name');
            const emailInput = card.querySelector('.plusone-email');

            people.push({
                id: null,
                invitedPersonId: null,
                name: nameInput?.value || '',
                email: emailInput?.value || null,
                isPlusOne: true,
                isPrimary: false,
                attending: true
            });
        });

        return people;
    }

    function getFoodPreferences() {
        const prefs = [];
        document.querySelectorAll('[data-food-person]').forEach(row => {
            const personIdx = row.dataset.foodPerson;
            const dayId = +row.dataset.foodDay;
            prefs.push({
                rsvpPersonId: +personIdx,
                eventDayId: dayId,
                dietaryOption: val(`food-dietary-${personIdx}-${dayId}`) || 'None',
                specialRequests: val(`food-requests-${personIdx}-${dayId}`) || null
            });
        });
        return prefs;
    }

    function getAccommodations() {
        const hasBooked = checked('acc-has-booked');
        return [{ hasBooked: hasBooked }];
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

    function getKidsDetails() {
        const kids = {};
        document.querySelectorAll('#kids-section input[type="number"]').forEach(input => {
            const bracket = input.id.replace('kids-', '');
            const count = parseInt(input.value) || 0;
            if (count > 0) kids[bracket] = count;
        });
        return Object.keys(kids).length > 0 ? kids : null;
    }

    function buildRequest(isDraft) {
        return {
            invitationId: INVITATION_ID,
            people: getPeople(),
            foodPreferences: getFoodPreferences(),
            accommodations: getAccommodations(),
            customAnswers: getCustomAnswers(),
            kidsDetails: getKidsDetails(),
            comments: val('rsvp-comments') || null,
            isDraft: isDraft
        };
    }

    // ── Review step rendering ─────────────────────────────────────────
    function renderReview() {
        const summary = el('review-summary');
        const people = getPeople();
        const attending = people.filter(p => p.attending);
        const notAttending = people.filter(p => !p.attending);

        let html = '<h6>Attendance</h6><ul>';
        attending.forEach(p => {
            html += `<li><i class="bi bi-check-circle text-success"></i> ${esc(p.name)} ${p.isPlusOne ? '(Plus One)' : ''}</li>`;
        });
        notAttending.forEach(p => {
            html += `<li><i class="bi bi-x-circle text-danger"></i> ${esc(p.name)} – Not attending</li>`;
        });
        html += '</ul>';

        // Food/dietary summary
        const foodPrefs = getFoodPreferences().filter(f => f.dietaryOption && f.dietaryOption !== 'None');
        if (foodPrefs.length > 0) {
            html += '<h6>Dietary Preferences</h6><p class="text-muted small">Dietary options selected per day.</p>';
        }

        // Accommodation summary
        const acc = getAccommodations();
        if (acc.length > 0 && acc[0].hasBooked) {
            html += '<h6>Accommodation</h6><p class="text-muted small">You have confirmed your booking.</p>';
        }

        summary.innerHTML = html;
    }

    // ── Plus One ───────────────────────────────────────────────────────
    function addPlusOne() {
        const template = el('plusone-template');
        if (!template) return;

        plusOneCounter++;
        const clone = template.content.cloneNode(true);
        const card = clone.querySelector('.person-card');
        const idx = `po-${plusOneCounter}`;
        card.dataset.personIdx = idx;

        // Set plus one number badge
        const badge = card.querySelector('.plusone-number');
        if (badge) badge.textContent = `#${plusOneCounter}`;

        // Set unique IDs for name/email
        const nameInput = card.querySelector('.plusone-name');
        if (nameInput) nameInput.id = `person-name-${idx}`;
        const emailInput = card.querySelector('.plusone-email');
        if (emailInput) emailInput.id = `person-email-${idx}`;

        // Bind remove button
        card.querySelector('.btn-remove-plusone')?.addEventListener('click', () => card.remove());

        el('group-details').appendChild(clone);
    }

    // ── Navigation ─────────────────────────────────────────────────────
    async function nextStep() {
        if (currentStep < TOTAL_STEPS - 1) {
            await saveDraft();
            goToStep(currentStep + 1);
        }
    }

    function prevStep() {
        if (currentStep > 0) goToStep(currentStep - 1);
    }

    function goToStep(step) {
        // Hide/show step panels
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

        // Render review summary when reaching the review step
        const panel = document.querySelector(`.step-panel[data-step="${step}"]`);
        if (panel?.dataset.stepId === 'review') {
            renderReview();
        }

        // Show/hide person cards based on attendance
        if (panel?.dataset.stepId === 'group') {
            updateGroupVisibility();
        }

        // Show/hide food rows based on attendance
        if (panel?.dataset.stepId === 'food') {
            updateFoodVisibility();
        }
    }

    function updateGroupVisibility() {
        // Get attending person IDs from attendance step
        const attendingIds = new Set();
        document.querySelectorAll('#attendance-list [data-person-idx]').forEach(card => {
            const idx = card.dataset.personIdx;
            if (checked(`person-attending-${idx}`)) {
                attendingIds.add(card.dataset.invitedPersonId);
            }
        });

        // Show/hide person cards in group details based on attendance
        document.querySelectorAll('#group-details-server .person-card').forEach(card => {
            const personId = card.dataset.invitedPersonId;
            card.style.display = attendingIds.has(personId) ? '' : 'none';
        });

        if (attendingIds.size === 0) {
            const noOne = document.createElement('p');
            noOne.className = 'text-muted no-attending-msg';
            noOne.textContent = 'No one is marked as attending. Go back to update attendance.';
            const existing = el('group-details')?.querySelector('.no-attending-msg');
            if (!existing) el('group-details')?.prepend(noOne);
        } else {
            el('group-details')?.querySelector('.no-attending-msg')?.remove();
        }
    }

    function updateFoodVisibility() {
        const attendingIds = new Set();
        document.querySelectorAll('#attendance-list [data-person-idx]').forEach(card => {
            const idx = card.dataset.personIdx;
            if (checked(`person-attending-${idx}`)) {
                attendingIds.add(card.dataset.invitedPersonId);
            }
        });

        document.querySelectorAll('.food-person-row').forEach(row => {
            const personId = row.dataset.invitedPersonId;
            row.style.display = attendingIds.has(personId) ? '' : 'none';
        });
    }

    // ── Auto-save draft on step navigation ──────────────────────────────
    async function saveDraft() {
        const request = buildRequest(true);
        try {
            const res = await fetch(`/rsvps/invitation/${INVITATION_ID}/draft`, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(request)
            });
            if (!res.ok) {
                const text = await res.text();
                console.warn('Auto-save draft returned error:', res.status, text);
            }
        } catch (e) {
            console.warn('Auto-save draft failed:', e.message);
        }
    }

    // ── Submit ──────────────────────────────────────────────────────────
    async function submitRsvp(isDraft) {
        const request = buildRequest(isDraft);
        const url = `/rsvps/invitation/${INVITATION_ID}/submit`;

        try {
            const btn = el('btn-submit');
            btn.disabled = true;
            btn.innerHTML = '<span class="spinner-border spinner-border-sm"></span> Submitting...';

            const res = await fetch(url, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(request)
            });

            let result;
            try {
                result = await res.json();
            } catch {
                alert('Failed to submit RSVP: unexpected server response');
                return;
            }

            if (!res.ok || !result.success) {
                const msg = result.errors?.join(', ') || result.message || 'Failed to submit RSVP';
                alert(msg);
                return;
            }

            hide('rsvp-app');
            show('rsvp-success');
        } catch (e) {
            alert('Error submitting RSVP: ' + e.message);
        } finally {
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
