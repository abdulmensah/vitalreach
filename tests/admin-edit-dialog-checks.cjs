const assert = require('node:assert/strict');
const fs = require('node:fs');
const vm = require('node:vm');
let observe, disconnected = false, focused = '', closeRequests = 0, error;
const trigger = { isConnected: true, focus: () => { focused = 'trigger'; } };
const document = { activeElement: trigger, body: { style: { overflow: 'auto' } } };
const dialog = new EventTarget();
dialog.isConnected = true;
dialog.showModal = () => { dialog.open = true; };
dialog.close = () => { dialog.open = false; };
dialog.querySelector = selector => selector === 'h2' ? { focus: () => { focused = 'title'; } } : error;
const context = { document, MutationObserver: class {
    constructor(callback) { observe = callback; }
    observe() {}
    disconnect() { disconnected = true; }
} };
vm.createContext(context);
vm.runInContext(fs.readFileSync('wwwroot/js/admin-edit-dialog.js', 'utf8').replaceAll('export function', 'function'), context);
const receiver = { invokeMethodAsync: async name => { assert.equal(name, 'RequestCloseAsync'); closeRequests++; } };
context.open(dialog, receiver);
assert.equal(dialog.open, true);
assert.equal(document.body.style.overflow, 'hidden');
assert.equal(focused, 'title');
const escape = new Event('cancel', { cancelable: true });
dialog.dispatchEvent(escape);
assert.equal(escape.defaultPrevented, true);
assert.equal(dialog.open, true, 'Escape must request confirmation instead of discarding the draft');
assert.equal(closeRequests, 1);
let scrolled = false;
error = { textContent: 'Please provide a title.', setAttribute() {}, focus() { focused = 'error'; }, scrollIntoView() { scrolled = true; } };
observe();
assert.equal(focused, 'error'); assert.equal(scrolled, true);
context.dispose(dialog);
assert.equal(dialog.open, false); assert.equal(document.body.style.overflow, 'auto');
assert.equal(focused, 'trigger'); assert.equal(disconnected, true);
context.dispose(dialog);
context.open(dialog, receiver);
dialog.isConnected = false; observe();
assert.equal(document.body.style.overflow, 'auto', 'Navigation restores scrolling');
console.log('PASS: editor opens, focuses, guards Escape, reveals errors, restores focus/scroll and cleans up on navigation');
