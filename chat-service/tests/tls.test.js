const { test } = require('node:test');
const assert = require('node:assert/strict');
const { readFileSync } = require('node:fs');
const https = require('node:https');
const { randomBytes } = require('node:crypto');
const { createChatServer } = require('../server/app');

test('el servicio HTTPS presenta el certificado esperado y el cliente valida la cadena', {
  skip: !process.env.CHAT_TLS_TEST_PFX, timeout: 10000
}, async t => {
  const certificate = readFileSync(process.env.CHAT_TLS_TEST_CA);
  const app = createChatServer({ key: randomBytes(48).toString('base64'), origins: ['https://localhost:30080'],
    store: {}, limit: async () => true,
    tls: { pfx: readFileSync(process.env.CHAT_TLS_TEST_PFX), passphrase: process.env.CHAT_TLS_TEST_PASSWORD, minVersion: 'TLSv1.2' }
  });
  t.after(() => new Promise(resolve => app.io.close(resolve)));
  await new Promise(resolve => app.server.listen(0, 'localhost', resolve));
  const response = await new Promise((resolve,reject) => {
    https.get({ hostname: 'localhost', port: app.server.address().port, path: '/health', ca: certificate, rejectUnauthorized: true },
      result => { const checked = { statusCode: result.statusCode, authorized: result.socket.authorized }; result.resume(); resolve(checked); }).on('error',reject);
  });
  assert.equal(response.statusCode, 200);
  assert.equal(response.authorized, true);
});
