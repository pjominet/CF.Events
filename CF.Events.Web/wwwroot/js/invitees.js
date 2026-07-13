(function () {
    const scheduleRadios = document.querySelectorAll('input[name="SendEmailsOnInvite"]');
    const scheduleOption = document.getElementById('scheduleRadio');
    const scheduleInput = document.querySelector('input[name="ScheduledFor"]');

    function updateScheduleInput() {
        if (!scheduleOption || !scheduleInput) return;
        if (scheduleOption.checked) {
            scheduleInput.disabled = false;
            scheduleInput.required = true;
        } else {
            scheduleInput.value = '';
            scheduleInput.disabled = true;
            scheduleInput.required = false;
        }
    }

    if (scheduleRadios.length > 0 && scheduleOption) {
        scheduleRadios.forEach(radio => {
            radio.addEventListener('change', updateScheduleInput);
        });
    }

    const accommodationToggle = document.querySelector('[name="AllowAccommodationCode"]');
    if (accommodationToggle) {
        accommodationToggle.addEventListener('change', function () {
            const codeSelect = document.querySelector('select[name="SelectedAccommodationCode"]');
            if (!!codeSelect) {
                codeSelect.disabled = !this.checked;
                codeSelect.required = this.checked;

                if (codeSelect.tomselect) {
                    if (this.checked) {
                        codeSelect.tomselect.enable();
                    } else {
                        codeSelect.tomselect.disable();
                    }
                }
            }
        });
    }

    // Initialize on page load
    if (scheduleOption) {
        updateScheduleInput();
    }

    const selectAllCheckbox = document.getElementById('selectAllInvitees');
    const inviteeCheckboxes = document.querySelectorAll('.invitee-checkbox');
    const bulkActionButtons = document.querySelectorAll('.bulk-action-btn');

    if (selectAllCheckbox) {
        selectAllCheckbox.addEventListener('change', function () {
            inviteeCheckboxes.forEach(cb => {
                cb.checked = this.checked;
            });
            updateBulkButtons();
        });
    }

    inviteeCheckboxes.forEach(cb => {
        cb.addEventListener('change', function () {
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

    window.executeBulkAction = async function (actionType) {
        const selectedUserIds = Array.from(inviteeCheckboxes)
            .filter(cb => cb.checked)
            .map(cb => cb.value);

        if (selectedUserIds.length === 0) return;

        if (actionType === 'remove') {
            const confirmed = await window.customConfirm(`Remove ${selectedUserIds.length} selected invitees?`);
            if (!confirmed) return;
        }

        if (actionType === 'save-the-date') {
            const confirmed = await window.customConfirm(`Send Save the Date email to ${selectedUserIds.length} selected invitees?`, {
                confirmClass: 'btn-primary'
            });
            if (!confirmed) return;
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

    // Admin RSVP Details Modal handling
    const adminRsvpContainer = document.getElementById('_rsvpResponsesContainer');
    if (adminRsvpContainer) {
        document.addEventListener('click', async function (e) {
            const btn = e.target.closest('button[data-admin-rsvp-user-id]');
            if (!btn) return;

            const userId = btn.dataset.adminRsvpUserId;
            const eventId = btn.dataset.adminRsvpEventId;
            if (!userId || !eventId) return;

            try {
                btn.disabled = true;
                const response = await fetch(`/events/${eventId}/rsvp-responses/${userId}`, {
                    headers: {'X-Requested-With': 'XMLHttpRequest'}
                });

                if (response.ok) {
                    adminRsvpContainer.innerHTML = await response.text();

                    const modalEl = document.getElementById('adminRsvpModal');
                    if (modalEl) {
                        const modal = new bootstrap.Modal(modalEl);
                        modal.show();
                    }
                }
            } catch (error) {
                console.error('Error fetching admin RSVP details:', error);
            } finally {
                btn.disabled = false;
            }
        });
    }

    // Export Excel with loading spinner
    const exportExcelBtn = document.getElementById('exportExcelBtn');
    if (exportExcelBtn) {
        exportExcelBtn.addEventListener('click', function (e) {
            showLoadingOverlay();

            // Check for cookie to hide overlay
            const checkCookie = setInterval(function () {
                const cookieName = "fileDownload";
                if (document.cookie.indexOf(cookieName + "=") !== -1) {
                    // Delete the cookie
                    document.cookie = cookieName + '=; Max-Age=-99999999; Path=/;';

                    // Small delay before hiding to ensure the download started
                    setTimeout(function() {
                        const overlay = document.getElementById('globalLoadingOverlay');
                        if (overlay) overlay.classList.remove('active');
                    }, 1000);

                    clearInterval(checkCookie);
                }
            }, 500);
        });
    }

    // Handle Invite Validity Modal population
    const setInviteValidityModal = document.getElementById('setInviteValidityModal');
    if (setInviteValidityModal) {
        setInviteValidityModal.addEventListener('show.bs.modal', function (event) {
            const button = event.relatedTarget;
            const eventId = button.getAttribute('data-bs-event-id');

            const modalEventIdInput = setInviteValidityModal.querySelector('#modalEventId');
            modalEventIdInput.value = eventId;
        });
    }
})();
