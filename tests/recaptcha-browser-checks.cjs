const assert = require('node:assert/strict');
const fs = require('node:fs');
const source = fs.readFileSync('wwwroot/js/recaptcha.js', 'utf8');
let checks = 0;
async function scenario(alternate, failFirst = false) {
    let loads = 0, calls = [];
    global.window = {};
    global.document = {
        createElement: () => ({ remove() {} }),
        head: { appendChild(script) {
            loads++;
            assert.ok(script.src.startsWith(alternate ? 'https://www.recaptcha.net/' : 'https://www.google.com/')); checks++;
            assert.ok(script.src.includes('render=site-key')); checks++;
            queueMicrotask(() => {
                if (failFirst && loads === 1) { script.onerror(); return; }
                window.grecaptcha = { ready: cb => cb(), execute: async (key, options) => { calls.push(options.action); return `token-${calls.length}`; } };
                script.onload();
            });
        } }
    };
    const module = await import('data:text/javascript;base64,' + Buffer.from(source + `\n// ${alternate}-${failFirst}`).toString('base64'));
    if (failFirst) { await assert.rejects(module.execute('site-key', 'contact', alternate)); checks++; }
    assert.equal(await module.execute('site-key', 'contact', alternate), 'token-1'); checks++;
    assert.equal(await module.execute('site-key', 'support_chat', alternate), 'token-2'); checks++;
    assert.deepEqual(calls, ['contact', 'support_chat']); checks++;
    assert.equal(loads, failFirst ? 2 : 1); checks++;
}
(async () => {
    await scenario(false);
    await scenario(true);
    await scenario(false, true);
    console.log(`PASS: ${checks} reCAPTCHA browser checks.`);
})().catch(error => { console.error(error); process.exitCode = 1; });
