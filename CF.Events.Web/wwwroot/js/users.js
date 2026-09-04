(function () {
    "use strict";

    const selectAllCheckbox = document.getElementById('selectAllUsers');
    const userCheckboxes = document.querySelectorAll('.user-checkbox:not(:disabled)');
    const bulkActionButtons = document.querySelectorAll('.bulk-action-btn');

    if (selectAllCheckbox) {
        selectAllCheckbox.addEventListener('change', function () {
            userCheckboxes.forEach(cb => {
                cb.checked = this.checked;
            });
            updateBulkButtons();
        });
    }

    userCheckboxes.forEach(cb => {
        cb.addEventListener('change', function () {
            if (!this.checked) {
                selectAllCheckbox.checked = false;
            } else {
                selectAllCheckbox.checked = Array.from(userCheckboxes).every(c => c.checked);
            }
            updateBulkButtons();
        });
    });

    document.querySelectorAll('td').forEach(td => {
        if (td.textContent.trim().toLowerCase().endsWith('@no-send.tech')) {
            td.classList.add('text-danger');
        }
    });

    const searchInput = document.getElementById('userSearchInput');
    if (searchInput) {
        const applyFilter = (searchTerm) => {
            const rows = document.querySelectorAll('table tbody tr');
            rows.forEach(row => {
                const displayName = row.querySelector('td:nth-child(2)')?.textContent?.toLowerCase() || '';
                const email = row.querySelector('td:nth-child(3)')?.textContent?.toLowerCase() || '';
                const guestGroup = row.querySelector('td:nth-child(5)')?.textContent?.toLowerCase() || '';

                if (displayName.includes(searchTerm) || email.includes(searchTerm) || guestGroup.includes(searchTerm)) {
                    row.classList.remove('d-none');
                } else {
                    row.classList.add('d-none');
                }
            });
            window?.applyDynamicTableStyles();
        };

        searchInput.addEventListener('input', function () {
            const searchTerm = this.value.toLowerCase().trim();
            applyFilter(searchTerm);

            // Save search term to sessionStorage
            const storageKey = 'userSearch-' + window.location.pathname;
            sessionStorage.setItem(storageKey, this.value);
        });

        // Re-apply filter on page load from session storage
        const storageKey = 'userSearch-' + window.location.pathname;
        const initialSearch = sessionStorage.getItem(storageKey);

        if (initialSearch) {
            searchInput.value = initialSearch;
            applyFilter(initialSearch.toLowerCase().trim());
        }
    }

    function updateBulkButtons() {
        const anyChecked = Array.from(userCheckboxes).some(cb => cb.checked);
        bulkActionButtons.forEach(btn => {
            btn.disabled = !anyChecked;
        });
    }

    window.executeBulkAction = async function (actionType) {
        const selectedUserIds = Array.from(userCheckboxes)
            .filter(cb => cb.checked)
            .map(cb => cb.value);

        if (selectedUserIds.length === 0) return;

        if (actionType === 'delete') {
            const confirmed = await window.customConfirm(`Delete ${selectedUserIds.length} selected users? This cannot be undone.`);
            if (!confirmed) return;
        }

        const form = document.getElementById('bulkActionForm');
        const userIdsInput = document.getElementById('bulkActionUserIds');

        userIdsInput.value = selectedUserIds.join(',');

        if (actionType === 'delete') {
            form.action = form.dataset.deleteUrl;
        }

        window.showLoadingOverlay();
        form.submit();
    };

    const roleRadios = document.querySelectorAll('.role-radio');
    const roleGuestRadio = document.getElementById('roleGuest');
    const roleUserRadio = document.getElementById('roleUser');
    const guestGroupContainer = document.getElementById('guestGroupContainer');
    const guestGroupInput = document.getElementById('guestGroupInput');
    const maxPeopleInput = document.getElementById('maxPeopleInput');

    const toggleGuestGroup = () => {
        if (roleGuestRadio && guestGroupContainer && guestGroupInput) {
            if (roleGuestRadio.checked) {
                guestGroupContainer.classList.remove('d-none');
                guestGroupInput.required = true;
            } else {
                guestGroupContainer.classList.add('d-none');
                guestGroupInput.required = false;
            }
        }
    };

    roleRadios.forEach(radio => {
        radio.addEventListener('change', toggleGuestGroup);
    });

    const addUserModal = document.getElementById('addUserModal');
    if (addUserModal) {
        addUserModal.addEventListener('show.bs.modal', function (event) {
            const button = event.relatedTarget;
            const userId = button.getAttribute('data-user-id');
            const form = document.getElementById('addUserForm');
            const title = document.getElementById('addUserModalTitle');
            const submitBtn = document.getElementById('addUserSubmitBtn');

            if (userId) {
                // Edit mode
                title.textContent = 'Edit User';
                submitBtn.textContent = 'Save Changes';
                form.action = '?handler=Edit';

                document.getElementById('userEditId').value = userId;
                document.getElementById('userDisplayName').value = button.getAttribute('data-user-displayname');
                document.getElementById('userEmail').value = button.getAttribute('data-user-email');
                document.getElementById('userPhone').value = button.getAttribute('data-user-phone') === 'n/a' ? '' : button.getAttribute('data-user-phone');

                const guestGroup = button.getAttribute('data-user-guestgroup');
                guestGroupInput.value = guestGroup === 'n/a' ? '' : guestGroup;

                const maxPeople = button.getAttribute('data-user-maxpeople');
                maxPeopleInput.value = maxPeople || 4;

                const roles = button.getAttribute('data-user-roles').split(',');
                if (roleUserRadio) roleUserRadio.checked = roles.includes('User');
                if (roleGuestRadio) roleGuestRadio.checked = roles.includes('Guest');
            } else {
                // Add mode
                title.textContent = 'Invite New User';
                submitBtn.textContent = 'Add';
                form.action = '?handler=Add';

                document.getElementById('userEditId').value = '';
                document.getElementById('userDisplayName').value = '';
                document.getElementById('userEmail').value = '';
                document.getElementById('userPhone').value = '';
                guestGroupInput.value = '';
                maxPeopleInput.value = 4;
                if (roleUserRadio) roleUserRadio.checked = false;
                if (roleGuestRadio) roleGuestRadio.checked = true;
            }
            toggleGuestGroup();
        });
    }

    const viewFeedbackModal = document.getElementById('viewFeedbackModal');
    if (viewFeedbackModal) {
        viewFeedbackModal.addEventListener('show.bs.modal', async function (event) {
            const button = event.relatedTarget;
            const userId = button.getAttribute('data-user-id');
            document.getElementById('feedbackUserDisplayName').textContent = button.getAttribute('data-user-displayname');

            const loading = document.getElementById('feedbackLoading');
            const content = document.getElementById('feedbackContent');
            const empty = document.getElementById('feedbackEmpty');
            const list = document.getElementById('feedbackList');

            loading.classList.remove('d-none');
            content.classList.add('d-none');
            empty.classList.add('d-none');
            list.innerHTML = '';

            try {
                const response = await fetch(`?handler=Feedback&userId=${userId}`);
                if (!response.ok) throw new Error('Failed to load feedback');

                const feedbacks = await response.json();

                loading.classList.add('d-none');

                if (feedbacks && feedbacks.length > 0) {
                    feedbacks.forEach(f => {
                        const item = document.createElement('div');
                        item.className = 'list-group-item px-0';
                        item.innerHTML = `
                            <div class="d-flex w-100 justify-content-between mb-1">
                                <small class="text-muted">${f.submittedAt}</small>
                            </div>
                            <p class="mb-1 text-wrap text-break" style="white-space: pre-wrap;">${f.text}</p>
                        `;
                        list.appendChild(item);
                    });
                    content.classList.remove('d-none');
                } else {
                    empty.classList.remove('d-none');
                }
            } catch (error) {
                console.error('Error fetching feedback:', error);
                loading.classList.add('d-none');
                empty.textContent = 'Error loading feedback.';
                empty.classList.remove('d-none');
            }
        });
    }

    // Initial check for non-modal elements if any
    toggleGuestGroup();
})();
