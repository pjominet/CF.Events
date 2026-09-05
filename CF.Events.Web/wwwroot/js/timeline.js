(function () {
    let intervalId = null;

    const SEGMENT_DURATION = 0.4; // seconds

    function applyAnimations(item, index, animate) {
        if (animate) {
            if (item.classList.contains('animate')) return;

            // Only animate if already completed (to avoid flashing all markers) or if it's the current one.
            if (item.classList.contains('completed') || item.classList.contains('current')) {
                // Add sequential delay for a nice drawing effect
                // Each segment starts after the previous one finishes
                const delay = index * SEGMENT_DURATION;
                item.style.transitionDelay = `${delay}s`;

                // For the marker pulse, we want it to start after the line reaches it
                const marker = item.querySelector('.timeline-marker');
                if (marker) {
                    marker.style.animationDelay = `${delay + SEGMENT_DURATION}s`;
                }

                item.classList.add('animate');
            }
        }
    }

    function updateTimelineProgress(animate = false) {
        const modal = document.getElementById('eventScheduleModal');
        if (!modal) return;

        const now = new Date();
        const todayStr = now.toISOString().split('T')[0];

        document.querySelectorAll('.tab-pane[data-date]').forEach(pane => {
            const paneDate = pane.getAttribute('data-date');
            const isToday = paneDate === todayStr;
            const isPast = new Date(paneDate) < new Date(todayStr);

            pane.querySelectorAll('.timeline-item').forEach((item, idx) => {
                applyAnimations(item, idx, animate);

                if (!isToday) {
                    item.classList.toggle('completed', isPast);
                    item.classList.remove('current');
                    return;
                }

                const startStr = item.getAttribute('data-start');
                const nextStartStr = item.getAttribute('data-next-start');
                const startTime = new Date(todayStr + 'T' + startStr + ':00');

                let isCompleted = now > startTime;
                let isCurrent = false;

                if (nextStartStr) {
                    const nextStartTime = new Date(todayStr + 'T' + nextStartStr + ':00');
                    isCurrent = now >= startTime && now < nextStartTime;
                } else {
                    const twoHoursLater = new Date(startTime.getTime() + 2 * 60 * 60 * 1000);
                    isCurrent = now >= startTime && now < twoHoursLater;
                }

                if (isCurrent) isCompleted = false;

                const wasCurrent = item.classList.contains('current');
                item.classList.toggle('completed', isCompleted);
                item.classList.toggle('current', isCurrent);

                if (isCurrent && !wasCurrent && modal.classList.contains('show')) {
                    item.scrollIntoView({behavior: 'smooth', block: 'center'});
                }
            });
        });
    }

    // Use event delegation for Bootstrap modal events to handle AJAX-injected content
    document.addEventListener('shown.bs.modal', function (event) {
        if (event.target.id !== 'eventScheduleModal') return;

        // First set the state WITHOUT animation to ensure 'completed' classes are present
        updateTimelineProgress(false);
        // Then run with animation flag - applyAnimations will now know what is completed
        updateTimelineProgress(true);

        if (!intervalId) {
            intervalId = setInterval(updateTimelineProgress, 60000);
        }

        const currentStep = document.querySelector('.timeline-item.current');
        if (currentStep) {
            currentStep.scrollIntoView({behavior: 'smooth', block: 'center'});
        } else {
            const completedSteps = document.querySelectorAll('.timeline-item.completed');
            if (completedSteps.length > 0) {
                completedSteps[completedSteps.length - 1].scrollIntoView({behavior: 'smooth', block: 'center'});
            }
        }
    });

    document.addEventListener('hidden.bs.modal', function (event) {
        if (event.target.id !== 'eventScheduleModal') return;

        if (intervalId) {
            clearInterval(intervalId);
            intervalId = null;
        }

        // Remove animate class so it can play again next time
        document.querySelectorAll('.timeline-item.animate').forEach(item => {
            item.classList.remove('animate');
            item.style.transitionDelay = '';
            const marker = item.querySelector('.timeline-marker');
            if (marker) {
                marker.style.animationDelay = '';
            }
        });
    });

    // Handle AJAX-injected content by listening for Bootstrap modal creation or content updates
    // Use a MutationObserver on the body to find when the modal is added to the DOM
    const observer = new MutationObserver(function (mutations) {
        mutations.forEach(function (mutation) {
            mutation.addedNodes.forEach(function (node) {
                if (node.id === 'eventScheduleModal' || (node.nodeType === 1 && node.querySelector('#eventScheduleModal'))) {
                    updateTimelineProgress();
                }
            });
        });
    });
    observer.observe(document.body, {childList: true, subtree: true});

    // Also try initial check in case it's already there
    updateTimelineProgress();
})();
