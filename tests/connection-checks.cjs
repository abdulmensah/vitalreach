const assert = require('node:assert/strict');
const fs = require('node:fs');
const vm = require('node:vm');
const source = fs.readFileSync('wwwroot/js/connection.js', 'utf8');
function harness(reconnect) {
    let options, reloads = 0;
    const elements = Object.fromEntries(['connection-status', 'connection-message', 'connection-retry', 'connection-reload'].map(id => [id, { hidden: true, disabled: false, addEventListener(name, fn) { this[name] = fn; } }]));
    const events = {};
    const context = { document: { getElementById: id => elements[id], addEventListener: (name, fn) => events[name] = fn, hidden: false },
        window: { addEventListener: (name, fn) => events[name] = fn, confirm: () => false, location: { reload: () => reloads++ } },
        Blazor: { start: value => { options = value; return Promise.resolve(); }, reconnect },
        setTimeout: callback => { queueMicrotask(callback); } };
    vm.runInNewContext(source, context);
    return { elements, events, down: () => options.circuit.reconnectionHandler.onConnectionDown(), up: () => options.circuit.reconnectionHandler.onConnectionUp(), reloads: () => reloads };
}
const flush = () => new Promise(resolve => setImmediate(resolve));
(async () => {
    let calls = 0;
    let h = harness(async () => { calls++; return true; });
    h.down(); await flush();
    assert.equal(h.elements['connection-status'].hidden, true);
    assert.equal(h.reloads(), 0);
    h = harness(async () => false); h.down(); await flush();
    assert.match(h.elements['connection-message'].textContent, /expired/);
    assert.equal(h.reloads(), 0);
    assert.equal(h.elements['connection-retry'].disabled, true);
    h.elements['connection-reload'].click(); assert.equal(h.reloads(), 0);
    calls = 0;
    h = harness(async () => { calls++; throw new Error('offline'); });
    h.down(); h.down(); await flush();
    assert.equal(calls, 30, 'one bounded retry loop');
    assert.equal(h.reloads(), 0);
    assert.equal(h.elements['connection-retry'].disabled, false);
    h.events.online(); await flush(); assert.equal(calls, 60, 'online event retries');
    let resolve;
    h = harness(() => new Promise(r => resolve = r)); h.down(); h.up(); resolve(false); await flush();
    assert.equal(h.elements['connection-status'].hidden, true, 'late failed attempt cannot undo recovery');
    console.log('PASS: reconnect success, rejected session, offline retries, manual reload cancellation, online recovery and race checks');
})();
