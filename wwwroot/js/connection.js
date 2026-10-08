(() => {
    const panel = document.getElementById('connection-status');
    const message = document.getElementById('connection-message');
    const retry = document.getElementById('connection-retry');
    const reload = document.getElementById('connection-reload');
    let disconnected = false;
    let running = false;
    let rejected = false;
    let generation = 0;

    async function reconnect() {
        if (!disconnected || running || rejected) return;
        running = true;
        retry.disabled = true;
        const current = generation;
        try {
            for (let attempt = 0; attempt < 30 && disconnected && current === generation; attempt++) {
                message.textContent = `Reconnecting… attempt ${attempt + 1} of 30.`;
                try {
                    const connected = await Blazor.reconnect();
                    if (current !== generation || !disconnected) return;
                    if (connected) {
                        onConnectionUp();
                        return;
                    }
                    rejected = true;
                    message.textContent = 'The previous session has expired or the application restarted. Reload when you are ready; unsaved entries will be lost.';
                    return;
                } catch {
                    // A network interruption can recover without replacing the form.
                }
                await new Promise(resolve => setTimeout(resolve, Math.min(2000 + attempt * 1000, 10000)));
            }
            if (disconnected && current === generation)
                message.textContent = 'Still disconnected. Check your connection and try reconnecting. Your form has not been reloaded.';
        } finally {
            if (current === generation) {
                running = false;
                retry.disabled = rejected;
            }
        }
    }

    function onConnectionUp() {
        generation++;
        disconnected = false;
        running = false;
        rejected = false;
        retry.disabled = false;
        panel.hidden = true;
    }
    retry.addEventListener('click', reconnect);
    reload.addEventListener('click', () => {
        if (window.confirm('Reload this page? Any entries that have not been submitted or saved will be lost.')) window.location.reload();
    });
    window.addEventListener('online', reconnect);
    window.addEventListener('focus', reconnect);
    document.addEventListener('visibilitychange', () => { if (!document.hidden) reconnect(); });
    Blazor.start({ circuit: {
        configureSignalR: builder => builder.withServerTimeout(60000).withKeepAliveInterval(15000),
        reconnectionHandler: {
            onConnectionDown: () => { disconnected = true; panel.hidden = false; reconnect(); },
            onConnectionUp
        }
    }}).catch(() => {
        panel.hidden = false;
        retry.disabled = true;
        message.textContent = 'The interactive page could not start. Check your connection, then reload when ready.';
    });
})();
