(function () {
    "use strict";

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
    const inviteesTableBody = document.querySelector('table tbody');
    const bulkActionButtons = document.querySelectorAll('.bulk-action-btn');

    selectAllCheckbox?.addEventListener('change', function () {
        const inviteeCheckboxes = document.querySelectorAll('.invitee-checkbox');
        inviteeCheckboxes.forEach(cb => {
            const row = cb.closest('tr');
            if (!row || !row.classList.contains('d-none')) {
                cb.checked = this.checked;
            }
        });
        updateBulkButtons();
    });

    inviteesTableBody?.addEventListener('change', function (e) {
        if (e.target.classList.contains('invitee-checkbox')) {
            const selectAllCheckbox = document.getElementById('selectAllInvitees');
            const inviteeCheckboxes = document.querySelectorAll('.invitee-checkbox');
            const visibleCheckboxes = Array.from(inviteeCheckboxes).filter(c => !c.closest('tr')?.classList.contains('d-none'));
            if (!e.target.checked) {
                if (selectAllCheckbox) selectAllCheckbox.checked = false;
            } else {
                if (selectAllCheckbox) selectAllCheckbox.checked = visibleCheckboxes.length > 0 && visibleCheckboxes.every(c => c.checked);
            }
            updateBulkButtons();
        }
    });

    function updateBulkButtons() {
        const inviteeCheckboxes = document.querySelectorAll('.invitee-checkbox');
        const anyChecked = Array.from(inviteeCheckboxes).some(cb => cb.checked);
        bulkActionButtons.forEach(btn => {
            btn.disabled = !anyChecked;
        });
    }

    const searchInput = document.getElementById('inviteeSearchInput');
    const priorityFilterSelect = document.getElementById('priorityFilterSelect');
    const statusFilters = document.querySelectorAll('.invitee-status-filter');
    const tableBody = inviteesTableBody;

    const searchStorageKey = 'inviteeSearch-' + window.location.pathname;
    const statusStorageKey = 'inviteeStatus-' + window.location.pathname;
    const priorityStorageKey = 'inviteePriority-' + window.location.pathname;

    let activeStatus = sessionStorage.getItem(statusStorageKey) || '';
    let activePriority = sessionStorage.getItem(priorityStorageKey) || '';

    function applyFilters() {
        const searchTerm = searchInput ? searchInput.value.toLowerCase().trim() : '';
        const rows = tableBody ? tableBody.querySelectorAll('tr') : [];

        rows.forEach(row => {
            const displayNameCell = row.querySelector('td:nth-child(2)');
            const displayName = displayNameCell?.textContent?.toLowerCase() || '';
            const email = displayNameCell?.querySelector('strong')?.getAttribute('title')?.toLowerCase() || '';
            const guestGroup = row.querySelector('td:nth-child(3)')?.textContent?.toLowerCase() || '';
            const rowStatus = (row.dataset.status || row.querySelector('td:nth-child(8)')?.textContent || '').toLowerCase().trim();
            const rowPriority = (row.dataset.priority || row.querySelector('.priority-select')?.value || '').trim();

            const matchesSearch = !searchTerm ||
                displayName.includes(searchTerm) ||
                email.includes(searchTerm) ||
                guestGroup.includes(searchTerm);
            const matchesStatus = !activeStatus || rowStatus === activeStatus;
            const matchesPriority = !activePriority || rowPriority === activePriority;

            if (matchesSearch && matchesStatus && matchesPriority) {
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

    let filterDebounceTimer = null;

    function debouncedApplyFilters() {
        if (filterDebounceTimer) clearTimeout(filterDebounceTimer);
        filterDebounceTimer = setTimeout(applyFilters, 250);
    }

    if (searchInput) {
        searchInput.addEventListener('input', function () {
            debouncedApplyFilters();
            sessionStorage.setItem(searchStorageKey, this.value);
        });

        const initialSearch = sessionStorage.getItem(searchStorageKey);
        if (initialSearch) {
            searchInput.value = initialSearch;
        }
    }
    priorityFilterSelect?.addEventListener('change', function () {
        activePriority = this.value;
        if (activePriority) {
            sessionStorage.setItem(priorityStorageKey, activePriority);
        } else {
            sessionStorage.removeItem(priorityStorageKey);
        }
        applyFilters();
    });

    const initialPriority = sessionStorage.getItem(priorityStorageKey);
    if (initialPriority) {
        priorityFilterSelect.value = initialPriority;
        activePriority = initialPriority;
    }

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

    applyFilters();

    window.executeBulkAction = async function (actionType) {
        const inviteeCheckboxes = document.querySelectorAll('.invitee-checkbox');
        const selectedUserIds = Array.from(inviteeCheckboxes)
            .filter(cb => cb.checked)
            .map(cb => cb.value);

        if (selectedUserIds.length === 0) return;

        if (actionType === 'remove') {
            const confirmed = await window.customConfirm(`Remove ${selectedUserIds.length} selected invitees?`);
            if (!confirmed) return;
        }

        if (actionType === 'resend') {
            const confirmed = await window.customConfirm(`Resend invitation email to ${selectedUserIds.length} selected invitees?`, {
                confirmClass: 'btn-info'
            });
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

        const triggeredBtn = document.querySelector(`.bulk-action-btn[onclick*="'${actionType}'"]`);
        if (triggeredBtn) window.showButtonLoading(triggeredBtn);

        showLoadingOverlay();
        form.submit();
    };

    // Admin RSVP Details Modal handling
    const adminRsvpContainer = document.getElementById('_rsvpResponsesContainer');
    if (adminRsvpContainer) {
        document.addEventListener('click', async function (e) {
            // View RSVP Details
            const viewBtn = e.target.closest('button[data-admin-rsvp-user-id][data-admin-rsvp-view-only="true"]');
            if (viewBtn) {
                const userId = viewBtn.dataset.adminRsvpUserId;
                const eventId = viewBtn.dataset.adminRsvpEventId;
                if (!userId || !eventId) return;

                try {
                    window.showButtonLoading(viewBtn);
                    const response = await fetch(`/admin/events/${eventId}/rsvp-responses/${userId}`, {
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
                    window.hideButtonLoading(viewBtn);
                }
                return;
            }

            // RSVP on behalf
            const onBehalfBtn = e.target.closest('button[data-admin-rsvp-user-id]:not([data-admin-rsvp-view-only="true"])');
            if (onBehalfBtn) {
                const userId = onBehalfBtn.dataset.adminRsvpUserId;
                const eventId = onBehalfBtn.dataset.adminRsvpEventId;
                if (!userId || !eventId) return;

                try {
                    window.showButtonLoading(onBehalfBtn);
                    const response = await fetch(`/admin/events/${eventId}/admin-rsvp/${userId}`, {
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
                    window.hideButtonLoading(onBehalfBtn);
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
        window.handleFileDownloadOverlay("fileDownload", this);
    });

    // Handle Invite Validity Modal population
    const setInviteValidityModal = document.getElementById('setInviteValidityModal');
    setInviteValidityModal?.addEventListener('show.bs.modal', function (event) {
        const button = event.relatedTarget;
        const eventId = button.getAttribute('data-bs-event-id');

        const modalEventIdInput = setInviteValidityModal.querySelector('#modalEventId');
        modalEventIdInput.value = eventId;
    });

    // Handle Add Guests collapse state persistence
    const collapseAddGuests = document.getElementById('collapseAddGuests');
    if (collapseAddGuests) {
        const storageKey = 'collapseAddGuestsState-' + window.location.pathname;

        // Restore state without animation
        const savedState = sessionStorage.getItem(storageKey);
        if (savedState === 'shown') {
            collapseAddGuests.classList.add('show');
            // Update the toggle button's aria-expanded attribute
            const toggleBtn = document.querySelector(`[data-bs-target="#${collapseAddGuests.id}"]`);
            if (toggleBtn) {
                toggleBtn.classList.remove('collapsed');
            }
        }

        // Listen for changes
        collapseAddGuests.addEventListener('shown.bs.collapse', function () {
            sessionStorage.setItem(storageKey, 'shown');
        });

        collapseAddGuests.addEventListener('hidden.bs.collapse', function () {
            sessionStorage.setItem(storageKey, 'hidden');
        });
    }

    // Silent optimistic updates for Accommodation Code and Priority
    const inviteesTableContainer = document.getElementById('inviteesTableContainer');
    const eventId = inviteesTableContainer?.dataset.eventId || window.location.pathname.match(/\/events\/(\d+)/)?.[1];

    if (inviteesTableContainer && eventId) {
        const pendingUpdates = new Map();
        let debounceTimer = null;
        const BUNDLE_DELAY_MS = 1500;

        function queueUpdate(userId, changes, element) {
            if (!userId) return;

            if (debounceTimer) {
                clearTimeout(debounceTimer);
            }

            const existing = pendingUpdates.get(userId) || {userId};
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
                const response = await fetch(`/admin/events/${eventId}/update-invitees`, {
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
                            const select = document.querySelector(`.priority-select[data-user-id="${update.userId}"]`);
                            if (select) {
                                select.dataset.originalValue = update.priority.toString();
                                select.classList.remove('border-warning');
                                select.classList.add('border-success');
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

        function handleSelectChanges(e) {
            if (e.target.classList.contains('accommodation-select')) {
                queueUpdate(e.target.dataset.userId, {accommodationCode: e.target.value}, e.target);
            }

            if (e.target.classList.contains('priority-select')) {
                const priorityValue = e.target.value;
                const row = e.target.closest('tr');
                if (row) {
                    row.dataset.priority = priorityValue;
                    applyFilters();
                }
                queueUpdate(e.target.dataset.userId, {priority: priorityValue}, e.target);
            }
        }

        inviteesTableBody.addEventListener('input', function (e) {
            handleSelectChanges(e);
        });

        inviteesTableBody.addEventListener('change', function (e) {
            handleSelectChanges(e);
        });
    }
})();
