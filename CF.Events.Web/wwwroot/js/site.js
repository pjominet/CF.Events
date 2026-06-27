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

    // Lightweight confirm wrapper for elements with data-confirm="message".
    function initConfirms() {
        document.querySelectorAll('[data-confirm]').forEach(function (el) {
            el.addEventListener("submit", function (e) {
                if (!window.confirm(el.getAttribute("data-confirm"))) {
                    e.preventDefault();
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

            if (!!el.getAttribute("multiple")) {
                settings.plugins = ['remove_button'];
                settings.maxItems = null;
                settings.clearAfterSelect = false;
                settings.closeAfterSelect = false;
            } else {
                settings.maxItems = 1;
                settings.clearAfterSelect = true;
                settings.closeAfterSelect = true;
            }

            new TomSelect(el, settings);
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

    // Copy-to-clipboard helper for badges
    window.copyToClipboard = function (text, element) {
        if (!element) return;

        navigator.clipboard.writeText(text).then(function () {
            const codeElement = element.querySelector("code");
            const originalText = codeElement ? codeElement.innerText : null;

            if (codeElement) {
                codeElement.innerText = "copied to clipboard";
                codeElement.classList.add("copied-text");
            }

            element.classList.add("clicked");
            setTimeout(function () {
                element.classList.remove("clicked");
                if (codeElement && originalText) {
                    codeElement.innerText = originalText;
                    codeElement.classList.remove("copied-text");
                }
            }, 700);
        }).catch(function (err) {
            console.error("Failed to copy:", err);
        });
    };

    window.fetchNewTempPassword = async function () {
        const response = await fetch("/api/generate-password", {
            headers: {"Accept": "text/plain"}
        });
        if (!response.ok) {
            throw new Error("Failed to generate password: " + response.status);
        }
        return (await response.text()).trim();
    };

    // Fill an input (by element id) with a freshly generated password.
    window.fillTempPassword = function (inputId) {
        window.fetchNewTempPassword().then(function (password) {
            const input = document.getElementById(inputId);
            if (input) input.value = password;
        }).catch(function (err) {
            console.error(err);
        });
    };

    document.addEventListener("DOMContentLoaded", function () {
        initTooltips();
        initPopovers();
        initAutoShowModals();
        initConfirms();
        initMultiSelects();
    });
})();
