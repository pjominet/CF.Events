(function() {
    "use strict";

    // Donation type toggle
    const donationCheckboxes = document.querySelectorAll('input[name="Event.DonationTypes"]');
    if (donationCheckboxes.length > 0) {
        donationCheckboxes.forEach(cb => {
            cb.addEventListener('change', function() {
                const ibanWrapper = document.getElementById('donationIbanWrapper');
                const linkWrapper = document.getElementById('donationLinkWrapper');
                const physicalWrapper = document.getElementById('donationPhysicalWrapper');
                const ibanCb = document.getElementById('donationTypeIban');
                const linkCb = document.getElementById('donationTypeLink');
                const physicalCb = document.getElementById('donationTypePhysical');
                if (ibanWrapper) ibanWrapper.style.display = (ibanCb && ibanCb.checked) ? 'block' : 'none';
                if (linkWrapper) linkWrapper.style.display = (linkCb && linkCb.checked) ? 'block' : 'none';
                if (physicalWrapper) physicalWrapper.style.display = (physicalCb && physicalCb.checked) ? 'block' : 'none';
            });
        });
    }

    // Dynamic rows helper
    function setupRemoveButtons(container) {
        if (!container) return;
        container.addEventListener('click', function(e) {
            const removeBtn = e.target.closest('.remove-row');
            if (removeBtn) {
                removeBtn.closest('.schedule-row, .faq-row').remove();
                reindexRows(container);
            }
        });
    }

    function reindexRows(container) {
        if (!container) return;
        const isFaq = container.id === 'faq-container';
        container.querySelectorAll('.schedule-row, .faq-row').forEach((row, index) => {
            row.querySelectorAll('input, textarea').forEach(input => {
                const name = input.getAttribute('name');
                if (name) {
                    // Update the index in the name attribute, e.g., Event.ScheduleSteps[0].Day -> Event.ScheduleSteps[1].Day
                    input.setAttribute('name', name.replace(/\[\d+\]/, '[' + index + ']'));
                }
                const id = input.getAttribute('id');
                if (id) {
                    // Update the index in the id attribute, e.g., Event_ScheduleSteps_0__Day -> Event_ScheduleSteps_1__Day
                    input.setAttribute('id', id.replace(/_\d+__/, '_' + index + '__'));
                }

                if (isFaq && input.classList.contains('faq-sort-order')) {
                    input.value = index;
                }
            });
        });
    }

    const scheduleContainer = document.getElementById('schedule-container');
    const faqContainer = document.getElementById('faq-container');

    setupRemoveButtons(scheduleContainer);
    setupRemoveButtons(faqContainer);

    const addScheduleBtn = document.getElementById('add-schedule');
    if (addScheduleBtn) {
        addScheduleBtn.addEventListener('click', function() {
            const index = scheduleContainer.querySelectorAll('.schedule-row').length;
            const maxDays = scheduleContainer.getAttribute('data-max-days') || '';
            const html = `
                <div class="row gx-2 mb-2 schedule-row">
                    <div class="col-md-2">
                        <div class="input-group">
                            <span class="input-group-text">Day</span>
                            <input name="Event.ScheduleSteps[${index}].Day" type="number" class="form-control" placeholder="Day" value="1" min="1" max="${maxDays}" step="1" />
                        </div>
                    </div>
                    <div class="col-md-2">
                        <input name="Event.ScheduleSteps[${index}].TimeStamp" type="time" class="form-control" title="Start Time" />
                    </div>
                    <div class="col-md-2">
                        <input name="Event.ScheduleSteps[${index}].EndTime" type="time" class="form-control" title="End Time" />
                    </div>
                    <div class="col-md-5">
                        <input name="Event.ScheduleSteps[${index}].Label" type="text" class="form-control" placeholder="Label" />
                    </div>
                    <div class="col-md-1">
                        <button type="button" class="btn btn-link text-danger remove-row"><i class="bi bi-x-lg"></i></button>
                    </div>
                </div>`;
            scheduleContainer.insertAdjacentHTML('beforeend', html);
        });
    }

    const addFaqBtn = document.getElementById('add-faq');
    if (addFaqBtn) {
        addFaqBtn.addEventListener('click', function() {
            const index = faqContainer.querySelectorAll('.faq-row').length;
            const html = `
                <div class="row gx-2 faq-row align-items-center">
                    <div class="col-auto">
                        <i class="bi bi-list faq-handle" style="cursor: grab;"></i>
                    </div>
                    <div class="col">
                        <div class="mb-2">
                            <input name="Event.FaqItems[${index}].Question" type="text" class="form-control" placeholder="Question" />
                        </div>
                        <div class="mb-2">
                            <textarea name="Event.FaqItems[${index}].Answer" class="form-control" placeholder="Answer" data-max-length="1000" data-counter-id="faq-c-${index}"></textarea>
                            <div class="d-flex justify-content-end mt-2">
                            <small id="faq-c-${index}" class="text-muted"></small>
                        </div>
                        </div>
                        <input type="hidden" name="Event.FaqItems[${index}].SortOrder" class="faq-sort-order" value="${index}" />
                    </div>
                    <div class="col-auto">
                        <button type="button" class="btn btn-link text-danger remove-row"><i class="bi bi-x-lg"></i></button>
                    </div>
                </div>`;
            faqContainer.insertAdjacentHTML('beforeend', html);
            window.initCharacterCounters(faqContainer);
        });
    }

    // FAQ Drag and Drop
    if (faqContainer && typeof Sortable !== 'undefined') {
        new Sortable(faqContainer, {
            handle: '.faq-handle',
            animation: 150,
            ghostClass: 'bg-light',
            onEnd: function() {
                reindexRows(faqContainer);
                isDirty = true;
            }
        });
    }

    // Start/End date validation
    const startDateInput = document.getElementById('Event_StartDate');
    const endDateInput = document.getElementById('Event_EndDate');
    const scheduleWarning = document.getElementById('schedule-warning');
    let originalMaxDays = 0;
    let eventDurationChanged = false;

    // Change tracking
    let isDirty = false;
    const eventForm = document.getElementById('eventForm');

    if (eventForm) {
        // Track changes on regular inputs
        eventForm.addEventListener('input', () => isDirty = true);
        eventForm.addEventListener('change', () => isDirty = true);

        // RTE-specific: Since RTE updates the textarea and triggers 'change',
        // the event listener above should already catch it.

        eventForm.addEventListener('submit', () => {
            isDirty = false;
        });

        // Handle navigation confirmation
        const handleNavigation = async (e, url) => {
            if (isDirty) {
                e.preventDefault();
                e.stopPropagation();

                const saveBefore = await window.customConfirm('You have unsaved changes. Would you like to save them before previewing?', {
                    title: 'Unsaved Changes',
                    confirmText: 'Save and Continue',
                    cancelText: 'Discard Changes',
                    confirmClass: 'btn-success'
                });

                if (saveBefore) {
                    const submitBtn = eventForm.querySelector('button[type="submit"]');
                    if (submitBtn) {
                        let redirectInput = document.getElementById('redirectAfterSave');
                        if (!redirectInput) {
                            redirectInput = document.createElement('input');
                            redirectInput.type = 'hidden';
                            redirectInput.id = 'redirectAfterSave';
                            redirectInput.name = 'RedirectAfterSave';
                            eventForm.appendChild(redirectInput);
                        }
                        redirectInput.value = url;
                        submitBtn.click();
                    }
                } else {
                    const discard = await window.customConfirm('Discard changes and proceed to preview?', {
                        title: 'Confirm Discard',
                        confirmText: 'Discard and Proceed',
                        cancelText: 'Stay on Page',
                        confirmClass: 'btn-danger'
                    });

                    if (discard) {
                        isDirty = false; // Prevent beforeunload trigger
                        window.location.href = url;
                    }
                }
            }
        };

        // Attach to preview links and other navigation
        const attachNavigationHandlers = () => {
            const previewLinks = document.querySelectorAll('a[class*="confirm-required"]');
            previewLinks.forEach(link => {
                // Remove existing to avoid double handlers if called multiple times
                link.removeEventListener('click', link._navHandler);
                link._navHandler = (e) => handleNavigation(e, link.href);
                link.addEventListener('click', link._navHandler);
            });
        };

        attachNavigationHandlers();

        // Also watch for dynamic links (if any are added later)
        const observer = new MutationObserver(attachNavigationHandlers);
        observer.observe(eventForm, { childList: true, subtree: true });

        window.addEventListener('beforeunload', (e) => {
            if (isDirty) {
                e.preventDefault();
                e.returnValue = '';
            }
        });
    }

    if (startDateInput && endDateInput) {
        const calculateDays = () => {
            if (startDateInput.value && endDateInput.value) {
                const start = new Date(startDateInput.value);
                const end = new Date(endDateInput.value);
                const diffTime = Math.abs(end - start);
                return Math.ceil(diffTime / (1000 * 60 * 60 * 24)) + 1;
            }
            return 0;
        };

        originalMaxDays = calculateDays();

        const updateMaxDays = () => {
            const diffDays = calculateDays();
            if (diffDays > 0) {
                if (scheduleContainer) {
                    scheduleContainer.setAttribute('data-max-days', diffDays);
                    let stepRemoved = false;
                    scheduleContainer.querySelectorAll('.schedule-row').forEach(row => {
                        const dayInput = row.querySelector('input[name$=".Day"]');
                        if (dayInput) {
                            const currentDay = parseInt(dayInput.value);
                            if (currentDay > diffDays) {
                                row.remove();
                                stepRemoved = true;
                            } else {
                                dayInput.setAttribute('max', diffDays);
                            }
                        }
                    });

                    if (stepRemoved) {
                        eventDurationChanged = true;
                        reindexRows(scheduleContainer);
                    }

                    if (eventDurationChanged && diffDays < originalMaxDays) {
                        if (scheduleWarning) {
                            scheduleWarning.classList.remove('d-none');
                        }
                    } else {
                        if (scheduleWarning) {
                            scheduleWarning.classList.add('d-none');
                        }
                    }
                }
            }
        };

        const updateMinEndDate = () => {
            endDateInput.min = startDateInput.value;
            if (endDateInput.value && endDateInput.value < startDateInput.value) {
                endDateInput.value = startDateInput.value;
            }
            updateMaxDays();
        };
        startDateInput.addEventListener('change', updateMinEndDate);
        startDateInput.addEventListener('input', updateMinEndDate);
        endDateInput.addEventListener('change', updateMaxDays);
        endDateInput.addEventListener('input', updateMinEndDate);
        updateMinEndDate();
    }
})();
