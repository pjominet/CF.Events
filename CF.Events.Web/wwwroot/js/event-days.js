let eventId;

function initEventDays(id) {
    eventId = id;

    document.getElementById('generateDaysBtn').addEventListener('click', generateDays);
    document.getElementById('saveDayBtn').addEventListener('click', saveDay);
    document.getElementById('addDayBtn').addEventListener('click', () => {
        document.getElementById('dayModalTitle').textContent = 'Add Event Day';
        document.getElementById('editDayId').value = '';
        document.getElementById('dayDate').value = '';
        document.getElementById('dayName').value = '';
        document.getElementById('dayOffersFood').checked = true;
        document.getElementById('dayOffersAccommodation').checked = true;
    });

    document.querySelectorAll('.edit-day-btn').forEach(btn => {
        btn.addEventListener('click', () => {
            document.getElementById('dayModalTitle').textContent = 'Edit Event Day';
            document.getElementById('editDayId').value = btn.dataset.dayId;
            document.getElementById('dayDate').value = btn.dataset.dayDate;
            document.getElementById('dayName').value = btn.dataset.dayName;
            document.getElementById('dayOffersFood').checked = btn.dataset.dayFood === 'true';
            document.getElementById('dayOffersAccommodation').checked = btn.dataset.dayAccommodation === 'true';
            new bootstrap.Modal(document.getElementById('dayModal')).show();
        });
    });

    document.querySelectorAll('.delete-day-btn').forEach(btn => {
        btn.addEventListener('click', () => deleteDay(btn.dataset.dayId, btn.dataset.dayName));
    });
}

async function generateDays() {
    if (!confirm('Auto-generate days for all dates in the event range? Existing days will not be duplicated.'))
        return;

    try {
        const res = await fetch(`/events/${eventId}/days/generate`, { method: 'POST' });
        const data = await res.json();
        if (res.ok) {
            alert(data.message);
            location.reload();
        } else {
            alert(data || 'Failed to generate days');
        }
    } catch (e) {
        alert('Error generating days: ' + e.message);
    }
}

async function saveDay() {
    const dayId = document.getElementById('editDayId').value;
    const isEdit = !!dayId;
    const body = {
        date: document.getElementById('dayDate').value,
        name: document.getElementById('dayName').value,
        offersFood: document.getElementById('dayOffersFood').checked,
        offersAccommodation: document.getElementById('dayOffersAccommodation').checked
    };

    if (!body.date) {
        alert('Please select a date');
        return;
    }

    try {
        const url = isEdit ? `/events/${eventId}/days/${dayId}` : `/events/${eventId}/days`;
        const res = await fetch(url, {
            method: isEdit ? 'PUT' : 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(body)
        });

        if (res.ok) {
            location.reload();
        } else {
            const err = await res.text();
            alert(err || 'Failed to save day');
        }
    } catch (e) {
        alert('Error saving day: ' + e.message);
    }
}

async function deleteDay(dayId, dayName) {
    if (!confirm(`Delete "${dayName}"? This will also remove related food preferences and accommodations.`))
        return;

    try {
        const res = await fetch(`/events/${eventId}/days/${dayId}`, { method: 'DELETE' });
        if (res.ok) {
            location.reload();
        } else {
            alert('Failed to delete day');
        }
    } catch (e) {
        alert('Error deleting day: ' + e.message);
    }
}
