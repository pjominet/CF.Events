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

    const roleGuestCheckbox = document.getElementById('roleGuest');
    const roleUserCheckbox = document.getElementById('roleUser');
    const guestGroupContainer = document.getElementById('guestGroupContainer');
    const guestGroupInput = document.getElementById('guestGroupInput');
    const maxPeopleInput = document.getElementById('maxPeopleInput');

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
                roleUserCheckbox.checked = roles.includes('User');
                roleGuestCheckbox.checked = roles.includes('Guest');
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
                roleUserCheckbox.checked = false;
                roleGuestCheckbox.checked = true;
            }
            toggleGuestGroup();
        });
    }

    if (roleGuestCheckbox && guestGroupContainer && guestGroupInput) {
        const toggleGuestGroup = () => {
            if (roleGuestCheckbox.checked) {
                guestGroupContainer.classList.remove('d-none');
                guestGroupInput.required = true;
            } else {
                guestGroupContainer.classList.add('d-none');
                guestGroupInput.required = false;
            }
        };

        roleGuestCheckbox.addEventListener('change', toggleGuestGroup);
        // Initial check
        toggleGuestGroup();
    }
})();
