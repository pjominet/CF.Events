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
            const codeWrapper = document.getElementById('accommodationCodeWrapper');
            if (!!codeWrapper) {
                codeWrapper.style.display = this.checked ? 'block' : 'none';
            }

            const linksWrapper = document.getElementById('bookingLinksWrapper');
            if (!!linksWrapper) {
                linksWrapper.style.display = this.checked ? 'block' : 'none';
            }
        });
    }

    // Initialize on page load
    updateScheduleInput();
})();
