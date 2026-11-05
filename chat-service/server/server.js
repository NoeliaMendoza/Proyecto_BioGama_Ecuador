const { Pool } = require('pg');
const { createClient } = require('redis');
const { createAdapter } = require('@socket.io/redis-adapter');
const { ChatStore } = require('./store');
const { createChatServer } = require('./app');
const { readFileSync } = require('node:fs');

async function start() {
  const key = process.env.CHAT_SIGNING_KEY;
  if (!key || Buffer.byteLength(key) < 32) throw new Error('Configura CHAT_SIGNING_KEY con al menos 32 bytes.');
  const origins = (process.env.CHAT_ALLOWED_ORIGINS || '').split(',').map(value => value.trim()).filter(Boolean);
  if (!origins.length || origins.some(value => !/^https?:\/\/[^/]+$/.test(value)))
    throw new Error('Configura los orígenes exactos permitidos de BioGama.');
  if (!process.env.REDIS_URL || !process.env.PGPASSWORD) throw new Error('Configura Redis y PostgreSQL.');
  const pool = new Pool({ max: 10, connectionTimeoutMillis: 5000, query_timeout: 10000 });
  pool.on('error', () => console.error('Conexión PostgreSQL interrumpida.'));
  const pub = createClient({ url: process.env.REDIS_URL });
  const sub = pub.duplicate();
  for (const client of [pub, sub]) client.on('error', () => console.error('Conexión Redis interrumpida.'));
  await Promise.all([pub.connect(), sub.connect()]);
  const store = new ChatStore(pool);
  await store.initialize();
  const limit = async id => {
    const count = await pub.eval(`local n=redis.call('INCR',KEYS[1]); if n==1 then redis.call('PEXPIRE',KEYS[1],60000) end; return n`,
      { keys: ['biogama-chat:rate:' + id], arguments: [] });
    return count <= 120;
  };
  const tlsFile = process.env.TLS_PFX_FILE;
  if (tlsFile && !process.env.TLS_PFX_PASSWORD) throw new Error('Falta la clave del certificado TLS.');
  const tls = tlsFile ? { pfx: readFileSync(tlsFile), passphrase: process.env.TLS_PFX_PASSWORD, minVersion: 'TLSv1.2' } : undefined;
  const { server, io } = createChatServer({ key, origins, store, limit, tls,
    adapter: createAdapter(pub, sub, { key: 'biogama-support' }),
    ready: async () => { await pool.query('SELECT 1'); return pub.isReady && sub.isReady; }
  });
  const port = Number(process.env.PORT || 3000);
  server.listen(port, '0.0.0.0', () => console.log('Chat BioGama disponible en puerto ' + port));
  let closing = false;
  async function close() {
    if (closing) return;
    closing = true;
    const timeout = setTimeout(() => process.exit(1), 10000); timeout.unref();
    await new Promise(resolve => io.close(resolve));
    await Promise.allSettled([pool.end(), pub.quit(), sub.quit()]);
  }
  process.on('SIGTERM', close);
  process.on('SIGINT', close);
}
start().catch(() => { console.error('No se pudo iniciar el chat. Revisa configuración y conectividad.'); process.exit(1); });
