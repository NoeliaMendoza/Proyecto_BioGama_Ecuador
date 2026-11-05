const { test } = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const vm = require('node:vm');
const { JSDOM } = require('jsdom');
const sourcePath = path.join(__dirname, '../../wwwroot/js/support-chat.js');
const source = fs.readFileSync(sourcePath, 'utf8');
const drain = async () => { for (let i = 0; i < 8; i++) await new Promise(resolve => setImmediate(resolve)); };
function page(t, { admin = false, denied = false } = {}) {
  const dom = new JSDOM(`<section id="support-chat" data-service-url="http://localhost:30081" data-admin="${admin}">
    ${admin ? '<button id="chat-refresh"></button><div id="chat-threads"></div>' : ''}
    <h2 id="chat-title"></h2><p id="chat-status"></p><p id="chat-error"></p><p id="chat-empty"></p>
    <ol id="chat-messages"></ol><button id="chat-older" hidden></button>
    <form id="chat-form"><input name="__RequestVerificationToken" value="test-csrf"><textarea id="chat-content"></textarea><button id="chat-send"></button></form>
    </section>`, { url: 'http://localhost:30080/Chat', runScripts: 'outside-only' });
  t.after(() => dom.window.close());
  const handlers = {}, calls = [], history = [], intervals = [];
  let sequence = 0, failNext = false;
  const socket = {
    connected: false, active: false,
    on(event, callback) { handlers[event] = callback; },
    connect() {
      options.auth(credentials => {
        if (!credentials.token) { handlers.connect_error(); return; }
        socket.connected = true; handlers.connect(); handlers['connection:ready']({ userId: admin ? 'admin' : 'visitor', admin });
      });
    },
    timeout() { return socket; },
    emit(event, payload, callback) {
      calls.push({ event, payload });
      if (event === 'private:history') callback(null, { ok: true, data: history });
      if (event === 'private:threads') callback(null, { ok: true, data: [{ visitorId: 'visitor', name: '<img src=x onerror=alert(1)>' }] });
      if (event === 'private:message') {
        if (failNext) { failNext = false; callback(new Error('timeout')); return; }
        const message = { ...payload, sequence: String(++sequence), author: '<script>unsafe()</script>', isAdmin: admin, createdAt: new Date().toISOString() };
        callback(null, { ok: true, data: message });
      }
    }
  };
  let options;
  dom.window.io = (_url, configured) => { options = configured; return socket; };
  dom.window.fetch = async (_url, configured) => {
    assert.equal(configured.method, 'POST'); assert.equal(configured.headers.RequestVerificationToken, 'test-csrf');
    return { ok: !denied, headers: { get: () => 'application/json' }, json: async () => ({ token: 'temporary-credential' }) };
  };
  dom.window.setInterval = callback => { intervals.push(callback); return intervals.length; };
  vm.runInContext(source, dom.getInternalVMContext(), { filename: sourcePath });
  return { window: dom.window, socket, handlers, calls, history, intervals, fail: () => { failNext = true; } };
}
test('visitante envía texto sin ejecutar HTML y conserva el identificador al reintentar', async t => {
  const app = page(t); await drain();
  const document = app.window.document, input = document.getElementById('chat-content');
  assert.equal(input.disabled, false);
  input.value = '<img src=x onerror=alert(1)>'; app.fail();
  document.getElementById('chat-form').dispatchEvent(new app.window.Event('submit', { cancelable: true })); await drain();
  assert.equal(input.value, '<img src=x onerror=alert(1)>');
  document.getElementById('chat-form').dispatchEvent(new app.window.Event('submit', { cancelable: true })); await drain();
  const sends = app.calls.filter(call => call.event === 'private:message');
  assert.equal(sends.length, 2); assert.equal(sends[0].payload.id, sends[1].payload.id);
  assert.equal(document.querySelector('#chat-messages img'), null);
  assert.equal(document.querySelector('#chat-messages script'), null);
  assert.match(document.getElementById('chat-messages').textContent, /<img/);
  assert.equal(input.value, '');
});
test('el administrador selecciona un visitante y los mensajes no cambian a otra conversación', async t => {
  const app = page(t, { admin: true }); await drain();
  const document = app.window.document;
  assert.equal(document.getElementById('chat-send').disabled, true);
  assert.equal(document.querySelector('#chat-threads img'), null);
  document.querySelector('#chat-threads button').click(); await drain();
  assert.equal(document.getElementById('chat-send').disabled, false);
  app.handlers['private:message']({ id: 'unrelated', visitorId: 'other', sequence: '1', content: 'No debe aparecer', author: 'Otro', createdAt: new Date().toISOString() });
  await drain(); assert.doesNotMatch(document.getElementById('chat-messages').textContent, /No debe aparecer/);
});
test('sin sesión válida bloquea el envío y al desconectarse conserva el historial visible', async t => {
  const denied = page(t, { denied: true }); await drain();
  assert.equal(denied.window.document.getElementById('chat-send').disabled, true);
  assert.match(denied.window.document.getElementById('chat-status').textContent, /iniciar sesión/);
  const app = page(t); await drain();
  app.socket.connected = false; app.handlers.disconnect('transport close');
  assert.equal(app.window.document.getElementById('chat-send').disabled, true);
  assert.match(app.window.document.getElementById('chat-status').textContent, /historial/);
});
