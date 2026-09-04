(function () {
    "use strict";

    window.autoRedirect = function (seconds) {
        const countdownElement = document.querySelector('span.countdown');
        const interval = setInterval(function () {
            seconds--;
            countdownElement.textContent = seconds;
            if (seconds <= 0) {
                clearInterval(interval);
                window.location.href = '/Account/Login';
            }
        }, 1000);
    }

    function initTooltips(container = document) {
        container.querySelectorAll('[data-bs-toggle="tooltip"]').forEach(function (el) {
            // eslint-disable-next-line no-undef
            new bootstrap.Tooltip(el);
        });
    }

    function initPopovers(container = document) {
        container.querySelectorAll('[data-bs-toggle="popover"]').forEach(function (el) {
            // eslint-disable-next-line no-undef
            new bootstrap.Popover(el);
        });
    }

    function initScrollPersistence() {
        const storageKey = 'scrollPositions-' + window.location.pathname;

        // Restore positions
        const savedPositions = JSON.parse(sessionStorage.getItem(storageKey) || '{}');
        Object.keys(savedPositions).forEach(id => {
            const el = document.getElementById(id);
            if (el) {
                el.scrollTop = savedPositions[id].top;
                el.scrollLeft = savedPositions[id].left;
            }
        });

        // Save positions before unload
        window.addEventListener('beforeunload', () => {
            const positions = {};
            document.querySelectorAll('[data-scroll-persist]').forEach(el => {
                if (el.id) {
                    positions[el.id] = {
                        top: el.scrollTop,
                        left: el.scrollLeft
                    };
                }
            });
            sessionStorage.setItem(storageKey, JSON.stringify(positions));
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
                modalEl.addEventListener('hidden.bs.modal', () => resolve(true), { once: true });
                modal.hide();
            };

            const onCancel = () => {
                cleanup();
                modalEl.addEventListener('hidden.bs.modal', () => resolve(false), { once: true });
                modal.hide();
            };

            const cleanup = () => {
                confirmBtn.removeEventListener('click', onConfirm);
                cancelBtn.removeEventListener('click', onCancel);
                modalEl.removeEventListener('hidden.bs.modal', onHidden);
            };

            const onHidden = () => {
                cleanup();
            };

            modalEl.addEventListener('hidden.bs.modal', onHidden, { once: true });
            confirmBtn.addEventListener('click', onConfirm);
            cancelBtn.addEventListener('click', onCancel);

            // If the modal is currently being hidden, wait until it's hidden before showing it again
            if (modalEl.classList.contains('collapsing') || modalEl.classList.contains('showing')) {
                 modalEl.addEventListener('hidden.bs.modal', () => modal.show(), { once: true });
            } else {
                modal.show();
            }
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

    function initMultiSelects(container = document) {
        container.querySelectorAll("select.tom-select").forEach(function (select) {
            if (select.tomselect) return;

            // Determine the dropdown parent.
            const modal = select.closest('.modal');
            const dropdownParent = modal ? null : 'body';

            let settings = {
                placeholder: select.getAttribute("data-placeholder") || "Select...",
                hidePlaceholder: true,
                allowEmptyOption: false,
                maxOptions: 20,
                dropdownParent: dropdownParent
            }

            if (!!select.hasAttribute("multiple")) {
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

            const ts = new TomSelect(select, settings);
            if (select.disabled) {
                ts.disable();
            }
        });
    }
    window.initMultiSelects = initMultiSelects;

    function initTagSelects(container = document) {
        container.querySelectorAll("input.tag-select").forEach(function (el) {
            if (el.tomselect) return;
            if (el.disabled) return;

            const modal = el.closest('.modal');
            const dropdownParent = modal ? null : 'body';

            new TomSelect(el, {
                create: true,
                persist: false,
                hidePlaceholder: true,
                allowEmptyOption: false,
                plugins: ['restore_on_backspace'],
                delimiter: ',',
                placeholder: el.getAttribute("data-placeholder") || "Add tag...",
                dropdownParent: dropdownParent
            });
        });
    }

    function initSidebar() {
        const sidebar = document.getElementById('sidebar');
        const overlay = document.getElementById('sidebarOverlay');
        const toggleBtn = document.getElementById('sidebarToggle');
        const closeBtn = document.getElementById('sidebarClose');

        if (!sidebar || !toggleBtn) return;

        const openSidebar = () => {
            sidebar.classList.add('active');
            overlay?.classList.add('active');
            document.body.style.overflow = 'hidden';
        };

        const closeSidebar = () => {
            sidebar.classList.remove('active');
            overlay?.classList.remove('active');
            document.body.style.overflow = '';
        };

        toggleBtn.addEventListener('click', openSidebar);
        closeBtn?.addEventListener('click', closeSidebar);
        overlay?.addEventListener('click', closeSidebar);

        // Close on escape key
        document.addEventListener('keydown', (e) => {
            if (e.key === 'Escape' && sidebar.classList.contains('active')) {
                closeSidebar();
            }
        });
    }

    function applyDynamicTableStyles(container = document) {
        container.querySelectorAll('td').forEach(td => {
            if (td.textContent.trim().toLowerCase() === 'n/a' || td.textContent.trim().toLowerCase() === '-') {
                td.classList.add('text-muted');
            }
        });

        container.querySelectorAll('td').forEach(td => {
            if (td.textContent.trim().toLowerCase().endsWith('@no-send.tech') || td.textContent.trim().toLowerCase() === 'undefined') {
                td.classList.add('text-danger');
            }
        });
    }
    window.applyDynamicTableStyles = applyDynamicTableStyles;

    function initCharacterCounters(container = document) {
        container.querySelectorAll('[data-max-length]').forEach(function (el) {
            const maxLength = parseInt(el.getAttribute('data-max-length'));
            const counterId = el.getAttribute('data-counter-id');
            const counterEl = document.getElementById(counterId);

            if (!counterEl) return;

            const updateCounter = () => {
                const currentLength = el.value.length;
                counterEl.textContent = `${currentLength}/${maxLength}`;

                if (currentLength > maxLength) {
                    counterEl.classList.add('text-danger');
                } else {
                    counterEl.classList.remove('text-danger');
                }
            };

            ['input', 'keyup', 'paste', 'change'].forEach(event => {
                el.addEventListener(event, updateCounter);
            });
            updateCounter(); // Initial call
        });
    }
    window.initCharacterCounters = initCharacterCounters;

    window.copyToClipboardAndShowFeedback = function (elementOrId, buttonOrDuration, duration = 750) {
        let textToCopy = '';
        let button = null;
        let finalDuration = duration;

        if (typeof elementOrId === 'string') {
            const source = document.getElementById(elementOrId);
            if (!source) {
                console.error(`Element with ID '${elementOrId}' not found`);
                return;
            }
            textToCopy = source.value || source.textContent || '';
            button = buttonOrDuration;
            finalDuration = duration;
        } else {
            textToCopy = elementOrId.textContent;
            button = elementOrId;
            finalDuration = typeof buttonOrDuration === 'number' ? buttonOrDuration : 750;
        }

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
        }, finalDuration);
    }

    // Show loading overlay
    window.showLoadingOverlay = function () {
        const overlay = document.getElementById('globalLoadingOverlay');
        if (overlay) {
            overlay.classList.add('active');
        }
    }

    // Hide loading overlay
    window.hideLoadingOverlay = function() {
        const overlay = document.getElementById('globalLoadingOverlay');
        if (overlay) {
            overlay.classList.remove('active');
        }
    }

    // Hide on page load/complete
    window.addEventListener('load', hideLoadingOverlay);

    function initTabPersistence() {
        const tabElements = document.querySelectorAll('button[data-bs-toggle="tab"]');
        const urlParams = new URLSearchParams(window.location.search);
        const activeTabId = urlParams.get('tab');

        // Function to update the URL with the active tab ID
        const updateUrlWithTab = (tabId) => {
            const url = new URL(window.location);
            url.searchParams.set('tab', tabId);
            window.history.replaceState({}, '', url);
        };

        // If there is a tab ID in the URL, try to activate it
        if (activeTabId) {
            const tabToActivate = document.getElementById(activeTabId + '-tab');
            if (tabToActivate) {
                // Remove 'active' class from all tabs and panes
                tabElements.forEach(tab => {
                    tab.classList.remove('active');
                    tab.setAttribute('aria-selected', 'false');
                });
                document.querySelectorAll('.tab-pane').forEach(pane => {
                    pane.classList.remove('show', 'active');
                });

                // Activate the target tab and pane
                tabToActivate.classList.add('active');
                tabToActivate.setAttribute('aria-selected', 'true');
                const targetPaneId = tabToActivate.getAttribute('data-bs-target');
                const targetPane = document.querySelector(targetPaneId);
                if (targetPane) {
                    targetPane.classList.add('show', 'active');
                }
            }
        }

        // Listen for tab changes and update the URL
        tabElements.forEach(tab => {
            tab.addEventListener('shown.bs.tab', (event) => {
                const targetId = event.target.id.replace('-tab', '');
                updateUrlWithTab(targetId);
            });
        });
    }

    document.addEventListener("DOMContentLoaded", function () {
        initTooltips();
        initPopovers();
        initAutoShowModals();
        initConfirms();
        initMultiSelects();
        initTagSelects();
        initTabPersistence();
        initScrollPersistence();
        initSidebar();
        initCharacterCounters();
        applyDynamicTableStyles();
        setTimeout(hideLoadingOverlay, 100);
    });
})();
