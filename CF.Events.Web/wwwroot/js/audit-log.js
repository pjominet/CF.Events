(function () {
    let page = 1;
    let loading = false;
    const tableBody = document.getElementById('auditTableBody');
    const scrollContainer = document.getElementById('auditScrollContainer');
    const loadingIndicator = document.getElementById('loadingIndicator');

    // Initial state from data attributes or global var if needed,
    // but we can infer hasMore from initial load count
    let hasMore = tableBody.children.length === 50;

    // Initialize tooltips for existing rows
    initTooltips(tableBody);

    const observer = new IntersectionObserver((entries) => {
        if (entries[0].isIntersecting && !loading && hasMore) {
            loadMore();
        }
    }, {
        root: scrollContainer,
        rootMargin: '100px'
    });

    function updateObserver() {
        const lastRow = tableBody.lastElementChild;
        if (lastRow) {
            observer.disconnect();
            observer.observe(lastRow);
        }
    }

    async function loadMore() {
        loading = true;
        loadingIndicator.classList.remove('d-none');

        try {
            const response = await fetch(`?handler=LoadMore&page=${page}`);
            const data = await response.json();

            if (data.length > 0) {
                const fragment = document.createDocumentFragment();
                data.forEach(audit => {
                    const row = document.createElement('tr');
                    const date = new Date(audit.loginAt).toLocaleString(undefined, {
                        year: 'numeric', month: 'numeric', day: 'numeric',
                        hour: 'numeric', minute: 'numeric'
                    });

                    const methodBadge = audit.authMethod === 'Password' ? 'bg-primary' :
                                       audit.authMethod === 'EmailToken' ? 'bg-info' : 'bg-secondary';

                    const userAgentShort = audit.userAgent.length > 50 ?
                        audit.userAgent.substring(0, 47) + '...' : audit.userAgent;

                    row.innerHTML = `
                        <td class="text-nowrap">${date}</td>
                        <td class="fw-bold">${escapeHtml(audit.displayName)}</td>
                        <td>${escapeHtml(audit.email)}</td>
                        <td><code>${escapeHtml(audit.ipAddress)}</code></td>
                        <td><span class="badge ${methodBadge}">${escapeHtml(audit.authMethod)}</span></td>
                        <td class="small text-muted text-truncate"
                            style="max-width: 250px;"
                            title="${escapeHtml(audit.userAgent)}"
                            data-bs-toggle="tooltip"
                            data-bs-placement="top">
                            ${escapeHtml(userAgentShort)}
                        </td>
                    `;
                    fragment.appendChild(row);
                });

                tableBody.appendChild(fragment);
                initTooltips(fragment);

                page++;
                hasMore = data.length === 50;
                updateObserver();
            } else {
                hasMore = false;
            }
        } catch (error) {
            console.error('Error loading more audits:', error);
        } finally {
            loading = false;
            loadingIndicator.classList.add('d-none');
        }
    }

    function initTooltips(container) {
        const tooltipTriggerList = container.querySelectorAll('[data-bs-toggle="tooltip"]');
        [...tooltipTriggerList].map(tooltipTriggerEl => new bootstrap.Tooltip(tooltipTriggerEl));
    }

    function escapeHtml(text) {
        const div = document.createElement('div');
        div.textContent = text;
        return div.innerHTML;
    }

    if (hasMore) {
        updateObserver();
    }
})();
