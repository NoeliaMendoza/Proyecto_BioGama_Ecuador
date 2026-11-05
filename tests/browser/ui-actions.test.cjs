const { test } = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const vm = require('node:vm');
const { JSDOM } = require('jsdom');
const sourcePath = path.resolve('wwwroot/js/ui-actions.js');

function setup(t, html) {
    const dom = new JSDOM(html);
    t.after(() => dom.window.close());
    const calls = [];
    const actions = {
        print: () => calls.push('print'),
        location: { assign: url => calls.push(url) },
        generarAnalisis: type => calls.push(type),
        copiarRespuesta: () => calls.push('copy'),
        confirm: () => true
    };
    vm.runInNewContext(fs.readFileSync(sourcePath, 'utf8'), { document: dom.window.document, window: actions }, { filename: sourcePath });
    return { dom, document: dom.window.document, calls, actions };
}

test('failed course images reveal the placeholder and recover when loaded without removing their element', t => {
    const { dom, document } = setup(t, '<div><span class="course-card-image-placeholder">Curso</span><img data-course-image src="https://example.com/missing.jpg"></div><img id="unrelated" src="https://example.com/logo.png">');
    const image = document.querySelector('[data-course-image]');
    image.dispatchEvent(new dom.window.Event('error'));
    document.querySelector('#unrelated').dispatchEvent(new dom.window.Event('error'));
    assert.equal(document.querySelector('[data-course-image]'), image);
    assert.equal(image.hidden, true);
    assert.ok(document.querySelector('.course-card-image-placeholder'));
    assert.ok(document.querySelector('#unrelated'));
    image.dispatchEvent(new dom.window.Event('load'));
    assert.equal(image.hidden, false);
});

test('an image that already failed before initialization also falls back', t => {
    const { document } = setup(t, '<div><span class="course-card-image-placeholder">Curso</span><img data-course-image></div>');
    assert.equal(document.querySelector('[data-course-image]').hidden, true);
    assert.ok(document.querySelector('.course-card-image-placeholder'));
});

test('quantity buttons preserve the enclosing form and step without inline code', t => {
    const { document } = setup(t, '<form id="cart"><input type="number" name="quantity" min="1" max="4" value="2"><button type="button" data-quantity-step="down">-</button><button type="button" data-quantity-step="up">+</button></form>');
    let submits = 0;
    document.querySelector('form').requestSubmit = () => submits++;
    document.querySelector('[data-quantity-step="down"]').click();
    assert.equal(document.querySelector('input').value, '1');
    document.querySelector('[data-quantity-step="up"]').click();
    assert.equal(document.querySelector('input').value, '2');
    assert.equal(submits, 2);
});

test('print, orders, AI and copy buttons invoke their explicit actions', t => {
    const { document, calls } = setup(t, '<button data-print><span>Print</span></button><button data-my-orders>Orders</button><button data-analysis-type="ficha">AI</button><button data-copy-response>Copy</button><button id="other">Other</button>');
    document.querySelector('span').click();
    for (const selector of ['[data-my-orders]', '[data-analysis-type]', '[data-copy-response]', '#other']) document.querySelector(selector).click();
    assert.deepEqual(calls, ['print', '/Orders/MyOrders', 'ficha', 'copy']);
});

test('cancelled confirmation prevents submit; accepted and ordinary forms still submit', t => {
    const { dom, document, actions } = setup(t, '<form data-confirm="Confirmar"></form><form id="ordinary"></form>');
    const confirmed = document.querySelector('form');
    function submit(form) {
        const event = new dom.window.Event('submit', { bubbles: true, cancelable: true });
        form.dispatchEvent(event);
        return event.defaultPrevented;
    }
    actions.confirm = () => false;
    assert.equal(submit(confirmed), true);
    actions.confirm = () => true;
    assert.equal(submit(confirmed), false);
    assert.equal(submit(document.querySelector('#ordinary')), false);
});
