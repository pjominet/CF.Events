function initRichTextEditors() {
    const textareas = document.querySelectorAll('textarea.rte-editor');

    textareas.forEach(textarea => {
        // Skip if already initialized
        if (textarea.dataset.rteInitialized) return;

        // Hide the original textarea
        textarea.style.display = 'none';

        // Create a container for EditorJS
        const container = document.createElement('div');
        container.className = 'rte-container border rounded p-2 bg-light';
        container.style.minHeight = '200px';
        textarea.parentNode.insertBefore(container, textarea.nextSibling);

        let initialData = {};
        if (textarea.value) {
            try {
                initialData = JSON.parse(textarea.value);
            } catch (e) {
                console.error('Failed to parse initial data for Editor.js', e);
                // If it's not JSON, we might want to wrap it in a paragraph block if we want to support legacy text
                // but for now, we assume it's JSON as produced by Editor.js
            }
        }

        const editor = new EditorJS({
            holder: container,
            placeholder: textarea.placeholder || 'Let\'s write an awesome story!',
            data: initialData,
            onChange: (api, event) => {
                editor.save().then((outputData) => {
                    textarea.value = JSON.stringify(outputData);
                    // Trigger change event on textarea so other scripts know it changed
                    textarea.dispatchEvent(new Event('change', { bubbles: true }));
                }).catch((error) => {
                    console.error('Saving failed: ', error);
                });
            }
        });

        textarea.dataset.rteInitialized = 'true';
    });
}

document.addEventListener('DOMContentLoaded', () => {
    initRichTextEditors();

    // Re-initialize or refresh editors when tabs are shown, in case they were hidden during first init
    const tabs = document.querySelectorAll('button[data-bs-toggle="tab"]');
    tabs.forEach(tab => {
        tab.addEventListener('shown.bs.tab', () => {
            initRichTextEditors();
        });
    });
});
