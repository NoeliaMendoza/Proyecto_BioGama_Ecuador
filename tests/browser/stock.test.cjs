const { test } = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const vm = require('node:vm');
const { JSDOM } = require('jsdom');
const sourcePath = path.resolve(__dirname, '../../wwwroot/js/product-stock.js');
const source = fs.readFileSync(sourcePath, 'utf8');
const drain = () => new Promise(resolve => setImmediate(resolve));
const snapshot = (available, minStock = 2) => ({
    total: available, products: [{ id: 'item-1', available, minStock }]
});

function catalogue(t, { fetch, startFails = false, total = true } = {}) {
    const dom = new JSDOM(`<!doctype html><p id="stockLiveStatus"></p>
        ${total ? '<span data-available-total></span>' : ''}
        <article data-product-id="ITEM-1"><div class="product-card-status"><span class="badge"></span></div>
        <span class="product-card-stock"></span><form class="product-card-buy"><input name="quantity" value="1"><button>Agregar</button></form>
        <button data-sold-out>Agotado</button></article>`, {
        url: 'https://biogama.test/Products?search=kit', runScripts: 'outside-only', pretendToBeVisual: true
    });
    t.after(() => dom.window.close());
    const handlers = {};
    const timeouts = [];
    const intervals = [];
    const calls = [];
    const connection = {
        state: 'Connected',
        on(name, callback) { handlers[name] = callback; },
        onreconnecting(callback) { handlers.reconnecting = callback; },
        onreconnected(callback) { handlers.reconnected = callback; },
        onclose(callback) { handlers.close = callback; },
        async start() {
            if (startFails) { startFails = false; throw new Error('connection unavailable'); }
            this.state = 'Connected';
        }
    };
    const builder = {
        withUrl() { return this; }, withAutomaticReconnect() { return this; }, build() { return connection; }
    };
    dom.window.signalR = {
        HubConnectionBuilder: function () { return builder; },
        HttpTransportType: { WebSockets: 1 }, HubConnectionState: { Connected: 'Connected' }
    };
    dom.window.setTimeout = (callback, delay) => { timeouts.push({ callback, delay }); return timeouts.length; };
    dom.window.clearTimeout = () => {};
    dom.window.setInterval = (callback, delay) => { intervals.push({ callback, delay }); return intervals.length; };
    let fetcher = fetch || (async () => ({ ok: true, json: async () => snapshot(10) }));
    dom.window.fetch = async (...args) => { calls.push(args); return fetcher(...args); };
    vm.runInContext(source, dom.getInternalVMContext(), { filename: sourcePath });
    return {
        document: dom.window.document, window: dom.window, handlers, connection, timeouts, intervals, calls,
        setSnapshot(data) { fetcher = async () => ({ ok: true, json: async () => data }); },
        text(selector) { return dom.window.document.querySelector(selector).textContent; }
    };
}

test('al conectar consulta el filtro actual y muestra disponibilidad', async t => {
    const app = catalogue(t); await drain();
    assert.equal(app.text('[data-available-total]'), '10');
    assert.equal(app.text('.product-card-stock'), '10 disponibles');
    assert.equal(app.text('.badge'), 'Disponible');
    assert.equal(app.calls[0][0], '/Products/Availability?search=kit');
    assert.equal(app.calls[0][1].cache, 'no-store');
});

test('agotarse oculta la compra y reponer stock vuelve a permitirla', async t => {
    const app = catalogue(t); await drain();
    app.setSnapshot(snapshot(0)); await app.handlers.StockChanged();
    assert.equal(app.text('.badge'), 'Agotado');
    assert.equal(app.document.querySelector('.product-card-buy button').disabled, true);
    assert.equal(app.document.querySelector('.product-card-buy').classList.contains('d-none'), true);
    app.setSnapshot(snapshot(8)); await app.handlers.StockChanged();
    assert.equal(app.document.querySelector('.product-card-buy button').disabled, false);
    assert.equal(app.document.querySelector('[data-sold-out]').classList.contains('d-none'), true);
});

