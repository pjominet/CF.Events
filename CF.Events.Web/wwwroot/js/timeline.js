(function() {
    const modal = document.getElementById('eventScheduleModal');
    if (!modal) return;

    function updateTimelineProgress() {
        const now = new Date();
        const todayStr = now.toISOString().split('T')[0];

        document.querySelectorAll('.tab-pane[data-date]').forEach(pane => {
            const paneDate = pane.getAttribute('data-date');
            const isToday = paneDate === todayStr;

            if (!isToday) {
                // If pane date is in the past, all items are completed
                // If in the future, none are.
                const isPast = new Date(paneDate) < new Date(todayStr);
                pane.querySelectorAll('.timeline-item').forEach(item => {
                    item.classList.toggle('completed', isPast);
                    item.classList.remove('current');
                });
                return;
            }

            const items = pane.querySelectorAll('.timeline-item');
            items.forEach((item, index) => {
                const startStr = item.getAttribute('data-start');
                const nextStartStr = item.getAttribute('data-next-start');

                const startTime = new Date(`${todayStr}T${startStr}:00`);
                let isCompleted = now > startTime;
                let isCurrent = false;

                if (nextStartStr) {
                    const nextStartTime = new Date(`${todayStr}T${nextStartStr}:00`);
                    isCurrent = now >= startTime && now < nextStartTime;
                } else {
                    // Fallback for last item of the day: current for 2 hours
                    const twoHoursLater = new Date(startTime.getTime() + 2 * 60 * 60 * 1000);
                    isCurrent = now >= startTime && now < twoHoursLater;
                }

                if (isCurrent) isCompleted = false;

                const wasCurrent = item.classList.contains('current');
                item.classList.toggle('completed', isCompleted);
                item.classList.toggle('current', isCurrent);

                // Auto-scroll if it just became current
                if (isCurrent && !wasCurrent && modal.classList.contains('show')) {
                    item.scrollIntoView({ behavior: 'smooth', block: 'center' });
                }
            });
        });
    }

    let intervalId = null;

    modal.addEventListener('shown.bs.modal', function () {
        updateTimelineProgress();

        // Start interval when shown
        if (!intervalId) {
            intervalId = setInterval(updateTimelineProgress, 60000);
        }

        const currentStep = document.querySelector('.timeline-item.current');
        if (currentStep) {
            currentStep.scrollIntoView({ behavior: 'smooth', block: 'center' });
        } else {
            const completedSteps = document.querySelectorAll('.timeline-item.completed');
            if (completedSteps.length > 0) {
                completedSteps[completedSteps.length - 1].scrollIntoView({ behavior: 'smooth', block: 'center' });
            }
        }
    });

    modal.addEventListener('hidden.bs.modal', function() {
        if (intervalId) {
            clearInterval(intervalId);
            intervalId = null;
        }
    });
})();
