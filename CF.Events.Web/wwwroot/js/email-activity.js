document.addEventListener('DOMContentLoaded', function () {
    // Initialize tooltips
    const tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
    tooltipTriggerList.forEach(function (tooltipTriggerEl) {
        new bootstrap.Tooltip(tooltipTriggerEl);
    });

    const syncBtn = document.getElementById('syncActivityBtn');
    const syncBtnText = document.getElementById('syncBtnText');
    const syncSpinner = document.getElementById('syncSpinner');
    const alertContainer = document.getElementById('syncAlertContainer');
    const antiForgeryForm = document.getElementById('antiForgeryForm');

    // Manual Sync action
    if (syncBtn) {
        syncBtn.addEventListener('click', async function () {
            const syncUrl = syncBtn.getAttribute('data-sync-url');
            if (!syncUrl) return;

            syncBtn.disabled = true;
            syncSpinner.classList.remove('d-none');
            syncBtnText.textContent = 'Syncing...';
            alertContainer.innerHTML = '';

            try {
                const tokenInput = antiForgeryForm ? antiForgeryForm.querySelector('input[name="__RequestVerificationToken"]') : null;
                const token = tokenInput ? tokenInput.value : '';

                const response = await fetch(syncUrl, {
                    method: 'POST',
                    headers: {
                        'RequestVerificationToken': token,
                        'Content-Type': 'application/json'
                    }
                });

                const result = await response.json();

                if (response.ok && result.success) {
                    alertContainer.innerHTML = `
                        <div class="alert alert-success alert-dismissible fade show" role="alert">
                            <i class="bi bi-check-circle-fill me-2"></i> ${result.message || 'Email activity synced successfully!'}
                            <button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>
                        </div>
                    `;
                    // Reload page after a brief moment to show fresh data
                    setTimeout(() => window.location.reload(), 1500);
                } else {
                    alertContainer.innerHTML = `
                        <div class="alert alert-danger alert-dismissible fade show" role="alert">
                            <i class="bi bi-exclamation-triangle-fill me-2"></i> ${result.message || 'Failed to sync email activity.'}
                            <button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>
                        </div>
                    `;
                }
            } catch (err) {
                console.error('Error syncing email activity:', err);
                alertContainer.innerHTML = `
                    <div class="alert alert-danger alert-dismissible fade show" role="alert">
                        <i class="bi bi-exclamation-triangle-fill me-2"></i> An unexpected network error occurred while syncing.
                        <button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>
                    </div>
                `;
            } finally {
                syncBtn.disabled = false;
                syncSpinner.classList.add('d-none');
                syncBtnText.textContent = 'Sync from SMTP2GO';
            }
        });
    }

    // Timeline Modal logic
    const timelineModalEl = document.getElementById('emailTimelineModal');
    let timelineModal = null;
    if (timelineModalEl) {
        timelineModal = new bootstrap.Modal(timelineModalEl);
    }

    const timelineLoading = document.getElementById('timelineLoading');
    const timelineOverview = document.getElementById('timelineOverview');
    const timelineContainer = document.getElementById('timelineContainer');
    const timelineItems = document.getElementById('timelineItems');
    const timelineEmpty = document.getElementById('timelineEmpty');
    const timelineEmailId = document.getElementById('timelineEmailId');
    const timelineRecipient = document.getElementById('timelineRecipient');
    const timelineSender = document.getElementById('timelineSender');
    const timelineSubject = document.getElementById('timelineSubject');
    const timelineSentAt = document.getElementById('timelineSentAt');
    const timelineSummaryBadges = document.getElementById('timelineSummaryBadges');

    document.querySelectorAll('.view-timeline-btn').forEach(button => {
        button.addEventListener('click', async function () {
            const timelineUrl = this.getAttribute('data-timeline-url');
            const emailId = this.getAttribute('data-email-id');

            if (!timelineModal || !timelineUrl) return;

            // Reset modal state
            timelineEmailId.textContent = emailId ? `(ID: ${emailId})` : '';
            timelineLoading.classList.remove('d-none');
            timelineOverview.classList.add('d-none');
            timelineContainer.classList.add('d-none');
            timelineEmpty.classList.add('d-none');
            timelineItems.innerHTML = '';
            timelineSummaryBadges.innerHTML = '';

            timelineModal.show();

            try {
                const response = await fetch(timelineUrl);
                if (!response.ok) {
                    throw new Error('Failed to load timeline data');
                }

                const data = await response.json();
                renderTimeline(data);
            } catch (error) {
                console.error('Error loading timeline:', error);
                timelineLoading.classList.add('d-none');
                timelineEmpty.textContent = 'Failed to load timeline events for this email.';
                timelineEmpty.classList.remove('d-none');
            }
        });
    });

    function getEventBadgeClass(eventName) {
        const lower = (eventName || '').toLowerCase();
        if (lower === 'delivered') return 'bg-success';
        if (lower === 'open' || lower === 'opened') return 'bg-info text-dark';
        if (lower === 'click' || lower === 'clicked') return 'bg-primary';
        if (lower.includes('bounce') || lower === 'spam' || lower === 'rejected') return 'bg-danger';
        if (lower === 'unsub' || lower === 'unsubscribed') return 'bg-warning text-dark';
        return 'bg-secondary';
    }

    function getEventIcon(eventName) {
        const lower = (eventName || '').toLowerCase();
        if (lower === 'delivered') return 'bi-check-circle-fill text-success';
        if (lower === 'open' || lower === 'opened') return 'bi-envelope-open-fill text-info';
        if (lower === 'click' || lower === 'clicked') return 'bi-cursor-fill text-primary';
        if (lower.includes('bounce') || lower === 'rejected') return 'bi-x-circle-fill text-danger';
        if (lower === 'spam') return 'bi-exclamation-triangle-fill text-danger';
        if (lower === 'unsub' || lower === 'unsubscribed') return 'bi-person-dash-fill text-warning';
        return 'bi-send-fill text-secondary';
    }

    function escapeHtml(str) {
        if (!str) return '';
        return String(str)
            .replace(/&/g, '&amp;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;')
            .replace(/"/g, '&quot;')
            .replace(/'/g, '&#039;');
    }

    function renderTimeline(data) {
        timelineLoading.classList.add('d-none');
        timelineOverview.classList.remove('d-none');

        // Populate header overview
        timelineRecipient.textContent = data.recipient || '-';
        timelineSender.textContent = data.from || '-';
        timelineSubject.textContent = data.subject || '(No Subject)';
        timelineSentAt.textContent = data.sentAt || '-';

        let badges = [];
        if (data.isDelivered) badges.push('<span class="badge bg-success me-1"><i class="bi bi-check-circle me-1"></i>Delivered</span>');
        if (data.isOpened) badges.push(`<span class="badge bg-info text-dark me-1"><i class="bi bi-envelope-open me-1"></i>Opened (${data.openCount}x)</span>`);
        if (data.isClicked) badges.push(`<span class="badge bg-primary me-1"><i class="bi bi-cursor-fill me-1"></i>Clicked (${data.clickCount}x)</span>`);
        if (data.hasError) badges.push('<span class="badge bg-danger me-1"><i class="bi bi-exclamation-circle me-1"></i>Error / Bounced</span>');
        timelineSummaryBadges.innerHTML = badges.join(' ');

        if (!data.events || data.events.length === 0) {
            timelineEmpty.classList.remove('d-none');
            return;
        }

        timelineContainer.classList.remove('d-none');

        let html = '<div class="list-group list-group-flush">';
        data.events.forEach((evt, idx) => {
            const badgeClass = getEventBadgeClass(evt.event);
            const iconClass = getEventIcon(evt.event);
            const isClick = (evt.event || '').toLowerCase() === 'click';
            const errorMsg = evt.errorMessage || evt.smtpResponse;

            html += `
                <div class="list-group-item px-2 py-3 border-bottom">
                    <div class="d-flex justify-content-between align-items-start mb-1">
                        <div class="d-flex align-items-center gap-2">
                            <i class="bi ${iconClass} fs-5"></i>
                            <span class="badge ${badgeClass} text-uppercase px-2 py-1">${escapeHtml(evt.event)}</span>
                            <span class="fw-semibold small">${escapeHtml(evt.eventAt)}</span>
                        </div>
                        <span class="text-muted small">#${idx + 1}</span>
                    </div>

                    ${isClick && evt.clickUrl ? `
                        <div class="mt-2 p-2 bg-light rounded border">
                            <span class="fw-bold small text-primary"><i class="bi bi-link-45deg"></i> Clicked Target:</span>
                            <a href="${escapeHtml(evt.clickUrl)}" target="_blank" rel="noopener noreferrer" class="text-break small d-block">
                                ${escapeHtml(evt.clickUrl)}
                            </a>
                        </div>
                    ` : ''}

                    <div class="d-flex flex-wrap gap-2 mt-2">
                        ${evt.host ? `
                            <span class="badge bg-light text-dark border small fw-normal">
                                <i class="bi bi-server me-1 text-secondary"></i>Host: ${escapeHtml(evt.host)}
                            </span>
                        ` : ''}
                        ${evt.byteSize ? `
                            <span class="badge bg-light text-dark border small fw-normal">
                                <i class="bi bi-file-earmark me-1 text-secondary"></i>Size: ${escapeHtml(evt.byteSize)} bytes
                            </span>
                        ` : ''}
                    </div>

                    ${evt.userAgent ? `
                        <div class="small text-muted mt-2 text-truncate" title="${escapeHtml(evt.userAgent)}">
                            <i class="bi bi-laptop me-1"></i> ${escapeHtml(evt.userAgent)}
                        </div>
                    ` : ''}

                    ${errorMsg && (evt.event.toLowerCase().includes('bounce') || evt.event.toLowerCase() === 'spam' || evt.event.toLowerCase() === 'rejected') ? `
                        <div class="alert alert-danger py-2 px-3 mt-2 mb-0 small">
                            <strong><i class="bi bi-exclamation-triangle-fill me-1"></i> Diagnostic Message:</strong>
                            <div class="text-break mt-1 font-monospace" style="font-size: 0.8rem;">${escapeHtml(errorMsg)}</div>
                        </div>
                    ` : ''}
                </div>
            `;
        });
        html += '</div>';

        timelineItems.innerHTML = html;
    }
});
