const { test } = require('node:test');
const assert = require('node:assert/strict');
const { randomBytes, randomUUID, createHmac } = require('node:crypto');
const { Pool } = require('pg');
const { createClient } = require('redis');
const { createAdapter } = require('@socket.io/redis-adapter');
const { io: connect } = require('socket.io-client');
const { ChatStore } = require('../server/store');
const { createChatServer } = require('../server/app');

test('dos instancias con PostgreSQL y Redis: privacidad, entrega, reintentos e historial', {
  skip: process.env.CHAT_INTEGRATION !== '1', timeout: 45000
}, async t => {
  const pool = new Pool({ max: 5 });
  const key = randomBytes(32).toString('base64'), suffix = randomUUID();
  const visitor = 'test-visitor-' + suffix, outsider = 'test-outsider-' + suffix;
  const admin = 'test-admin-' + suffix;
  const store = new ChatStore(pool); await store.initialize();
  const resources = [], sockets = [], servers = [];
  t.after(async () => {
    for (const socket of sockets) socket.disconnect();
    for (const app of servers) await new Promise(resolve => app.io.close(resolve));
    await Promise.allSettled(resources.map(client => client.quit()));
    await pool.query('DELETE FROM biogama_chat.messages WHERE visitor_id=ANY($1::text[])', [[visitor, outsider]]);
    await pool.query('DELETE FROM biogama_chat.threads WHERE visitor_id=ANY($1::text[])', [[visitor, outsider]]);
    await pool.end();
  });
  function token(id, role = 'visitor') {
    const now = Math.floor(Date.now() / 1000);
    const payload = Buffer.from(JSON.stringify({ sub: id, name: 'Prueba automatizada', role,
      iss: 'biogama-web', aud: 'biogama-chat', iat: now, exp: now + 300 })).toString('base64url');
    return payload + '.' + createHmac('sha256', key).update(payload).digest('base64url');
  }
  async function instance() {
    const pub = createClient({ url: process.env.REDIS_URL }), sub = pub.duplicate();
    resources.push(pub, sub); await Promise.all([pub.connect(), sub.connect()]);
    const app = createChatServer({ key, origins: ['http://localhost:30080'], store,
      adapter: createAdapter(pub, sub, { key: 'test-chat-' + suffix }), limit: async () => true });
    servers.push(app);
    await new Promise(resolve => app.server.listen(0, '127.0.0.1', resolve));
    return 'http://127.0.0.1:' + app.server.address().port;
  }
  async function client(url, auth, origin = 'http://localhost:30080', shouldFail = false) {
    const socket = connect(url, { transports: ['websocket'], reconnection: false,
      auth: { token: auth }, extraHeaders: { Origin: origin }, autoConnect: false });
    sockets.push(socket);
    await new Promise((resolve, reject) => {
      const timer = setTimeout(() => reject(new Error('Handshake timeout')), 7000);
      socket.on('connect', () => { clearTimeout(timer); shouldFail ? reject(new Error('Acceso indebido')) : resolve(); });
      socket.on('connect_error', error => { clearTimeout(timer); shouldFail ? resolve() : reject(error); });
      socket.connect();
    });
    return socket;
  }
  const request = (socket, event, payload = {}) => new Promise((resolve, reject) => {
    socket.timeout(5000).emit(event, payload, (failure, reply) => failure ? reject(failure) : resolve(reply));
  });
  const first = await instance(), second = await instance();
  await client(first, 'invalid-token', undefined, true);
  await client(first, token(visitor), 'https://untrusted.example', true);
  const a = await client(first, token(visitor));
  const b = await client(second, token(admin, 'admin'));
  const c = await client(second, token(outsider));
  let leaked = false; c.on('private:message', () => { leaked = true; });
  const received = new Promise(resolve => b.once('private:message', resolve));
  const payload = { id: randomUUID(), visitorId: outsider, content: 'Quiero colaborar como investigador.' };
  const sent = await request(a, 'private:message', payload);
  assert.equal(sent.ok, true); assert.equal(sent.data.visitorId, visitor);
  const event = await received; assert.equal(event.id, payload.id);
  const duplicate = await request(a, 'private:message', payload);
  assert.equal(duplicate.data.sequence, sent.data.sequence);
  const badReuse = await request(a, 'private:message', { ...payload, content: 'Otro texto' });
  assert.equal(badReuse.ok, false);
  const answer = new Promise(resolve => a.once('private:message', resolve));
  assert.equal((await request(b, 'private:message', { id: randomUUID(), visitorId: visitor, content: 'Gracias. Revisaremos tu solicitud.' })).ok, true);
  assert.equal((await answer).isAdmin, true);
  assert.equal((await request(c, 'private:history', { visitorId: visitor })).data.length, 0);
  assert.equal((await request(c, 'private:threads')).ok, false);
  assert.equal((await request(a, 'private:message', { id: randomUUID(), content: ' ' })).ok, false);
  a.disconnect();
  const recovered = await client(second, token(visitor));
  const history = await request(recovered, 'private:history');
  assert.equal(history.data.length, 2);
  assert.equal((await new ChatStore(pool).history({ id: visitor, admin: false }, {})).length, 2);
  assert.equal((await pool.query('SELECT count(*) FROM biogama_chat.messages WHERE id=$1', [payload.id])).rows[0].count, '1');
  assert.equal(leaked, false);
});
