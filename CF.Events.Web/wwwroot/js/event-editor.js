(function() {
    "use strict";

    // Donation type toggle
    const donationCheckboxes = document.querySelectorAll('input[name="Event.DonationTypes"]');
    if (donationCheckboxes.length > 0) {
        donationCheckboxes.forEach(cb => {
            cb.addEventListener('change', function() {
                const ibanWrapper = document.getElementById('donationIbanWrapper');
                const linkWrapper = document.getElementById('donationLinkWrapper');
                const ibanCb = document.getElementById('donationTypeIban');
                const linkCb = document.getElementById('donationTypeLink');
                if (ibanWrapper) ibanWrapper.style.display = (ibanCb && ibanCb.checked) ? 'block' : 'none';
                if (linkWrapper) linkWrapper.style.display = (linkCb && linkCb.checked) ? 'block' : 'none';
            });
        });
    }

    // Dynamic rows helper
    function setupRemoveButtons(container) {
        if (!container) return;
        container.addEventListener('click', function(e) {
            const removeBtn = e.target.closest('.remove-row');
            if (removeBtn) {
                removeBtn.closest('.schedule-row, .faq-row').remove();
                reindexRows(container);
            }
        });
    }

    function reindexRows(container) {
        container.querySelectorAll('.schedule-row, .faq-row').forEach((row, index) => {
            row.querySelectorAll('input, textarea').forEach(input => {
                const name = input.getAttribute('name');
                if (name) {
                    // Update the index in the name attribute, e.g., Event.ScheduleSteps[0].Day -> Event.ScheduleSteps[1].Day
                    input.setAttribute('name', name.replace(/\[\d+\]/, '[' + index + ']'));
                }
                const id = input.getAttribute('id');
                if (id) {
                    // Update the index in the id attribute, e.g., Event_ScheduleSteps_0__Day -> Event_ScheduleSteps_1__Day
                    input.setAttribute('id', id.replace(/_\d+__/, '_' + index + '__'));
                }
            });
        });
    }

    const scheduleContainer = document.getElementById('schedule-container');
    const faqContainer = document.getElementById('faq-container');

    setupRemoveButtons(scheduleContainer);
    setupRemoveButtons(faqContainer);

    const addScheduleBtn = document.getElementById('add-schedule');
    if (addScheduleBtn) {
        addScheduleBtn.addEventListener('click', function() {
            const index = scheduleContainer.querySelectorAll('.schedule-row').length;
            const maxDays = scheduleContainer.getAttribute('data-max-days') || '';
            const html = `
                <div class="row gx-2 mb-2 schedule-row">
                    <div class="col-md-2">
                        <input name="Event.ScheduleSteps[${index}].Day" type="number" class="form-control" placeholder="Day" value="1" min="1" max="${maxDays}" step="1" />
                    </div>
                    <div class="col-md-3">
                        <input name="Event.ScheduleSteps[${index}].TimeStamp" type="time" class="form-control" />
                    </div>
                    <div class="col-md-6">
                        <input name="Event.ScheduleSteps[${index}].Label" class="form-control" placeholder="Label" />
                    </div>
                    <div class="col-md-1">
                        <button type="button" class="btn btn-link text-danger remove-row"><i class="bi bi-x-lg"></i></button>
                    </div>
                </div>`;
            scheduleContainer.insertAdjacentHTML('beforeend', html);
        });
    }

    const addFaqBtn = document.getElementById('add-faq');
    if (addFaqBtn) {
        addFaqBtn.addEventListener('click', function() {
            const index = faqContainer.querySelectorAll('.faq-row').length;
            const html = `
                <div class="row gx-2 mb-3 faq-row">
                    <div class="col-11">
                        <div class="mb-2">
                            <input name="Event.FaqItems[${index}].Question" class="form-control" placeholder="Question" />
                        </div>
                        <div class="mb-2">
                            <textarea name="Event.FaqItems[${index}].Answer" class="form-control" placeholder="Answer" rows="1"></textarea>
                        </div>
                    </div>
                    <div class="col-1">
                        <button type="button" class="btn btn-link text-danger remove-row"><i class="bi bi-x-lg"></i></button>
                    </div>
                </div>`;
            faqContainer.insertAdjacentHTML('beforeend', html);
        });
    }

    // Start/End date validation
    const startDateInput = document.getElementById('Event_StartDate');
    const endDateInput = document.getElementById('Event_EndDate');

    if (startDateInput && endDateInput) {
        const updateMaxDays = () => {
            if (startDateInput.value && endDateInput.value) {
                const start = new Date(startDateInput.value);
                const end = new Date(endDateInput.value);
                const diffTime = Math.abs(end - start);
                const diffDays = Math.ceil(diffTime / (1000 * 60 * 60 * 24)) + 1;

                if (scheduleContainer) {
                    scheduleContainer.setAttribute('data-max-days', diffDays);
                    scheduleContainer.querySelectorAll('.schedule-row input[name$=".Day"]').forEach(input => {
                        input.setAttribute('max', diffDays);
                    });
                }
            }
        };

        const updateMinEndDate = () => {
            endDateInput.min = startDateInput.value;
            if (endDateInput.value && endDateInput.value < startDateInput.value) {
                endDateInput.value = startDateInput.value;
            }
            updateMaxDays();
        };
        startDateInput.addEventListener('change', updateMinEndDate);
        endDateInput.addEventListener('change', updateMaxDays);
        updateMinEndDate();
    }
})();
