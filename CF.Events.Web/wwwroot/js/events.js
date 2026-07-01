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
            editEventModal.querySelector('[name="StartDate"]').value = eventData.startDate;
            editEventModal.querySelector('[name="EndDate"]').value = eventData.endDate;
            editEventModal.querySelector('[name="Location"]').value = eventData.location;
            editEventModal.querySelector('[name="Description"]').value = eventData.description;
            editEventModal.querySelector('[name="AccommodationCode"]').value = eventData.accommodationCode || '';

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

            const accommodationWrapper = document.getElementById('accommodationCodeWrapper');
            if (accommodationWrapper) {
                accommodationWrapper.style.display = eventData.showAccommodationOptions ? 'block' : 'none';
            }

            // Set checkboxes
            const setCheckbox = (name, value) => {
                const el = editEventModal.querySelector(`[name="${name}"]`);
                if (el) el.checked = value;
            };

            setCheckbox('ShowAccommodationOptions', eventData.showAccommodationOptions);
            setCheckbox('AllowComments', eventData.allowComments);
            setCheckbox('AllowKids', eventData.allowKids);
        });

        // Sync EndDate min to StartDate value
        const startDateInput = editEventModal.querySelector('[name="StartDate"]');
        const endDateInput = editEventModal.querySelector('[name="EndDate"]');
        if (startDateInput && endDateInput) {
            startDateInput.addEventListener('change', function () {
                endDateInput.min = this.value;
                if (endDateInput.value && endDateInput.value < this.value) {
                    endDateInput.value = this.value;
                }
            });
        }

        const accommodationToggle = editEventModal.querySelector('[name="ShowAccommodationOptions"]');
        if (accommodationToggle) {
            accommodationToggle.addEventListener('change', function() {
                const wrapper = document.getElementById('accommodationCodeWrapper');
                if (wrapper) {
                    wrapper.style.display = this.checked ? 'block' : 'none';
                }
            });
        }
    }
})();
