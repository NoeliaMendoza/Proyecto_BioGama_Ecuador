const http = require('node:http');
const https = require('node:https');
const express = require('express');
const { Server } = require('socket.io');
const { authenticate } = require('./auth');
const registerPrivateMessageHandlers = require('./socket/privateMessageHandler');

function createChatServer({ key, origins, store, limit, adapter, tls, ready = async () => true, logger = console }) {
  const app = express();
  app.disable('x-powered-by');
  app.use((_req, res, next) => { res.set('X-Content-Type-Options', 'nosniff'); res.set('Cache-Control', 'no-store'); next(); });
  app.get('/health', (_req, res) => res.json({ status: 'healthy' }));
  app.get('/health/ready', async (_req, res) => {
    try { if (!await ready()) throw new Error('not ready'); res.json({ status: 'ready' }); }
    catch { res.status(503).json({ status: 'unavailable' }); }
  });
  const server = tls ? https.createServer(tls, app) : http.createServer(app);
  const io = new Server(server, {
    transports: ['websocket'], maxHttpBufferSize: 16384, serveClient: false,
    // WebSockets no quedan protegidos solamente por CORS: comprobar Origin en el handshake.
    allowRequest: (req, callback) => callback(null, origins.includes(req.headers.origin))
  });
  if (adapter) io.adapter(adapter);
  io.use(authenticate(key));
  io.on('connection', socket => registerPrivateMessageHandlers(io, socket, store, limit, logger));
  return { server, io };
}
module.exports = { createChatServer };
