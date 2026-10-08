const editors = new WeakMap();

export function open(dialog, receiver) {
    if (editors.has(dialog)) return;
    const previousFocus = document.activeElement;
    const previousOverflow = document.body.style.overflow;
    let lastError = '';
    const cancel = event => {
        event.preventDefault();
        receiver.invokeMethodAsync('RequestCloseAsync').catch(() => {});
    };
    const observer = new MutationObserver(() => {
        if (!dialog.isConnected) { dispose(dialog); return; }
        const error = dialog.querySelector('.admin-message.error, .validation-summary-errors');
        const message = error?.textContent.trim() || '';
        if (message && message !== lastError) {
            error.setAttribute('tabindex', '-1');
            error.focus({ preventScroll: true });
            error.scrollIntoView({ block: 'nearest' });
        }
        lastError = message;
    });
    editors.set(dialog, { previousFocus, previousOverflow, cancel, observer });
    dialog.addEventListener('cancel', cancel);
    try {
        dialog.showModal();
        document.body.style.overflow = 'hidden';
        dialog.querySelector('h2').focus({ preventScroll: true });
        observer.observe(document.body, { childList: true, subtree: true, characterData: true });
    } catch (error) { dispose(dialog); throw error; }
}

export function dispose(dialog) {
    const state = editors.get(dialog);
    if (!state) return;
    state.observer.disconnect();
    dialog.removeEventListener('cancel', state.cancel);
    if (dialog.open) dialog.close();
    document.body.style.overflow = state.previousOverflow;
    if (state.previousFocus?.isConnected) state.previousFocus.focus({ preventScroll: true });
    editors.delete(dialog);
}
