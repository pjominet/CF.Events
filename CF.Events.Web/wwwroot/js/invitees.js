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

    const selectAllCheckbox = document.getElementById('selectAllInvitees');
    const inviteeCheckboxes = document.querySelectorAll('.invitee-checkbox');
    const bulkActionButtons = document.querySelectorAll('.bulk-action-btn');

    if (selectAllCheckbox) {
        selectAllCheckbox.addEventListener('change', function() {
            inviteeCheckboxes.forEach(cb => {
                cb.checked = this.checked;
            });
            updateBulkButtons();
        });
    }

    inviteeCheckboxes.forEach(cb => {
        cb.addEventListener('change', function() {
            if (!this.checked) {
                selectAllCheckbox.checked = false;
            } else {
                selectAllCheckbox.checked = Array.from(inviteeCheckboxes).every(c => c.checked);
            }
            updateBulkButtons();
        });
    });

    function updateBulkButtons() {
        const anyChecked = Array.from(inviteeCheckboxes).some(cb => cb.checked);
        bulkActionButtons.forEach(btn => {
            btn.disabled = !anyChecked;
        });
    }

    window.executeBulkAction = function(actionType) {
        const selectedUserIds = Array.from(inviteeCheckboxes)
            .filter(cb => cb.checked)
            .map(cb => cb.value);

        if (selectedUserIds.length === 0) return;

        if (actionType === 'remove' && !confirm(`Remove ${selectedUserIds.length} selected invitees?`)) {
            return;
        }

        if (actionType === 'save-the-date' && !confirm(`Send Save the Date email to ${selectedUserIds.length} selected invitees?`)) {
            return;
        }

        const form = document.getElementById('bulkActionForm');
        const userIdsInput = document.getElementById('bulkActionUserIds');
        const actionInput = document.getElementById('bulkActionType');

        userIdsInput.value = selectedUserIds.join(',');
        actionInput.value = actionType;

        if (actionType === 'resend') {
            form.action = form.dataset.resendUrl;
        } else if (actionType === 'remove') {
            form.action = form.dataset.removeUrl;
        } else if (actionType === 'save-the-date') {
            form.action = form.dataset.saveTheDateUrl;
        }

        showLoadingOverlay();
        form.submit();
    };
})();
