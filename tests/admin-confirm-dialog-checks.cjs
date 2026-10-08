const assert = require('node:assert/strict');
const vm = require('node:vm');
const fs = require('node:fs');
const window = new EventTarget();
const dialog = new EventTarget();
const description = { textContent: '' };
dialog.open = false; dialog.isConnected = true;
dialog.querySelector = () => description;
dialog.showModal = () => { dialog.open = true; };
dialog.close = value => { dialog.open = false; dialog.returnValue = value; dialog.dispatchEvent(new Event('close')); };
let observer;
vm.runInNewContext(fs.readFileSync('wwwroot/js/admin-confirm.js', 'utf8'), {
    window, document: { getElementById: () => dialog, body: {} },
    MutationObserver: class { constructor(callback) { observer = callback; } observe() {} disconnect() {} }
});
(async () => {
    let result = window.adminConfirm.ask('<img src=x onerror=bad()>');
    assert.equal(description.textContent, '<img src=x onerror=bad()>');
    assert.equal(await window.adminConfirm.ask('duplicate'), false);
    dialog.close('cancel'); assert.equal(await result, false);
    result = window.adminConfirm.ask('save'); dialog.close('confirm'); assert.equal(await result, true);
    result = window.adminConfirm.ask('escape'); dialog.dispatchEvent(new Event('cancel')); assert.equal(await result, false);
    result = window.adminConfirm.ask('navigate'); window.dispatchEvent(new Event('pagehide')); assert.equal(await result, false);
    assert.equal(dialog.open, false);
    result = window.adminConfirm.ask('removed'); dialog.isConnected = false; observer(); assert.equal(await result, false);
    console.log('PASS: dialog confirms, cancels, escapes, handles navigation/removal and rejects duplicates');
})();
