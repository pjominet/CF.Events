(function () {
    "use strict";

    function initTooltips() {
        document.querySelectorAll('[data-bs-toggle="tooltip"]').forEach(function (el) {
            // eslint-disable-next-line no-undef
            new bootstrap.Tooltip(el);
        });
    }

    function initPopovers() {
        document.querySelectorAll('[data-bs-toggle="popover"]').forEach(function (el) {
            // eslint-disable-next-line no-undef
            new bootstrap.Popover(el);
        });
    }

    // Re-open a Bootstrap modal automatically if the server flagged it (e.g. validation errors).
    function initAutoShowModals() {
        document.querySelectorAll('.modal[data-autoshow="true"]').forEach(function (el) {
            // eslint-disable-next-line no-undef
            const modal = bootstrap.Modal.getOrCreateInstance(el);
            modal.show();
        });
    }

    // Custom confirm modal wrapper
    window.customConfirm = function (message, options = {}) {
        return new Promise((resolve) => {
            const modalEl = document.getElementById('confirmModal');
            if (!modalEl) {
                console.warn('Confirm modal not found, falling back to window.confirm');
                resolve(window.confirm(message));
                return;
            }

            const messageEl = document.getElementById('confirmModalMessage');
            const confirmBtn = document.getElementById('confirmModalConfirmBtn');
            const cancelBtn = document.getElementById('confirmModalCancelBtn');
            const titleEl = document.getElementById('confirmModalLabel');

            if (messageEl) messageEl.textContent = message;
            if (options.title && titleEl) titleEl.textContent = options.title;
            if (options.confirmText && confirmBtn) confirmBtn.textContent = options.confirmText;
            if (options.cancelText && cancelBtn) cancelBtn.textContent = options.cancelText;

            if (confirmBtn) {
                confirmBtn.className = 'btn ' + (options.confirmClass || 'btn-danger');
            }

            // eslint-disable-next-line no-undef
            const modal = bootstrap.Modal.getOrCreateInstance(modalEl);

            const onConfirm = () => {
                cleanup();
                resolve(true);
                modal.hide();
            };

            const onCancel = () => {
                cleanup();
                resolve(false);
                modal.hide();
            };

            const cleanup = () => {
                confirmBtn.removeEventListener('click', onConfirm);
                cancelBtn.removeEventListener('click', onCancel);
            };

            confirmBtn.addEventListener('click', onConfirm);
            cancelBtn.addEventListener('click', onCancel);

            modal.show();
        });
    }

    // Lightweight confirm wrapper for elements with data-confirm="message".
    function initConfirms() {
        document.querySelectorAll('[data-confirm]').forEach(function (el) {
            const tagName = el.tagName.toLowerCase();
            const eventName = (tagName === 'form') ? 'submit' : 'click';

            el.addEventListener(eventName, async function (e) {
                if (el.dataset.confirming) return;

                e.preventDefault();
                e.stopImmediatePropagation();

                const message = el.getAttribute("data-confirm");
                const confirmed = await window.customConfirm(message);

                if (confirmed) {
                    el.dataset.confirming = "true";
                    if (tagName === 'form') {
                        el.submit();
                    } else {
                        el.click();
                    }
                    delete el.dataset.confirming;
                }
            });
        });
    }

    function initMultiSelects() {
        document.querySelectorAll("select.tom-select").forEach(function (el) {
            if (el.disabled) return;

            let settings = {
                placeholder: el.getAttribute("data-placeholder") || "Select...",
                hidePlaceholder: true,
                allowEmptyOption: false,
                maxOptions: 20,
                dropdownParent: "body"
            }

            if (!!el.hasAttribute("multiple")) {
                settings.plugins = ['remove_button'];
                settings.maxItems = null;
                settings.clearAfterSelect = false;
                settings.closeAfterSelect = false;
            } else {
                settings.maxItems = 1;
                settings.clearAfterSelect = false;
                settings.closeAfterSelect = true;
                settings.onItemAdd = function () {
                    this.blur();
                };
            }

            if (el.classList.contains("tom-select-html")) {
                settings.render = {
                    option: function (data, escape) {
                        return '<div>' + (data.html || escape(data.text)) + '</div>';
                    },
                    item: function (data, escape) {
                        return '<div>' + (data.html || escape(data.text)) + '</div>';
                    }
                };
            }

            new TomSelect(el, settings);
        });
    }

    function initTagSelects() {
        document.querySelectorAll("input.tag-select").forEach(function (el) {
            if (el.disabled) return;

            new TomSelect(el, {
                create: true,
                persist: false,
                hidePlaceholder: true,
                allowEmptyOption: false,
                plugins: ['restore_on_backspace'],
                delimiter: ',',
                placeholder: el.getAttribute("data-placeholder") || "Add tag...",
            });
        });
    }

    // Copy-to-clipboard handler
    window.copyToClipboardAndShowFeedback = function (elementId, button, duration = 750) {
        const source = document.getElementById(elementId);
        if (!source) {
            console.error(`Element with ID '${elementId}' not found`);
            return;
        }

        const textToCopy = source.value || source.textContent || '';
        if (!textToCopy.trim()) {
            console.error('No text to copy');
            return;
        }

        const originalText = button.textContent;
        button.textContent = 'Copied!';

        navigator.clipboard.writeText(textToCopy)
            .catch(err => {
                console.error('Failed to copy:', err);
                button.textContent = originalText;
            });

        setTimeout(() => {
            button.textContent = originalText;
        }, duration);
    }

    window.copyToClipboardAndShowFeedback = function (element, duration = 750) {
        const originalText = element.textContent;
        element.textContent = 'Copied!';

        navigator.clipboard.writeText(originalText)
            .catch(err => {
                console.error('Failed to copy:', err);
                element.textContent = originalText;
            });

        setTimeout(() => {
            element.textContent = originalText;
        }, duration);
    }

    // Show loading overlay
    window.showLoadingOverlay = function () {
        const overlay = document.getElementById('globalLoadingOverlay');
        if (overlay) {
            overlay.classList.add('active');
        }
    }

    // Hide loading overlay
    function hideLoadingOverlay() {
        const overlay = document.getElementById('globalLoadingOverlay');
        if (overlay) {
            overlay.classList.remove('active');
        }
    }

    // Hide on page load/complete
    window.addEventListener('load', hideLoadingOverlay);

    document.addEventListener("DOMContentLoaded", function () {
        initTooltips();
        initPopovers();
        initAutoShowModals();
        initConfirms();
        initMultiSelects();
        initTagSelects();
        setTimeout(hideLoadingOverlay, 100);
    });
})();
