let eventId;

function initCustomQuestions(id) {
    eventId = id;

    const typeSelect = document.getElementById('qType');
    typeSelect.addEventListener('change', toggleOptionsGroup);

    document.getElementById('saveQuestionBtn').addEventListener('click', saveQuestion);

    document.getElementById('addQuestionBtn').addEventListener('click', () => {
        document.getElementById('questionModalTitle').textContent = 'Add Custom Question';
        document.getElementById('editQuestionId').value = '';
        document.getElementById('qLabel').value = '';
        document.getElementById('qHelpText').value = '';
        document.getElementById('qType').value = '0';
        document.getElementById('qOptions').value = '';
        document.getElementById('qRequired').checked = false;
        document.getElementById('qSortOrder').value = '0';
        document.getElementById('qStepGroup').value = 'Extras';
        document.getElementById('qStepOrder').value = '0';
        document.getElementById('qShowIf').value = '';
        toggleOptionsGroup();
    });

    document.querySelectorAll('.edit-question-btn').forEach(btn => {
        btn.addEventListener('click', () => {
            document.getElementById('questionModalTitle').textContent = 'Edit Custom Question';
            document.getElementById('editQuestionId').value = btn.dataset.qId;
            document.getElementById('qLabel').value = btn.dataset.qLabel;
            document.getElementById('qHelpText').value = btn.dataset.qHelptext || '';
            document.getElementById('qType').value = btn.dataset.qType;
            document.getElementById('qOptions').value = btn.dataset.qOptions || '';
            document.getElementById('qRequired').checked = btn.dataset.qRequired === 'true';
            document.getElementById('qSortOrder').value = btn.dataset.qSortorder;
            document.getElementById('qStepGroup').value = btn.dataset.qStepgroup;
            document.getElementById('qStepOrder').value = btn.dataset.qSteporder;
            document.getElementById('qShowIf').value = btn.dataset.qShowif || '';
            toggleOptionsGroup();
            new bootstrap.Modal(document.getElementById('questionModal')).show();
        });
    });

    document.querySelectorAll('.delete-question-btn').forEach(btn => {
        btn.addEventListener('click', () => deleteQuestion(btn.dataset.qId, btn.dataset.qLabel));
    });
}

function toggleOptionsGroup() {
    const type = parseInt(document.getElementById('qType').value);
    // SingleChoice = 3, MultiChoice = 4
    document.getElementById('optionsGroup').style.display = (type === 3 || type === 4) ? 'block' : 'none';
}

async function saveQuestion() {
    const questionId = document.getElementById('editQuestionId').value;
    const isEdit = !!questionId;
    const optionsText = document.getElementById('qOptions').value.trim();
    const options = optionsText ? optionsText.split('\n').map(o => o.trim()).filter(o => o) : null;

    const body = {
        label: document.getElementById('qLabel').value,
        helpText: document.getElementById('qHelpText').value || null,
        type: parseInt(document.getElementById('qType').value),
        options: options,
        isRequired: document.getElementById('qRequired').checked,
        sortOrder: parseInt(document.getElementById('qSortOrder').value) || 0,
        stepGroup: document.getElementById('qStepGroup').value,
        stepOrder: parseInt(document.getElementById('qStepOrder').value) || 0,
        showIf: document.getElementById('qShowIf').value || null
    };

    if (!body.label) {
        alert('Please enter a label');
        return;
    }

    try {
        const url = isEdit
            ? `/events/${eventId}/custom-questions/${questionId}`
            : `/events/${eventId}/custom-questions`;
        const res = await fetch(url, {
            method: isEdit ? 'PUT' : 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(body)
        });

        if (res.ok) {
            location.reload();
        } else {
            const err = await res.text();
            alert(err || 'Failed to save question');
        }
    } catch (e) {
        alert('Error saving question: ' + e.message);
    }
}

async function deleteQuestion(questionId, label) {
    if (!confirm(`Delete question "${label}"? This will also remove all related answers.`))
        return;

    try {
        const res = await fetch(`/events/${eventId}/custom-questions/${questionId}`, { method: 'DELETE' });
        if (res.ok) {
            location.reload();
        } else {
            alert('Failed to delete question');
        }
    } catch (e) {
        alert('Error deleting question: ' + e.message);
    }
}
