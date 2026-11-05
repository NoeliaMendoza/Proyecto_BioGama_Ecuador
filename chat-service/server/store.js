const { ChatError } = require('./errors');

function visitorFor(user, requested) {
  if (!user.admin) return user.id;
  if (typeof requested !== 'string' || !requested.trim() || requested.length > 450)
    throw new ChatError('Selecciona una conversación.');
  return requested;
}

// Adaptado de validateContent en messageService de Tarea2. Se conserva texto plano.
function validateContent(content) {
  if (typeof content !== 'string' || !content.trim() || content.length > 2000)
    throw new ChatError('Escribe entre 1 y 2000 caracteres.');
  return content.trim();
}

function message(row) {
  return { id: row.id, sequence: row.sequence, visitorId: row.visitor_id,
    authorId: row.author_id, author: row.author_name, isAdmin: row.is_admin,
    content: row.content, createdAt: new Date(row.created_at).toISOString() };
}

class ChatStore {
  constructor(pool) { this.pool = pool; }
  async initialize() {
    const client = await this.pool.connect();
    try {
      await client.query('BEGIN');
      // Las réplicas inicializan un esquema propio, serializadas por PostgreSQL.
      await client.query('SELECT pg_advisory_xact_lock(721402601)');
      await client.query(`CREATE SCHEMA IF NOT EXISTS biogama_chat;
        CREATE TABLE IF NOT EXISTS biogama_chat.threads (
          visitor_id varchar(450) PRIMARY KEY, visitor_name varchar(256) NOT NULL,
          updated_at timestamptz NOT NULL DEFAULT now());
        CREATE TABLE IF NOT EXISTS biogama_chat.messages (
          id uuid PRIMARY KEY, sequence bigserial UNIQUE NOT NULL,
          visitor_id varchar(450) NOT NULL REFERENCES biogama_chat.threads(visitor_id),
          author_id varchar(450) NOT NULL, author_name varchar(256) NOT NULL,
          is_admin boolean NOT NULL, content varchar(2000) NOT NULL CHECK (length(trim(content)) > 0),
          created_at timestamptz NOT NULL DEFAULT now());
        CREATE INDEX IF NOT EXISTS messages_thread_sequence ON biogama_chat.messages(visitor_id, sequence DESC)`);
      await client.query('COMMIT');
    } catch (error) { await client.query('ROLLBACK'); throw error; }
    finally { client.release(); }
  }
  async history(user, payload) {
    const visitorId = visitorFor(user, payload?.visitorId);
    const before = payload?.before;
    if (before != null && (typeof before !== 'string' || !/^[1-9]\d{0,18}$/.test(before) || BigInt(before) > 9223372036854775807n))
      throw new ChatError('Página inválida.');
    const result = await this.pool.query(`SELECT * FROM (
      SELECT * FROM biogama_chat.messages WHERE visitor_id=$1 AND ($2::bigint IS NULL OR sequence<$2)
      ORDER BY sequence DESC LIMIT 100) recent ORDER BY sequence`, [visitorId, before ?? null]);
    return result.rows.map(message);
  }
  async threads(user) {
    if (!user.admin) throw new ChatError('Solo el administrador puede consultar la bandeja.');
    const result = await this.pool.query(`SELECT visitor_id AS "visitorId", visitor_name AS name,
      updated_at AS "updatedAt" FROM biogama_chat.threads ORDER BY updated_at DESC, visitor_id LIMIT 100`);
    return result.rows;
  }
  async create(user, payload) {
    const visitorId = visitorFor(user, payload?.visitorId);
    const content = validateContent(payload?.content);
    if (typeof payload?.id !== 'string' || !/^[\da-f]{8}-[\da-f]{4}-4[\da-f]{3}-[89ab][\da-f]{3}-[\da-f]{12}$/i.test(payload.id))
      throw new ChatError('Identificador de mensaje inválido.');
    const client = await this.pool.connect();
    try {
      await client.query('BEGIN');
      if (!user.admin) await client.query(`INSERT INTO biogama_chat.threads(visitor_id, visitor_name)
        VALUES ($1,$2) ON CONFLICT(visitor_id) DO UPDATE SET visitor_name=EXCLUDED.visitor_name`, [user.id, user.name]);
      else {
        const found = await client.query('SELECT visitor_id FROM biogama_chat.threads WHERE visitor_id=$1', [visitorId]);
        if (!found.rowCount) throw new ChatError('La conversación no está disponible.');
      }
      const result = await client.query(`INSERT INTO biogama_chat.messages
        (id, visitor_id, author_id, author_name, is_admin, content) VALUES ($1,$2,$3,$4,$5,$6)
        ON CONFLICT(id) DO NOTHING RETURNING *`,
      [payload.id, visitorId, user.id, user.admin ? 'Equipo BioGama' : user.name, user.admin, content]);
      let row = result.rows[0];
      if (!row) {
        row = (await client.query('SELECT * FROM biogama_chat.messages WHERE id=$1', [payload.id])).rows[0];
        if (!row || row.author_id !== user.id || row.visitor_id !== visitorId || row.content !== content)
          throw new ChatError('Identificador de mensaje ya utilizado.');
      }
      await client.query('UPDATE biogama_chat.threads SET updated_at=now() WHERE visitor_id=$1', [visitorId]);
      await client.query('COMMIT');
      return message(row);
    } catch (error) { await client.query('ROLLBACK'); throw error; }
    finally { client.release(); }
  }
}
module.exports = { ChatStore, visitorFor, validateContent };
