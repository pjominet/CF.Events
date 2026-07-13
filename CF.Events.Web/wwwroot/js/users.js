(function () {
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
    const guestGroupContainer = document.getElementById('guestGroupContainer');
    const guestGroupInput = document.getElementById('guestGroupInput');

    if (roleGuestCheckbox && guestGroupContainer && guestGroupInput) {
        const toggleGuestGroup = () => {
            if (roleGuestCheckbox.checked) {
                guestGroupContainer.classList.remove('d-none');
                guestGroupInput.required = true;
            } else {
                guestGroupContainer.classList.add('d-none');
                guestGroupInput.required = false;
                guestGroupInput.value = '';
            }
        };

        roleGuestCheckbox.addEventListener('change', toggleGuestGroup);
        // Initial check
        toggleGuestGroup();
    }
})();
