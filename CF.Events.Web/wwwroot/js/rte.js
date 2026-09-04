"use strict";

function initRichTextEditors() {
    const textareas = document.querySelectorAll('textarea.rte-editor');

    textareas.forEach(textarea => {
        // Skip if already initialized
        if (textarea.dataset.rteInitialized) return;

        // Hide the original textarea
        textarea.classList.add('visually-hidden');

        // Create a container for EditorJS
        const rteContainer = document.createElement('div');
        rteContainer.className = 'rte-container border rounded p-2 bg-white';
        if (textarea.classList.contains('rte-sm'))
            rteContainer.style.minHeight = '100px';
        else rteContainer.style.minHeight = '300px';

        textarea.parentNode.insertBefore(rteContainer, textarea.nextSibling);

        let initialData = {};
        if (textarea.value) {
            try {
                initialData = JSON.parse(textarea.value);
            } catch (e) {
                console.error('Failed to parse initial data for Editor.js', e);
            }
        }

        // Try to find an event ID from a hidden input or similar
        const objectIdInput = document.querySelector('[data-upload-id]');
        const objectId = objectIdInput ? objectIdInput.value : 0;
        const uploadSessionId = objectIdInput ? objectIdInput.getAttribute('data-upload-id') : null;
        const folderName = (objectId === '0' || objectId === 0 || !objectId) && uploadSessionId ? uploadSessionId : (objectId || '0');

        const editor = new EditorJS({
            holder: rteContainer,
            placeholder: textarea.placeholder || 'Let\'s write an awesome story!',
            data: initialData,
            tools: {
                header: {
                    class: Header,
                    inlineToolbar: true,
                    config: {
                        placeholder: 'Enter a header',
                        levels: [2, 3, 4],
                        defaultLevel: 3
                    }
                },
                list: {
                    class: EditorjsList,
                    inlineToolbar: true,
                    config: {
                        defaultStyle: 'unordered'
                    }
                },
                table: {
                    class: Table,
                    inlineToolbar: true,
                    config: {
                        rows: 2,
                        cols: 3,
                    },
                },
                image: {
                    class: ImageTool,
                    /*tunes: ['imageTunePlus'],*/
                    config: {
                        endpoints: {
                            byFile: `/file/upload-image/${folderName}`,
                        }
                    }
                },
                /*imageTunePlus: {
                    class: ImageTunePlus,
                },*/
                quote: {
                    class: Quote,
                    inlineToolbar: true,
                    config: {
                        quotePlaceholder: 'Enter a quote',
                        captionPlaceholder: 'Quote\'s author',
                    },
                },
                checklist: {
                    class: Checklist,
                    inlineToolbar: true,
                },
                delimiter: Delimiter,
                underline: Underline
            },
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

    // Also check for visible editors on load (in case tab was set by persistence)
    setTimeout(initRichTextEditors, 100);
});

