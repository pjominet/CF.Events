(function () {
    const bookContainer = document.getElementById('bookContainer');
    const openBtn = document.getElementById('cover-button');
    const openText = document.getElementById('cover-button-text');
    const closeBtn = document.getElementById('cancel-invite-btn');

    const openBook = () => {
        if (!bookContainer.classList.contains('open')) {
            bookContainer.classList.remove('closing');
            bookContainer.classList.add('open');
        }
    };

    const closeBook = () => {
        if (bookContainer.classList.contains('open')) {
            bookContainer.classList.remove('open');
            bookContainer.classList.add('closing');
        }
    }

    openBtn.addEventListener('click', (e) => {
        e.stopPropagation();
        if (autoOpenTimeout) {
            clearTimeout(autoOpenTimeout);
            autoOpenTimeout = null;
        }
        openBook();
    });

    closeBtn.addEventListener('click', (e) => {
        e.stopPropagation();
        closeBook();
    });

    const mobileQuery = window.matchMedia('(max-width: 768px)');
    function handleMobileChange(e) {
        if (e.matches) {
            openText.innerText = 'Tap to open'
        } else {
            openText.innerText = 'Click to open'
        }
    }

    handleMobileChange(mobileQuery);
    mobileQuery.addEventListener('change', handleMobileChange);

    // Auto-open after 15 seconds
    let autoOpenTimeout = setTimeout(() => {
        if (!bookContainer.classList.contains('open')) {
            openBook();
        }
        autoOpenTimeout = null;
    }, 15000);
})();
