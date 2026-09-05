(function() {
    const modal = document.getElementById('eventScheduleModal');
    if (!modal) return;

    function parseTime(timeStr, baseDate = new Date(2000, 0, 1)) {
        const [h, m] = timeStr.split(':').map(Number);
        const date = new Date(baseDate);
        date.setHours(h, m, 0, 0);
        return date;
    }

    function calculateGap(item) {
        const startStr = item.getAttribute('data-start');
        const endStr = item.getAttribute('data-end');
        const nextStartStr = item.getAttribute('data-next-start');

        if (!nextStartStr) {
            item.classList.remove('gap');
            return;
        }

        const currentEndTimeStr = endStr || startStr;
        const endDate = parseTime(currentEndTimeStr);
        let nextDate = parseTime(nextStartStr);

        if (nextDate < endDate) nextDate.setDate(nextDate.getDate() + 1);

        const diffMin = (nextDate - endDate) / (1000 * 60);
        item.classList.toggle('gap', diffMin > 15);
    }

    function applyAnimations(item, index, animate) {
        if (animate) {
            item.classList.add('animate');
            item.style.animationDelay = `${index * 0.1}s`;
        }
    }

    function updateTimelineProgress(animate = false) {
        const now = new Date();
        const todayStr = now.toISOString().split('T')[0];

        document.querySelectorAll('.tab-pane[data-date]').forEach(pane => {
            const paneDate = pane.getAttribute('data-date');
            const isToday = paneDate === todayStr;
            const isPast = new Date(paneDate) < new Date(todayStr);

            pane.querySelectorAll('.timeline-item').forEach((item, idx) => {
                applyAnimations(item, idx, animate);
                calculateGap(item);

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
                    item.scrollIntoView({ behavior: 'smooth', block: 'center' });
                }
            });
        });
    }

    let intervalId = null;

    modal.addEventListener('shown.bs.modal', function () {
        updateTimelineProgress(true);

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

    // Initial check
    updateTimelineProgress();
})();
