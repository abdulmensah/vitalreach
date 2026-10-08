// A non-blocking native HTML dialog keeps SignalR heartbeats running while an admin decides.
window.adminConfirm = {
    ask(message) {
        const dialog = document.getElementById('admin-confirm-dialog');
        if (!dialog || dialog.open) return Promise.resolve(false);
        const description = dialog.querySelector('#admin-confirm-message');
        description.textContent = message;
        dialog.returnValue = 'cancel';
        return new Promise(resolve => {
            let finished = false;
            const observer = new MutationObserver(() => { if (!dialog.isConnected) finish(false); });
            const finish = confirmed => {
                if (finished) return;
                finished = true;
                observer.disconnect();
                dialog.removeEventListener('close', closed);
                dialog.removeEventListener('cancel', canceled);
                window.removeEventListener('pagehide', canceled);
                resolve(confirmed);
            };
            const closed = () => finish(dialog.returnValue === 'confirm');
            const canceled = () => { if (dialog.open) dialog.close('cancel'); finish(false); };
            dialog.addEventListener('close', closed);
            dialog.addEventListener('cancel', canceled);
            window.addEventListener('pagehide', canceled);
            observer.observe(document.body, { childList: true, subtree: true });
            try { dialog.showModal(); } catch { finish(false); }
        });
    }
};
