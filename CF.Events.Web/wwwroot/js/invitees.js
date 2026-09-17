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
            const rowStatus = (row.dataset.status || row.querySelector('td:nth-child(8)')?.textContent || '').toLowerCase().trim();

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

    // Silent optimistic updates for Accommodation Code and Priority
    const accommodationSelects = document.querySelectorAll('.accommodation-select');
    const priorityInputs = document.querySelectorAll('.priority-input');
    const inviteesTableContainer = document.getElementById('inviteesTableContainer');
    const eventId = inviteesTableContainer?.dataset.eventId || window.location.pathname.match(/\/events\/(\d+)/)?.[1];

    if ((accommodationSelects.length > 0 || priorityInputs.length > 0) && eventId) {
        const pendingUpdates = new Map();
        let debounceTimer = null;
        const BUNDLE_DELAY_MS = 2000;

        function queueUpdate(userId, changes, element) {
            if (!userId) return;

            if (debounceTimer) {
                clearTimeout(debounceTimer);
            }

            const existing = pendingUpdates.get(userId) || { userId };
            if (changes.accommodationCode !== undefined) {
                existing.accommodationCode = changes.accommodationCode;
            }
            if (changes.priority !== undefined) {
                existing.priority = changes.priority;
            }
            pendingUpdates.set(userId, existing);

            if (element) {
                const originalValue = element.dataset.originalValue || '';
                const currentValue = String(element.value);
                if (currentValue !== originalValue) {
                    element.classList.add('border-warning');
                } else {
                    element.classList.remove('border-warning');
                }
            }

            if (pendingUpdates.size > 0) {
                debounceTimer = setTimeout(flushUpdates, BUNDLE_DELAY_MS);
            }
        }

        async function flushUpdates() {
            if (pendingUpdates.size === 0) return;

            const updatesToSend = Array.from(pendingUpdates.values());
            pendingUpdates.clear();

            try {
                const response = await fetch(`/events/${eventId}/update-invitees`, {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json',
                        'X-Requested-With': 'XMLHttpRequest'
                    },
                    body: JSON.stringify(updatesToSend)
                });

                if (response.ok) {
                    const result = await response.json();
                    updatesToSend.forEach(update => {
                        if (update.accommodationCode !== undefined) {
                            const select = document.querySelector(`.accommodation-select[data-user-id="${update.userId}"]`);
                            if (select) {
                                select.dataset.originalValue = update.accommodationCode;
                                select.classList.remove('border-warning');
                                select.classList.add('border-success');
                            }
                        }
                        if (update.priority !== undefined) {
                            const input = document.querySelector(`.priority-input[data-user-id="${update.userId}"]`);
                            if (input) {
                                input.dataset.originalValue = update.priority.toString();
                                input.classList.remove('border-warning');
                                input.classList.add('border-success');
                            }
                        }
                    });

                    if (typeof toastr !== 'undefined' && result && result.count > 0) {
                        let message = `Successfully updated ${result.count} invitee`;
                        if (result.count > 1) {
                            message += 's';
                        }
                        toastr.success(message);
                    }
                } else {
                    console.error('Failed to update invitees:', response.statusText);
                    if (typeof toastr !== 'undefined') {
                        toastr.error('Failed to update invitees');
                    }
                }
            } catch (error) {
                console.error('Error during silent update of invitees:', error);
                if (typeof toastr !== 'undefined') {
                    toastr.error('An error occurred while updating invitees');
                }
            }
        }

        accommodationSelects.forEach(select => {
            function handleAccommodationChange() {
                const userId = this.dataset.userId;
                queueUpdate(userId, { accommodationCode: this.value }, this);
            }

            select.addEventListener('input', handleAccommodationChange);
            select.addEventListener('change', handleAccommodationChange);
        });

        priorityInputs.forEach(input => {
            function handlePriorityChange() {
                const userId = this.dataset.userId;
                const priority = parseInt(this.value, 10);
                if (!isNaN(priority) && priority >= 1 && priority <= 3) {
                    queueUpdate(userId, { priority: priority }, this);
                }
            }

            input.addEventListener('input', handlePriorityChange);
            input.addEventListener('change', handlePriorityChange);
        });
    }
})();