test('una reserva reduce el maximo de compra y ajusta una cantidad excesiva', async t => {
    const app = catalogue(t); await drain();
    app.document.querySelector('input').value = '9';
    app.setSnapshot(snapshot(2)); await app.handlers.StockChanged();
    assert.equal(app.document.querySelector('input').value, '2');
    assert.equal(app.document.querySelector('input').max, '2');
    assert.match(app.text('.badge'), /2 unidades/);
});

test('producto retirado o stock negativo impiden comprar', async t => {
    const app = catalogue(t); await drain();
    app.setSnapshot({ total: 0, products: [] }); await app.handlers.StockChanged();
    assert.equal(app.text('.badge'), 'No disponible');
    app.setSnapshot(snapshot(-1)); await app.handlers.StockChanged();
    assert.equal(app.text('.product-card-stock'), '0 disponibles');
    assert.equal(app.document.querySelector('.product-card-buy button').disabled, true);
});

test('reconexion sincroniza el stock perdido y cierre informa al usuario', async t => {
    const app = catalogue(t); await drain();
    app.handlers.reconnecting(); assert.match(app.text('#stockLiveStatus'), /Reconectando/);
    app.setSnapshot(snapshot(4)); await app.handlers.reconnected();
    assert.equal(app.text('.product-card-stock'), '4 disponibles');
    app.handlers.close(); assert.match(app.text('#stockLiveStatus'), /Sin actualizaciones/);
    app.connection.state = 'Disconnected'; await app.handlers.StockChanged();
    assert.match(app.text('#stockLiveStatus'), /Conexion interrumpida/);
});

test('fallo HTTP no borra el catalogo y programa reintento', async t => {
    const app = catalogue(t, { fetch: async () => ({ ok: false }) }); await drain();
    assert.match(app.text('#stockLiveStatus'), /No se pudo actualizar/);
    assert.equal(app.timeouts[0].delay, 5000);
    app.setSnapshot(snapshot(7)); await app.timeouts[0].callback();
    assert.equal(app.text('.product-card-stock'), '7 disponibles');
});

test('fallo inicial de conexion reintenta y termina sincronizando', async t => {
    const app = catalogue(t, { startFails: true }); await drain();
    assert.match(app.text('#stockLiveStatus'), /Reintentando/);
    assert.equal(app.timeouts[0].delay, 5000);
    await app.timeouts[0].callback();
    assert.equal(app.text('[data-available-total]'), '10');
});

test('eventos concurrentes se serializan y conservan la ultima disponibilidad', async t => {
    let resolveFirst;
    const first = new Promise(resolve => { resolveFirst = resolve; });
    let count = 0;
    const app = catalogue(t, { fetch: async () => ++count === 1 ? first : { ok: true, json: async () => snapshot(3) } });
    await drain();
    await app.handlers.StockChanged();
    assert.equal(count, 1);
    resolveFirst({ ok: true, json: async () => snapshot(10) }); await drain();
    assert.equal(count, 2);
    assert.equal(app.text('.product-card-stock'), '3 disponibles');
});

test('sin evento la sincronizacion periodica y recuperar foco actualizan datos', async t => {
    const app = catalogue(t, { total: false }); await drain();
    assert.equal(app.intervals[0].delay, 30000);
    app.setSnapshot(snapshot(6)); await app.intervals[0].callback();
    assert.equal(app.text('.product-card-stock'), '6 disponibles');
    Object.defineProperty(app.document, 'hidden', { value: true, configurable: true });
    const before = app.calls.length;
    app.document.dispatchEvent(new app.window.Event('visibilitychange')); await drain();
    assert.equal(app.calls.length, before);
    Object.defineProperty(app.document, 'hidden', { value: false });
    app.setSnapshot(snapshot(5));
    app.document.dispatchEvent(new app.window.Event('visibilitychange')); await drain();
    assert.equal(app.text('.product-card-stock'), '5 disponibles');
});
