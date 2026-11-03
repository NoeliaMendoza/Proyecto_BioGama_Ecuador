const { test } = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const vm = require('node:vm');
const pathModule = require('node:path');
const { JSDOM } = require('jsdom');

function render(text) {
    const dom = new JSDOM('', { runScripts: 'outside-only' });
    try {
        for (const path of ['wwwroot/lib/marked/marked.umd.js', 'wwwroot/lib/dompurify/purify.min.js', 'wwwroot/js/markdown-renderer.js']) {
            vm.runInContext(fs.readFileSync(path, 'utf8'), dom.getInternalVMContext(), { filename: pathModule.resolve(path) });
        }
        return dom.window.BioGamaMarkdown.render(text);
    } finally { dom.window.close(); }
}

test('AI markdown keeps headings and emphasis', () => {
    const html = render('# Jaguar\n\n**Especie** de Ecuador');
    assert.match(html, /<h1>Jaguar<\/h1>/);
    assert.match(html, /<strong>Especie<\/strong>/);
});

test('AI output cannot inject scripts, events or javascript links', () => {
    const html = render('<script>alert(1)</script><img src="x" onerror="alert(1)"><a href="javascript:alert(1)">Enlace</a>');
    assert.doesNotMatch(html, /<script|onerror|javascript:/i);
    assert.match(html, /Enlace/);
});

test('missing AI output renders safely as empty text', () => {
    assert.equal(render(undefined), '');
});
