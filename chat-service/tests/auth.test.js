const { test } = require('node:test');
const assert = require('node:assert/strict');
const { createHmac, randomBytes } = require('node:crypto');
const { validateToken } = require('../server/auth');
const { visitorFor, validateContent } = require('../server/store');
const key = randomBytes(32).toString('base64');
const now = Math.floor(Date.now() / 1000);
function sign(changes = {}) {
  const payload = Buffer.from(JSON.stringify({ iss: 'biogama-web', aud: 'biogama-chat', sub: 'visitor-1',
    name: 'Visitante', role: 'visitor', iat: now, exp: now + 300, ...changes })).toString('base64url');
  return payload + '.' + createHmac('sha256', key).update(payload).digest('base64url');
}
test('middleware valida firma, emisor, audiencia, identidad, rol y vencimiento', () => {
  assert.equal(validateToken(sign(), key, now).id, 'visitor-1');
  assert.equal(validateToken(sign({ role: 'admin' }), key, now).admin, true);
  assert.throws(() => validateToken(sign(), randomBytes(32).toString('base64'), now));
  for (const claims of [{ exp: now }, { iat: now + 60 }, { exp: now + 301 }, { role: 'Administrador' },
    { aud: 'otro-servicio' }, { iss: 'otro-emisor' }, { sub: '' }, { name: null }, { exp: '9999999999' }])
    assert.throws(() => validateToken(sign(claims), key, now));
  for (const token of ['', null, 'a.b.c', 'a.!', 'x'.repeat(5000)]) assert.throws(() => validateToken(token, key, now));
});
test('un visitante no puede seleccionar la conversación de otra persona', () => {
  assert.equal(visitorFor({ id: 'visitor-1', admin: false }, 'visitor-2'), 'visitor-1');
  assert.equal(visitorFor({ id: 'admin', admin: true }, 'visitor-2'), 'visitor-2');
  assert.throws(() => visitorFor({ admin: true }, null));
});
test('los mensajes vacíos, no textuales y demasiado largos se rechazan', () => {
  for (const value of ['', '   ', null, {}, 'x'.repeat(2001)]) assert.throws(() => validateContent(value));
  assert.equal(validateContent('  Hola  '), 'Hola');
  assert.equal(validateContent('<img src=x onerror=alert(1)>'), '<img src=x onerror=alert(1)>');
});
module.exports = { sign, key };
