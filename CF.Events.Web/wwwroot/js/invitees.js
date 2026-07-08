(function() {
    const scheduleRadios = document.querySelectorAll('input[name="SendEmailsOnInvite"]');
    const scheduleOption = document.getElementById('scheduleRadio');
    const scheduleInput = document.querySelector('input[name="ScheduledFor"]');

    function updateScheduleInput() {
        if (scheduleOption.checked) {
            scheduleInput.disabled = false;
            scheduleInput.required = true;
        } else {
            scheduleInput.value = '';
            scheduleInput.disabled = true;
            scheduleInput.required = false;
        }
    }

    scheduleRadios.forEach(radio => {
        radio.addEventListener('change', updateScheduleInput);
    });

    const accommodationToggle = document.querySelector('[name="NewInvite.AllowAccommodationCode"]');
    if (accommodationToggle) {
        accommodationToggle.addEventListener('change', function() {
            const codeSelect = document.querySelector('select[name="SelectedAccommodationCode"]');
            console.log(codeSelect);
            if (!!codeSelect) {
                codeSelect.disabled = !this.checked;
                codeSelect.required = this.checked;
            }
        });
    }

    // Initialize on page load
    updateScheduleInput();
})();
