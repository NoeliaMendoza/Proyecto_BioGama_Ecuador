/* Adaptación de privateMessages.js y socket.js de Tarea2: acuses, borrador y texto seguro. */
(() => {
    'use strict';
    const root = document.getElementById('support-chat');
    if (!root) return;
    const admin = root.dataset.admin === 'true';
    const byId = id => document.getElementById(id);
    const input = byId('chat-content'), send = byId('chat-send'), list = byId('chat-messages');
    const status = byId('chat-status'), error = byId('chat-error');
    const csrf = root.querySelector('input[name="__RequestVerificationToken"]').value;
    let visitorId = null, sending = false, refreshing = false, hasOlder = false;
    let pendingMessage = null, selectedName = '', threads = [], messages = new Map();
    let selection = 0, tokenDenied = false, refreshAgain = false;
    const drafts = new Map();
    const socket = io(root.dataset.serviceUrl, {
        transports: ['websocket'], autoConnect: false, reconnectionDelayMax: 10000,
        auth: async callback => {
            try {
                const response = await fetch('/Chat/Token', { method: 'POST', credentials: 'same-origin',
                    headers: { RequestVerificationToken: csrf, Accept: 'application/json' } });
                if (!response.ok || !response.headers.get('content-type')?.includes('application/json')) {
                    tokenDenied = true;
                    throw new Error('Inicia sesión nuevamente en BioGama.');
                }
                const credentials = await response.json();
                tokenDenied = false;
                callback({ token: credentials.token });
            } catch (failure) {
                error.textContent = failure.message;
                callback({ token: '' });
            }
        }
    });
    function controls() {
        const enabled = socket.connected && visitorId !== null && !sending;
        input.disabled = !enabled; send.disabled = !enabled;
    }
    function request(event, payload = {}) {
        return new Promise((resolve, reject) => {
            if (!socket.connected) { reject(new Error('Espera a recuperar la conexión.')); return; }
            socket.timeout(12000).emit(event, payload, (failure, response) => {
                if (failure) reject(new Error('No se recibió confirmación. Reintenta: el mensaje no se duplicará.'));
                else if (!response?.ok) reject(new Error(response?.error || 'No se pudo completar la solicitud.'));
                else resolve(response.data);
            });
        });
    }
    function render() {
        list.replaceChildren();
        for (const message of [...messages.values()].sort((a, b) => BigInt(a.sequence) < BigInt(b.sequence) ? -1 : 1)) {
            const item = document.createElement('li');
            item.className = 'support-message' + (message.isAdmin ? ' is-admin' : '');
            const author = document.createElement('strong'); author.textContent = message.author;
            const text = document.createElement('p'); text.textContent = message.content;
            const time = document.createElement('time'); time.dateTime = message.createdAt;
            time.textContent = new Date(message.createdAt).toLocaleString('es-EC', { dateStyle: 'short', timeStyle: 'short' });
            item.append(author, text, time); list.append(item);
        }
        byId('chat-empty').hidden = messages.size > 0;
        byId('chat-older').hidden = !hasOlder;
    }
    function renderThreads() {
        const target = byId('chat-threads');
        target.replaceChildren();
        if (!threads.length) target.textContent = 'Todavía no hay consultas de visitantes.';
        for (const thread of threads) {
            const button = document.createElement('button'); button.type = 'button'; button.className = 'support-thread';
            button.textContent = thread.name;
            button.setAttribute('aria-pressed', String(thread.visitorId === visitorId));
            button.addEventListener('click', () => {
                if (sending) return;
                if (visitorId !== null) drafts.set(visitorId, input.value);
                visitorId = thread.visitorId; selectedName = thread.name; selection++;
                input.value = drafts.get(visitorId) || ''; pendingMessage = null;
                messages = new Map(); hasOlder = false; render(); controls();
                byId('chat-title').textContent = 'Conversación con ' + selectedName;
                renderThreads(); refresh().catch(showError);
            });
            target.append(button);
        }
    }
    function showError(failure) { error.textContent = failure.message; }
    async function refresh() {
        if (refreshing) { refreshAgain = true; return; }
        if (!socket.connected) return;
        refreshing = true;
        const current = selection, active = visitorId;
        try {
            if (admin) { threads = await request('private:threads'); renderThreads(); }
            if (active !== null) {
                const history = await request('private:history', { visitorId: active });
                if (current !== selection) return;
                for (const message of history) messages.set(message.id, message);
                hasOlder = history.length === 100;
                render(); list.scrollTop = list.scrollHeight;
            }
        } finally {
            refreshing = false;
            if (refreshAgain) { refreshAgain = false; void refresh().catch(showError); }
        }
    }
    socket.on('connect', () => {
        status.textContent = 'Conectado'; error.textContent = ''; controls();
    });
    socket.on('connection:ready', identity => {
        if (!admin) visitorId = identity.userId;
        controls(); refresh().catch(showError);
    });
    socket.on('disconnect', reason => {
        status.textContent = 'Sin conexión. El historial guardado se recuperará al reconectar.'; controls();
        if (reason === 'io server disconnect' && !tokenDenied) setTimeout(() => socket.connect(), 1000);
    });
    socket.on('connect_error', () => {
        status.textContent = 'Chat no disponible. Reintentando…'; controls();
        if (!tokenDenied && !socket.active) setTimeout(() => socket.connect(), 5000);
        if (tokenDenied) status.textContent = 'Vuelve a iniciar sesión para continuar.';
    });
    socket.on('private:message', message => {
        if (message.visitorId === visitorId) {
            messages.set(message.id, message); render(); list.scrollTop = list.scrollHeight;
        }
        if (admin) refresh().catch(showError);
    });
    byId('chat-form').addEventListener('submit', async event => {
        event.preventDefault();
        if (sending || !input.value.trim() || visitorId === null || !socket.connected) return;
        const content = input.value.trim(), active = visitorId;
        if (!pendingMessage || pendingMessage.content !== content || pendingMessage.visitorId !== active)
            pendingMessage = { id: crypto.randomUUID(), visitorId: active, content };
        sending = true; controls(); error.textContent = '';
        try {
            const message = await request('private:message', pendingMessage);
            messages.set(message.id, message); input.value = ''; drafts.delete(active); pendingMessage = null;
            render(); list.scrollTop = list.scrollHeight;
        } catch (failure) { showError(failure); }
        finally { sending = false; controls(); }
    });
    byId('chat-older').addEventListener('click', async () => {
        const current = selection;
        const first = [...messages.values()].reduce((value, message) =>
            value === null || BigInt(message.sequence) < BigInt(value) ? message.sequence : value, null);
        if (!first) return;
        try {
            const history = await request('private:history', { visitorId, before: first });
            if (current !== selection) return;
            for (const message of history) messages.set(message.id, message);
            hasOlder = history.length === 100; render();
        } catch (failure) { showError(failure); }
    });
    byId('chat-refresh')?.addEventListener('click', () => refresh().catch(showError));
    window.addEventListener('focus', () => refresh().catch(showError));
    // Redis Pub/Sub no conserva mensajes perdidos: recuperar siempre desde PostgreSQL.
    setInterval(() => { if (!document.hidden) refresh().catch(showError); }, 30000);
    socket.connect();
})();
