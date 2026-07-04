(function() {
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
            editEventModal.querySelector('[name="Date"]').value = eventData.date;
            editEventModal.querySelector('[name="Location"]').value = eventData.location;
            editEventModal.querySelector('[name="Description"]').value = eventData.description;

            let codeControl = editEventModal.querySelector('[name="AccommodationCodes"]').tomselect;
            codeControl.addOptions(eventData.accommodationCodes.map(code => ({ value: code, text: code })))
            eventData.accommodationCodes.forEach(code => codeControl.addItem(code, true));

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
        });
    }
})();
