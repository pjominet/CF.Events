(function () {
    "use strict";

    /*const scheduleRadios = document.querySelectorAll('input[name="SendEmailsOnInvite"]');
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
    }*/

    const selectAllCheckbox = document.getElementById('selectAllInvitees');
    const inviteeCheckboxes = document.querySelectorAll('.invitee-checkbox');
    const bulkActionButtons = document.querySelectorAll('.bulk-action-btn');

    selectAllCheckbox?.addEventListener('change', function () {
        inviteeCheckboxes.forEach(cb => {
            const row = cb.closest('tr');
            if (!row || !row.classList.contains('d-none')) {
                cb.checked = this.checked;
            }
        });
        updateBulkButtons();
    });

    inviteeCheckboxes.forEach(cb => {
        cb.addEventListener('change', function () {
            const visibleCheckboxes = Array.from(inviteeCheckboxes).filter(c => !c.closest('tr')?.classList.contains('d-none'));
            if (!this.checked) {
                selectAllCheckbox.checked = false;
            } else {
                selectAllCheckbox.checked = visibleCheckboxes.length > 0 && visibleCheckboxes.every(c => c.checked);
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

    const searchInput = document.getElementById('inviteeSearchInput');
    const statusFilters = document.querySelectorAll('.invitee-status-filter');
    const rows = document.querySelectorAll('table tbody tr');

    const searchStorageKey = 'inviteeSearch-' + window.location.pathname;
    const statusStorageKey = 'inviteeStatus-' + window.location.pathname;

    let activeStatus = sessionStorage.getItem(statusStorageKey) || '';

    function applyFilters() {
        const searchTerm = searchInput ? searchInput.value.toLowerCase().trim() : '';

        rows.forEach(row => {
            const displayName = row.querySelector('td:nth-child(2)')?.textContent?.toLowerCase() || '';
            const email = row.querySelector('td:nth-child(3)')?.textContent?.toLowerCase() || '';
            const rowStatus = (row.dataset.status || row.querySelector('td:nth-child(7)')?.textContent || '').toLowerCase().trim();

            const matchesSearch = !searchTerm || displayName.includes(searchTerm) || email.includes(searchTerm);
            const matchesStatus = !activeStatus || rowStatus === activeStatus;

            if (matchesSearch && matchesStatus) {
                row.classList.remove('d-none');
            } else {
                row.classList.add('d-none');
            }
        });

        statusFilters.forEach(btn => {
            const btnStatus = (btn.dataset.status || '').toLowerCase().trim();
            if (activeStatus && btnStatus === activeStatus) {
                btn.classList.add('text-decoration-underline', 'fw-bold');
                btn.style.opacity = '1';
            } else if (activeStatus && btnStatus !== activeStatus) {
                btn.classList.remove('text-decoration-underline', 'fw-bold');
                btn.style.opacity = '0.6';
            } else {
                btn.classList.remove('text-decoration-underline', 'fw-bold');
                btn.style.opacity = '1';
            }
        });
    }

    if (searchInput) {
        searchInput.addEventListener('input', function () {
            applyFilters();
            sessionStorage.setItem(searchStorageKey, this.value);
        });

        const initialSearch = sessionStorage.getItem(searchStorageKey);
        if (initialSearch) {
            searchInput.value = initialSearch;
        }
    }

    if (statusFilters.length > 0) {
        statusFilters.forEach(btn => {
            btn.addEventListener('click', function () {
                const targetStatus = (this.dataset.status || '').toLowerCase().trim();
                if (activeStatus === targetStatus && targetStatus !== '') {
                    activeStatus = '';
                } else {
                    activeStatus = targetStatus;
                }

                if (activeStatus) {
                    sessionStorage.setItem(statusStorageKey, activeStatus);
                } else {
                    sessionStorage.removeItem(statusStorageKey);
                }
                applyFilters();
            });
        });
    }

    applyFilters();

    window.executeBulkAction = async function (actionType) {
        const selectedUserIds = Array.from(inviteeCheckboxes)
            .filter(cb => cb.checked)
            .map(cb => cb.value);

        if (selectedUserIds.length === 0) return;

        if (actionType === 'remove') {
            const confirmed = await window.customConfirm(`Remove ${selectedUserIds.length} selected invitees?`);
            if (!confirmed) return;
        }

        if (actionType === 'save-date') {
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
            form.action = form.dataset.saveDateUrl;
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

    function initAdminRsvpModal(modalContainer) {
        const form = modalContainer.querySelector('#adminRsvpForm');
        const attendingFields = modalContainer.querySelector('#admin-attending-fields');

        // Toggle attending fields
        modalContainer.querySelectorAll('input[name="NewRsvp.Attending"]').forEach(radio => {
            radio.addEventListener('change', (e) => {
                attendingFields.classList.toggle('d-none', e.target.value === 'false');
            });
        });

        // Initialize shared rsvp logic
        window?.applyDynamicTableStyles(modalContainer);
        window.rsvpShared.initParticipantManagement(modalContainer);
        window.rsvpShared.initDayCheckboxes(modalContainer);
        window.rsvpShared.initDietarySwitches(modalContainer);

        // Populate participant options if they already exist (e.g. editing)
        window.rsvpShared.updateParticipantSelections(modalContainer);

        // Re-init multi-selects
        window?.initMultiSelects(modalContainer);

        form?.addEventListener('submit', function (_) {
            // Make sure the hidden inputs for attendance are generated.
            window.rsvpShared.prepareAttendanceInputs(this, this.querySelector('#participant-attendance'));
        });
    }

    // Export Excel with loading spinner
    const exportExcelBtn = document.getElementById('exportExcelBtn');
    exportExcelBtn?.addEventListener('click', function (_) {
        window.handleFileDownloadOverlay();
    });

    // Handle Invite Validity Modal population
    const setInviteValidityModal = document.getElementById('setInviteValidityModal');
    setInviteValidityModal?.addEventListener('show.bs.modal', function (event) {
        const button = event.relatedTarget;
        const eventId = button.getAttribute('data-bs-event-id');

        const modalEventIdInput = setInviteValidityModal.querySelector('#modalEventId');
        modalEventIdInput.value = eventId;
    });

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
