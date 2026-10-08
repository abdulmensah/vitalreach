let ready;
function load(siteKey, useRecaptchaNet) {
    if (ready) return ready;
    ready = new Promise((resolve, reject) => {
        const script = document.createElement('script');
        const fail = () => { clearTimeout(timer); script.remove(); ready = null; reject(new Error('Verification unavailable')); };
        const timer = setTimeout(fail, 15000);
        const authority = useRecaptchaNet ? 'https://www.recaptcha.net' : 'https://www.google.com';
        script.src = `${authority}/recaptcha/api.js?render=${encodeURIComponent(siteKey)}`;
        script.async = true;
        script.onload = () => {
            if (!window.grecaptcha?.ready) { fail(); return; }
            window.grecaptcha.ready(() => { clearTimeout(timer); resolve(); });
        };
        script.onerror = fail;
        document.head.appendChild(script);
    });
    return ready;
}

export async function execute(siteKey, action, useRecaptchaNet) {
    await load(siteKey, useRecaptchaNet);
    return await new Promise((resolve, reject) => {
        const timer = setTimeout(() => reject(new Error('Verification timed out')), 15000);
        Promise.resolve().then(() => window.grecaptcha.execute(siteKey, { action }))
            .then(resolve, reject).finally(() => clearTimeout(timer));
    });
}
