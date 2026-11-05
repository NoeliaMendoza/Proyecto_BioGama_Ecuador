const { ChatError } = require('../errors');
// Adaptación del registro de eventos y acuses de Tarea2: persistencia antes de emitir.
module.exports = function registerPrivateMessageHandlers(io, socket, store, limit, logger) {
  const user = socket.data.user;
  const room = visitorId => 'visitor:' + visitorId;
  socket.join(user.admin ? 'administrators' : room(user.id));
  const expiry = setTimeout(() => socket.disconnect(true), Math.max(1, user.expires * 1000 - Date.now()));
  expiry.unref();
  socket.on('disconnect', () => clearTimeout(expiry));

  function register(event, action) {
    socket.on(event, async (payload, reply) => {
      if (typeof reply !== 'function') return;
      try {
        if (Date.now() >= user.expires * 1000) throw new ChatError('La sesión del chat venció.');
        if (!await limit(user.id)) throw new ChatError('Demasiadas solicitudes. Espera un minuto.');
        reply({ ok: true, data: await action(payload) });
      } catch (error) {
        // Los errores SQL/infraestructura no se exponen al navegador ni se registran con datos privados.
        const expected = error instanceof ChatError;
        if (!expected) logger.error('Falló una operación del chat.');
        reply({ ok: false, error: expected ? error.message : 'Servicio temporalmente no disponible. Inténtalo nuevamente.' });
      }
    });
  }
  register('private:history', payload => store.history(user, payload));
  register('private:threads', () => store.threads(user));
  register('private:message', async payload => {
    const message = await store.create(user, payload);
    // La unión de salas evita duplicar el evento si un socket pertenece a ambas.
    io.to(room(message.visitorId)).to('administrators').emit('private:message', message);
    return message;
  });
  socket.emit('connection:ready', { userId: user.id, admin: user.admin, pod: process.env.HOSTNAME || 'local' });
};
