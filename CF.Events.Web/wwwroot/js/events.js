(function() {
    const editEventModal = document.getElementById('editEventModal');
    if (editEventModal) {
        editEventModal.addEventListener('show.bs.modal', event => {
            const button = event.relatedTarget;
            if (!button || button.id === 'newEventBtn') {
                // Reset form for new event
                editEventModal.querySelector('form').reset();
                editEventModal.querySelector('[name="NewEvent.Id"]').value = 0;
                editEventModal.querySelector('.modal-title').textContent = 'Create New Event';
                editEventModal.querySelector('button[type="submit"]').textContent = 'Create Event';
                return;
            }

            // Populate form for edit
            const eventData = JSON.parse(button.getAttribute('data-bs-event'));

            const modalTitle = editEventModal.querySelector('.modal-title');
            const submitBtn = editEventModal.querySelector('button[type="submit"]');

            modalTitle.textContent = 'Edit Event';
            submitBtn.textContent = 'Save Changes';

            editEventModal.querySelector('[name="NewEvent.Id"]').value = eventData.id;
            editEventModal.querySelector('[name="NewEvent.Name"]').value = eventData.name;
            editEventModal.querySelector('[name="NewEvent.Date"]').value = eventData.date;
            editEventModal.querySelector('[name="NewEvent.Location"]').value = eventData.location;
            editEventModal.querySelector('[name="NewEvent.Description"]').value = eventData.description;

            // Set checkboxes
            const setCheckbox = (name, value) => {
                const el = editEventModal.querySelector(`[name="NewEvent.${name}"]`);
                if (el) el.checked = value;
            };

            setCheckbox('OfferDinner', eventData.offerDinner);
            setCheckbox('OfferLunch', eventData.offerLunch);
            setCheckbox('OfferBreakfast', eventData.offerBreakfast);
            setCheckbox('OfferBrunch', eventData.offerBrunch);
            setCheckbox('ShowAccommodationOptions', eventData.showAccommodationOptions);
            setCheckbox('AllowComments', eventData.allowComments);
            setCheckbox('AllowPartners', eventData.allowPartners);
            setCheckbox('AllowKids', eventData.allowKids);
        });
    }
})();
