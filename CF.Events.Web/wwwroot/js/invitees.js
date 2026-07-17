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
        } else if (actionType === 'save-date') {
            form.action = form.dataset.saveTheDateUrl;
        }

        showLoadingOverlay();
        form.submit();
    };

    // Admin RSVP Details Modal handling
    const adminRsvpContainer = document.getElementById('_rsvpResponsesContainer');
    if (adminRsvpContainer) {
        document.addEventListener('click', async function (e) {
            // View RSVP Details
            const viewBtn = e.target.closest('button[data-admin-rsvp-user-id]');
            if (viewBtn) {
                const userId = viewBtn.dataset.adminRsvpUserId;
                const eventId = viewBtn.dataset.adminRsvpEventId;
                if (!userId || !eventId) return;

                try {
                    viewBtn.disabled = true;
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
                    viewBtn.disabled = false;
                }
                return;
            }

            // RSVP on behalf
            const behalfBtn = e.target.closest('button[data-admin-rsvp-behalf-user-id]');
            if (behalfBtn) {
                const userId = behalfBtn.dataset.adminRsvpBehalfUserId;
                const eventId = behalfBtn.dataset.adminRsvpBehalfEventId;
                if (!userId || !eventId) return;

                try {
                    behalfBtn.disabled = true;
                    const response = await fetch(`${window.location.pathname}?handler=AdminRsvpForm&id=${eventId}&userId=${userId}`, {
                        headers: {'X-Requested-With': 'XMLHttpRequest'}
                    });

                    if (response.ok) {
                        adminRsvpContainer.innerHTML = await response.text();
                        const modalEl = document.getElementById('adminRsvpModal');
                        if (modalEl) {
                            initAdminRsvpModal(modalEl);
                            const modal = new bootstrap.Modal(modalEl);
                            modal.show();
                        }
                    }
                } catch (error) {
                    console.error('Error fetching admin RSVP form:', error);
                } finally {
                    behalfBtn.disabled = false;
                }
            }
        });
    }

    function initAdminRsvpModal(modalEl) {
        const form = modalEl.querySelector('#adminRsvpForm');
        const attendingFields = modalEl.querySelector('#admin-attending-fields');
        const participantContainer = modalEl.querySelector('#participant-container');
        const addBtn = modalEl.querySelector('#add-participant');

        // Toggle attending fields
        modalEl.querySelectorAll('input[name="NewRsvp.Attending"]').forEach(radio => {
            radio.addEventListener('change', (e) => {
                attendingFields.classList.toggle('d-none', e.target.value === 'false');
            });
        });

        // Add participant
        if (addBtn) {
            addBtn.addEventListener('click', () => {
                const index = participantContainer.querySelectorAll('.participant-row').length;
                const row = document.createElement('div');
                row.className = 'row g-2 mb-2 participant-row';
                row.innerHTML = `
                    <div class="col">
                        <input name="NewRsvp.Participants[${index}]" class="form-control participant-input" placeholder="Participant Name" required />
                    </div>
                    <div class="col-auto">
                        <button type="button" class="btn btn-outline-danger remove-participant"><i class="bi bi-x-lg"></i></button>
                    </div>
                `;
                participantContainer.appendChild(row);
                // Participants changed, update other lists
                if (window.rsvpShared) {
                    window.rsvpShared.updateParticipantSelections(modalEl);
                }
            });
        }

        if (participantContainer) {
            participantContainer.addEventListener('click', (e) => {
                if (e.target.closest('.remove-participant')) {
                    e.target.closest('.participant-row').remove();
                    if (window.rsvpShared) {
                        window.rsvpShared.updateParticipantSelections(modalEl);
                    }
                }
            });

            participantContainer.addEventListener('input', (e) => {
                if (e.target.classList.contains('participant-input')) {
                    if (window.rsvpShared) {
                        window.rsvpShared.updateParticipantSelections(modalEl);
                    }
                }
            });
        }

        // Initialize shared rsvp logic
        if (window.rsvpShared) {
            if (window.siteHelpers && window.siteHelpers.initMultiSelects) {
                window.siteHelpers.initMultiSelects(modalEl);
            }
            window.rsvpShared.initDayCheckboxes(modalEl);
            window.rsvpShared.initDietarySwitches(modalEl);

            // Populate participant options if they already exist (e.g. editing)
            window.rsvpShared.updateParticipantSelections(modalEl);

            form?.addEventListener('submit', function (e) {
                // Ensure participants are up to date for dietary/attendance?
                // Actually we need to make sure the hidden inputs for attendance are generated.
                if (window.rsvpShared.prepareAttendanceInputs) {
                    window.rsvpShared.prepareAttendanceInputs(this, this.querySelector('#participant-attendance'));
                }
            });
        }
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

    // Bulk Accommodation Code Updates tracking
    const accommodationSelects = document.querySelectorAll('.accommodation-select');
    const saveAccommodationBtn = document.getElementById('saveAccommodationBtn');

    if (accommodationSelects.length > 0 && saveAccommodationBtn) {
        const bulkAccommodationForm = document.getElementById('bulkAccommodationForm');
        const updatesInput = document.getElementById('bulkAccommodationUpdates');

        accommodationSelects.forEach(select => {
            select.addEventListener('change', function () {
                const originalValue = this.dataset.originalValue || '';
                const currentValue = this.value;

                if (currentValue !== originalValue) {
                    this.classList.add('border-info');
                } else {
                    this.classList.remove('border-info');
                }

                updateUpdatesInput();
                updateSaveButtonVisibility();
            });
        });

        if (bulkAccommodationForm) {
            bulkAccommodationForm.addEventListener('submit', function () {
                showLoadingOverlay();
            });
        }

        function updateUpdatesInput() {
            if (!updatesInput) return;

            const updates = {};
            accommodationSelects.forEach(select => {
                const originalValue = select.dataset.originalValue || '';
                if (select.value !== originalValue) {
                    const userId = select.name.match(/\[(.*?)\]/)[1];
                    updates[userId] = select.value;
                }
            });

            updatesInput.value = JSON.stringify(updates);
        }

        function updateSaveButtonVisibility() {
            const anyModified = Array.from(accommodationSelects).some(select => {
                const originalValue = select.dataset.originalValue || '';
                return select.value !== originalValue;
            });

            if (anyModified) {
                saveAccommodationBtn.classList.remove('d-none');
            } else {
                saveAccommodationBtn.classList.add('d-none');
            }
        }
    }
})();
