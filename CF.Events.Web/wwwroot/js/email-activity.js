document.addEventListener('DOMContentLoaded', function () {
    // Initialize tooltips
    const tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
    tooltipTriggerList.forEach(function (tooltipTriggerEl) {
        new bootstrap.Tooltip(tooltipTriggerEl);
    });

    const syncBtn = document.getElementById('syncActivityBtn');
    const syncForm = document.getElementById('syncActivityForm');
    const syncBtnText = document.getElementById('syncBtnText');
    const syncSpinner = document.getElementById('syncSpinner');

    // Manual Sync action
    syncBtn?.addEventListener('click', async function () {
        const syncUrl = syncBtn.getAttribute('data-sync-url');
        if (!syncUrl) return;

        const token = syncForm.querySelector('input[name="__RequestVerificationToken"]').value;

        syncBtn.disabled = true;
        syncSpinner.classList.remove('d-none');
        syncBtnText.textContent = 'Syncing...';

        try {
            const response = await fetch(syncUrl, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': token
                }
            });

            if (response.ok) {
                // Reload page after a brief moment to show fresh data
                setTimeout(() => window.location.reload(), 1000);
            }
        } catch (err) {
            console.error('Error syncing email activity:', err);
        } finally {
            syncBtn.disabled = false;
            syncSpinner.classList.add('d-none');
            syncBtnText.textContent = 'Sync from SMTP2GO';
        }
    });

    // Timeline Modal logic
    const timelineModalEl = document.getElementById('emailTimelineModal');
    let timelineModal = null;
    if (timelineModalEl) {
        timelineModal = new bootstrap.Modal(timelineModalEl);
    }

    const timelineLoading = document.getElementById('timelineLoading');
    const timelineContainer = document.getElementById('timelineContainer');
    const timelineEmailId = document.getElementById('timelineEmailId');

    document.querySelectorAll('.view-timeline-btn').forEach(button => {
        button.addEventListener('click', async function () {
            const timelineUrl = this.getAttribute('data-timeline-url');
            const emailId = this.getAttribute('data-email-id');

            if (!timelineModal || !timelineUrl) return;

            // Reset modal state
            timelineEmailId.textContent = emailId ? `(ID: ${emailId})` : '';
            timelineLoading.classList.remove('d-none');
            timelineContainer.innerHTML = '';

            timelineModal.show();

            try {
                const response = await fetch(timelineUrl);
                if (!response.ok) {
                    throw new Error('Failed to load timeline data');
                }

                const html = await response.text();
                timelineLoading.classList.add('d-none');
                timelineContainer.innerHTML = html;
            } catch (error) {
                console.error('Error loading timeline:', error);
                timelineLoading.classList.add('d-none');
                timelineContainer.innerHTML = '<div class="text-center py-4 text-danger">Failed to load timeline events for this email.</div>';
            }
        });
    });

    // Auto-search logic with debounce to allow for longer typing without search trigger
    const searchInput = document.getElementById('emailSearchInput');
    if (searchInput) {
        let debounceTimer;
        searchInput.addEventListener('input', function () {
            clearTimeout(debounceTimer);
            debounceTimer = setTimeout(() => {
                searchInput.form.submit();
            }, 500);
        });

        // Focus search input at the end of the text when page loads if there's a search term
        if (searchInput.value) {
            searchInput.focus();
            const val = searchInput.value;
            searchInput.value = '';
            searchInput.value = val;
        }
    }
});
