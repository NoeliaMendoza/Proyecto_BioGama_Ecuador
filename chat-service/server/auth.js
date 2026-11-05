const { createHmac, timingSafeEqual } = require('node:crypto');

function validateToken(token, key, now = Math.floor(Date.now() / 1000)) {
  if (typeof key !== 'string' || Buffer.byteLength(key) < 32) throw new Error('Clave de chat inválida.');
  if (typeof token !== 'string' || token.length > 4096) throw new Error('Sesión inválida.');
  const parts = token.split('.');
  if (parts.length !== 2 || !parts.every(part => /^[\w-]+$/.test(part))) throw new Error('Sesión inválida.');
  const expected = createHmac('sha256', key).update(parts[0], 'ascii').digest();
  const received = Buffer.from(parts[1], 'base64url');
  if (received.length !== expected.length || !timingSafeEqual(expected, received)) throw new Error('Sesión inválida.');
  const claims = JSON.parse(Buffer.from(parts[0], 'base64url').toString('utf8'));
  if (claims.iss !== 'biogama-web' || claims.aud !== 'biogama-chat' ||
      !Number.isSafeInteger(claims.exp) || !Number.isSafeInteger(claims.iat) ||
      claims.exp <= now || claims.iat > now + 30 || claims.exp - claims.iat > 300 || claims.exp <= claims.iat ||
      typeof claims.sub !== 'string' || !claims.sub.trim() || claims.sub.length > 450 ||
      typeof claims.name !== 'string' || claims.name.length > 256 ||
      !['admin', 'visitor'].includes(claims.role)) throw new Error('Sesión inválida o vencida.');
  return { id: claims.sub, name: claims.name, admin: claims.role === 'admin', expires: claims.exp };
}

// io.use es el middleware: el cliente no puede elegir su identidad ni su rol.
function authenticate(key) {
  return (socket, next) => {
    try { socket.data.user = validateToken(socket.handshake.auth?.token, key); next(); }
    catch { next(new Error('Inicia sesión nuevamente en BioGama.')); }
  };
}
module.exports = { validateToken, authenticate };
