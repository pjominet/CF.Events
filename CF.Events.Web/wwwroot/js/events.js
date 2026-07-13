(function () {
    const editEventModal = document.getElementById('editEventModal');
    if (editEventModal) {
        editEventModal.addEventListener('show.bs.modal', event => {
            const button = event.relatedTarget;
            if (!button || button.id === 'newEventBtn') {
                // Reset form for new event
                editEventModal.querySelector('form').reset();
                editEventModal.querySelector('[name="Id"]').value = 0;
                editEventModal.querySelector('.modal-title').textContent = 'Create New Event';
                editEventModal.querySelector('button[type="submit"]').textContent = 'Create Event';

                const imageWrapper = document.getElementById('currentInvitationImageWrapper');
                if (imageWrapper) imageWrapper.style.display = 'none';
                return;
            }

            // Populate form for edit
            const eventData = JSON.parse(button.getAttribute('data-bs-event'));

            const modalTitle = editEventModal.querySelector('.modal-title');
            const submitBtn = editEventModal.querySelector('button[type="submit"]');

            modalTitle.textContent = 'Edit Event';
            submitBtn.textContent = 'Save Changes';

            editEventModal.querySelector('[name="Id"]').value = eventData.id;
            editEventModal.querySelector('[name="Name"]').value = eventData.name;
            editEventModal.querySelector('[name="StartDate"]').value = eventData.startDate;
            editEventModal.querySelector('[name="EndDate"]').value = eventData.endDate;
            editEventModal.querySelector('[name="Location"]').value = eventData.location;
            editEventModal.querySelector('[name="AccommodationDetails"]').value = eventData.accommodationDetails;
            editEventModal.querySelector('[name="Description"]').value = eventData.description;
            editEventModal.querySelector('[name="SaveDateEmailTemplateId"]').value = eventData.saveDateTemplateId || '';
            editEventModal.querySelector('[name="InvitationEmailTemplateId"]').value = eventData.invitationTemplateId || '';
            editEventModal.querySelector('[name="DonationIban"]').value = eventData.donationIban || '';

            let codesControl = editEventModal.querySelector('[name="AccommodationCodes"]').tomselect;
            codesControl.addOptions(eventData.accommodationCodes.map(code => ({value: code, text: code})))
            eventData.accommodationCodes.forEach(code => codesControl.addItem(code, true));

            let linksControl = editEventModal.querySelector('[name="BookingLinks"]').tomselect;
            linksControl.addOptions(eventData.bookingLinks.map(link => ({value: link, text: link})))
            eventData.bookingLinks.forEach(link => linksControl.addItem(link, true));

            const imageWrapper = document.getElementById('currentInvitationImageWrapper');
            const imageNameSpan = document.getElementById('currentInvitationImageName');
            if (imageWrapper && imageNameSpan) {
                if (eventData.originalInvitationFileName) {
                    imageNameSpan.textContent = eventData.originalInvitationFileName;
                    imageWrapper.style.display = 'block';
                } else {
                    imageWrapper.style.display = 'none';
                }
            }

            const startDateInput = document.getElementById('StartDate');
            const endDateInput = document.getElementById('EndDate');

            if (startDateInput && endDateInput) {
                // Function to update the minimum end date
                const updateMinEndDate = () => {
                    endDateInput.min = startDateInput.value;

                    // Optional: If the current end date is now before the new start date, reset it
                    if (endDateInput.value && endDateInput.value < startDateInput.value) {
                        endDateInput.value = startDateInput.value;
                    }
                };

                // Listen for changes on the Start Date
                startDateInput.addEventListener('change', updateMinEndDate);

                // Run once on load to set initial state (useful when editing an event)
                updateMinEndDate();
            }
        });
    }
})();
