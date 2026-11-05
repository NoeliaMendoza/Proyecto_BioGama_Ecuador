const { test } = require('node:test');
const assert = require('node:assert/strict');
const { spawn } = require('node:child_process');
const { once } = require('node:events');
const { randomBytes } = require('node:crypto');
const net = require('node:net');
const path = require('node:path');
const entry = path.join(__dirname, '../server/server.js');
test('el servicio rechaza arrancar sin clave compartida', async () => {
  const child = spawn(process.execPath, [entry], { windowsHide: true, env: { ...process.env, CHAT_SIGNING_KEY: '' }, stdio: 'ignore' });
  const [exitCode] = await once(child, 'exit');
  assert.equal(exitCode, 1);
});
test('el proceso real responde a las sondas con PostgreSQL y Redis disponibles', {
  skip: process.env.CHAT_INTEGRATION !== '1', timeout: 20000
}, async t => {
  const reservation = net.createServer();
  await new Promise(resolve => reservation.listen(0, '127.0.0.1', resolve));
  const port = reservation.address().port;
  await new Promise(resolve => reservation.close(resolve));
  const child = spawn(process.execPath, [entry], { windowsHide: true, stdio: 'ignore', env: {
    ...process.env, PORT: String(port), CHAT_SIGNING_KEY: randomBytes(48).toString('base64'),
    CHAT_ALLOWED_ORIGINS: 'http://localhost:30080'
  } });
  t.after(async () => {
    if (child.exitCode === null) { const exit = once(child, 'exit'); child.kill(); await exit; }
  });
  let response;
  for (let i = 0; i < 100; i++) {
    try { response = await fetch(`http://127.0.0.1:${port}/health/ready`); if (response.ok) break; } catch {}
    if (child.exitCode !== null) throw new Error('El proceso de chat no inició.');
    await new Promise(resolve => setTimeout(resolve, 100));
  }
  assert.equal(response?.status, 200);
  assert.deepEqual(await response.json(), { status: 'ready' });
  const health = await fetch(`http://127.0.0.1:${port}/health`);
  assert.equal(health.headers.get('x-content-type-options'), 'nosniff');
  assert.equal(health.headers.get('cache-control'), 'no-store');
  assert.equal(health.headers.get('x-powered-by'), null);
});
