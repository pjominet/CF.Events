(function () {
    let intervalId = null;

    const SEGMENT_DURATION = 0.3; // seconds

    function applyAnimations(item, index, animate) {
        if (!animate) return;
        if (item.classList.contains('animate')) return;
        if (item.classList.contains('completed') || item.classList.contains('current')) {
            // Add sequential delay for a nice drawing effect
            // Each segment starts slightly before the previous one finishes to create a continuous look
            const delay = index * (SEGMENT_DURATION * 0.7);
            item.style.transitionDelay = `${delay}s`;

            // For the marker pulse and fill, we want it to start as the line approaches it
            const marker = item.querySelector('.timeline-marker');
            if (marker) {
                // Start filling the marker when the line segment is about halfway through
                // since the marker is near the top of the item and the line draws downward.
                const fillDelay = delay + (SEGMENT_DURATION * 0.35);
                marker.style.transitionDelay = `${fillDelay}s`;
                marker.style.animationDelay = `${fillDelay}s`;
            }

            item.classList.add('animate');
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

                // Parse start time components
                const [startHour, startMinute] = startStr.split(':').map(Number);

                // If the hour is < 6, it belongs to the next calendar day logically
                let startTime = new Date(paneDate + 'T' + startStr + ':00');
                if (startHour < 6) {
                    startTime.setDate(startTime.getDate() + 1);
                }

                let isCompleted = now > startTime;
                let isCurrent;

                if (nextStartStr) {
                    const [nextHour, nextMinute] = nextStartStr.split(':').map(Number);
                    let nextStartTime = new Date(paneDate + 'T' + nextStartStr + ':00');
                    if (nextHour < 6) {
                        nextStartTime.setDate(nextStartTime.getDate() + 1);
                    }
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
                marker.style.transitionDelay = '';
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
