(function() {
    const grid = document.getElementById('invitesGrid');
    if (!grid) return;

    const pageSize = parseInt(grid.dataset.pageSize);
    const totalCount = parseInt(grid.dataset.totalCount);
    const loadUrl = grid.dataset.loadUrl;
    let currentPage = 1;
    let isLoading = false;
    let hasMore = totalCount > pageSize;

    const spinner = document.getElementById('loadingSpinner');

    async function loadMore() {
        if (isLoading || !hasMore) return;

        isLoading = true;
        spinner.style.display = 'block';
        currentPage++;

        try {
            const response = await fetch(`${loadUrl}?pageNumber=${currentPage}`, {
                headers: { 'X-Requested-With': 'XMLHttpRequest' }
            });

            if (!response.ok) {
                hasMore = false;
                return;
            }

            const html = await response.text();
            const temp = document.createElement('div');
            temp.innerHTML = html;

            const newCards = Array.from(temp.querySelectorAll('#invitesGrid > [class*="col-"]'));
            if (newCards.length === 0) {
                hasMore = false;
                return;
            }

            newCards.forEach(card => grid.insertBefore(card, spinner));

            if (newCards.length < pageSize) hasMore = false;
        } catch (error) {
            console.error('Error loading invites:', error);
            hasMore = false;
        } finally {
            isLoading = false;
            spinner.style.display = 'none';
        }
    }

    function checkScroll() {
        if (isLoading || !hasMore) return;
        const { scrollTop, scrollHeight, clientHeight } = document.documentElement;
        if (scrollTop + clientHeight > scrollHeight - 200) loadMore();
    }

    if (hasMore) {
        window.addEventListener('scroll', checkScroll);
        window.addEventListener('resize', checkScroll);
    }

    window.addEventListener('beforeunload', () => {
        window.removeEventListener('scroll', checkScroll);
        window.removeEventListener('resize', checkScroll);
    });

    // RSVP Details Modal handling
    const eventContainer = document.getElementById('_eventDetailContainer');
    if (eventContainer) {
        document.addEventListener('click', async function(e) {
            const btn = e.target.closest('button[data-event-id]');
            if (!btn) return;

            const eventId = btn.dataset.eventId;
            if (!eventId) return;

            const action = btn.dataset.action;
            if (!action) return;

            const target = btn.dataset.target;
            if (!target) return;

            try {
                btn.disabled = true;
                const response = await fetch(`/events/${eventId}/${action}`, {
                    headers: { 'X-Requested-With': 'XMLHttpRequest' }
                });

                if (response.ok) {
                    eventContainer.innerHTML = await response.text();

                    const modal = document.getElementById(target);
                    if (modal) {
                        const modal = new bootstrap.Modal(modal);
                        modal.show();
                    }
                }
            } catch (error) {
                console.error('Error fetching event details:', error);
            } finally {
                btn.disabled = false;
            }
        });
    }
})();
