(function() {
    const sendNowOption = document.getElementById('sendNowRadio');
    const scheduleOption = document.getElementById('scheduleRadio');
    const scheduleInput = document.querySelector('input[name="ScheduledFor"]');

    function updateScheduleInput() {
        if (sendNowOption.checked) {
            scheduleInput.value = '';
            scheduleInput.disabled = true;
        } else {
            scheduleInput.disabled = false;
        }
    }

    sendNowOption.addEventListener('change', updateScheduleInput);
    scheduleOption.addEventListener('change', updateScheduleInput);

    const accommodationToggle = document.querySelector('[name="NewInvite.AllowAccommodationCode"]');
    if (accommodationToggle) {
        accommodationToggle.addEventListener('change', function() {
            const select = document.getElementById('NewInvite.SelectedAccommodationCode');
            if (!!select) {
                select.disabled = this.checked ? 'disabled' : '';
            }
        });
    }

    // Initialize on page load
    updateScheduleInput();
})();
