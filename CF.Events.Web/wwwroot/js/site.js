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

    // Auto-dismiss flash toasts rendered by the layout.
    function initToasts() {
        document.querySelectorAll('#toastContainer .toast[data-autodismiss="true"]').forEach(function (el) {
            setTimeout(function () {
                el.style.transition = "opacity 0.3s ease";
                el.style.opacity = "0";
                setTimeout(function () { el.remove(); }, 300);
            }, 5000);
        });
    }

    // Show a toast programmatically.
    window.showToast = function (message, type) {
        const container = document.getElementById("toastContainer");
        if (!container) return;

        let toast = document.createElement("div");
        toast.className = `toast ${type || "info"}`;

        let msg = document.createElement("span");
        msg.className = "toast-message";
        msg.textContent = message;

        let close = document.createElement("button");
        close.type = "button";
        close.className = "toast-close";
        close.innerHTML = "&times;";
        close.addEventListener("click", function () { toast.remove(); });
        toast.appendChild(msg);
        toast.appendChild(close);
        container.appendChild(toast);

        setTimeout(function () {
            toast.style.transition = "opacity 0.3s ease";
            toast.style.opacity = "0";
            setTimeout(function () { toast.remove(); }, 300);
        }, 5000);
    };

    // Re-open a Bootstrap modal automatically if the server flagged it (e.g. validation errors).
    function initAutoShowModals() {
        document.querySelectorAll('.modal[data-autoshow="true"]').forEach(function (el) {
            // eslint-disable-next-line no-undef
            const modal = bootstrap.Modal.getOrCreateInstance(el);
            modal.show();
        });
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
            headers: { "Accept": "text/plain" }
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
            if (window.showToast) window.showToast("Could not generate a password", "error");
        });
    };

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

    document.addEventListener("DOMContentLoaded", function () {
        initTooltips();
        initPopovers();
        initToasts();
        initAutoShowModals();
        initConfirms();
    });
})();
