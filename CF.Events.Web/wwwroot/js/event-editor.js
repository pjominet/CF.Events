document.addEventListener('DOMContentLoaded', function() {
    // Donation type toggle
    const donationRadios = document.querySelectorAll('input[name="Event.DonationType"]');
    if (donationRadios.length > 0) {
        donationRadios.forEach(radio => {
            radio.addEventListener('change', function() {
                const ibanWrapper = document.getElementById('donationIbanWrapper');
                const linkWrapper = document.getElementById('donationLinkWrapper');
                if (ibanWrapper) ibanWrapper.style.display = (this.value.toLowerCase() === 'iban') ? 'block' : 'none';
                if (linkWrapper) linkWrapper.style.display = (this.value.toLowerCase() === 'link') ? 'block' : 'none';
            });
        });
    }

    // Dynamic rows helper
    function setupRemoveButtons(container) {
        if (!container) return;
        container.addEventListener('click', function(e) {
            if (e.target.classList.contains('remove-row')) {
                e.target.closest('.row, .faq-row').remove();
                reindexRows(container);
            }
        });
    }

    function reindexRows(container) {
        container.querySelectorAll('.row, .faq-row').forEach((row, index) => {
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
            const html = `
                <div class="row mb-2 schedule-row">
                    <div class="col-md-2">
                        <input name="Event.ScheduleSteps[${index}].Day" class="form-control" placeholder="Day" />
                    </div>
                    <div class="col-md-3">
                        <input name="Event.ScheduleSteps[${index}].TimeStamp" type="time" class="form-control" />
                    </div>
                    <div class="col-md-5">
                        <input name="Event.ScheduleSteps[${index}].Label" class="form-control" placeholder="Label" />
                    </div>
                    <div class="col-md-2">
                        <button type="button" class="btn btn-danger remove-row">Remove</button>
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
                <div class="mb-3 faq-row border p-2">
                    <div class="mb-2">
                        <input name="Event.FaqItems[${index}].Question" class="form-control" placeholder="Question" />
                    </div>
                    <div class="mb-2">
                        <textarea name="Event.FaqItems[${index}].Answer" class="form-control" placeholder="Answer"></textarea>
                    </div>
                    <button type="button" class="btn btn-danger btn-sm remove-row">Remove</button>
                </div>`;
            faqContainer.insertAdjacentHTML('beforeend', html);
        });
    }

    // Start/End date validation
    const startDateInput = document.getElementById('Event_StartDate');
    const endDateInput = document.getElementById('Event_EndDate');

    if (startDateInput && endDateInput) {
        const updateMinEndDate = () => {
            endDateInput.min = startDateInput.value;
            if (endDateInput.value && endDateInput.value < startDateInput.value) {
                endDateInput.value = startDateInput.value;
            }
        };
        startDateInput.addEventListener('change', updateMinEndDate);
        updateMinEndDate();
    }
});
