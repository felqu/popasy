window.forma = {
    downloadCsv(filename, content) {
        const blob = new Blob(['\ufeff' + content], { type: 'text/csv;charset=utf-8;' });
        const url = URL.createObjectURL(blob);
        const anchor = document.createElement('a');
        anchor.href = url;
        anchor.download = filename;
        document.body.appendChild(anchor);
        anchor.click();
        anchor.remove();
        setTimeout(() => URL.revokeObjectURL(url), 1000);
    }
};

// Keyboard access for global search and all dialogs, without a UI dependency.
(() => {
    let activeModal = null;
    let previousFocus = null;
    const focusable = modal => [...modal.querySelectorAll('a[href], button:not(:disabled), input:not(:disabled), select, textarea, [tabindex="0"]')]
        .filter(element => element.getClientRects().length);
    new MutationObserver(() => {
        const modal = document.querySelector('.modal');
        if (modal && modal !== activeModal) {
            previousFocus = document.activeElement;
            activeModal = modal;
            const input = modal.querySelector('input:not([type=checkbox]):not([type=radio]), select');
            (input || focusable(modal)[0])?.focus();
        } else if (!modal && activeModal) {
            activeModal = null;
            if (previousFocus?.isConnected) previousFocus.focus();
        }
    }).observe(document.body, { childList: true, subtree: true });

    document.addEventListener('keydown', event => {
        if (event.key === '/' && !event.ctrlKey && !event.metaKey && !event.altKey &&
            !event.target.closest('input, textarea, select, [contenteditable=true]') && !activeModal) {
            event.preventDefault();
            document.getElementById('workspace-search')?.click();
        }
        if (event.key === 'Escape') {
            if (activeModal) activeModal.querySelector('.modal-heading button')?.click();
            else document.querySelector('.sidebar-backdrop')?.click();
        }
        if (event.key === 'Tab' && activeModal) {
            const items = focusable(activeModal);
            if (!items.length) return;
            const index = items.indexOf(document.activeElement);
            if (event.shiftKey && index <= 0) { event.preventDefault(); items.at(-1).focus(); }
            else if (!event.shiftKey && (index === items.length - 1 || index < 0)) { event.preventDefault(); items[0].focus(); }
        }
    });
})();
