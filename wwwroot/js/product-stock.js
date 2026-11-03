(() => {
    const status = document.getElementById('stockLiveStatus');
    const cards = [...document.querySelectorAll('[data-product-id]')];
    const total = document.querySelector('[data-available-total]');
    const connection = new signalR.HubConnectionBuilder()
        .withUrl('/hubs/stock', {
            transport: signalR.HttpTransportType.WebSockets,
            skipNegotiation: true
        })
        .withAutomaticReconnect()
        .build();
    let pending = false;
    let refreshing = false;
    let retryTimer;

    function render(data) {
        if (total) total.textContent = data.total;
        const products = new Map(data.products.map(p => [p.id.toLowerCase(), p]));
        cards.forEach(card => {
            const product = products.get(card.dataset.productId.toLowerCase());
            const available = product ? Math.max(0, product.available) : 0;
            const badge = card.querySelector('.product-card-status .badge');
            badge.className = 'badge ' + (available <= 0 ? 'badge-soft-danger'
                : available <= product.minStock ? 'badge-soft-warning' : 'badge-soft-success');
            badge.textContent = !product ? 'No disponible' : available <= 0 ? 'Agotado'
                : available <= product.minStock ? `Ultimas ${available} unidades` : 'Disponible';
            card.classList.toggle('product-card--sold-out', available <= 0);
            card.querySelector('.product-card-stock').textContent = `${available} disponibles`;
            const form = card.querySelector('.product-card-buy');
            form.classList.toggle('d-none', available <= 0);
            form.querySelector('button').disabled = available <= 0;
            const quantity = form.querySelector('[name="quantity"]');
            quantity.max = Math.max(1, available);
            if (Number(quantity.value) > available) quantity.value = Math.max(1, available);
            card.querySelector('[data-sold-out]').classList.toggle('d-none', available > 0);
        });
    }

    async function refresh() {
        pending = true;
        if (refreshing) return;
        refreshing = true;
        try {
            while (pending) {
                pending = false;
                const response = await fetch('/Products/Availability' + window.location.search, {
                    cache: 'no-store', headers: { Accept: 'application/json' }
                });
                if (!response.ok) throw new Error('availability');
                render(await response.json());
            }
            status.textContent = connection.state === signalR.HubConnectionState.Connected
                ? 'Disponibilidad en vivo. Actualizada a las ' + new Date().toLocaleTimeString()
                : 'Conexion interrumpida. La disponibilidad puede cambiar.';
        } catch {
            status.textContent = 'No se pudo actualizar la disponibilidad. Reintentando...';
            clearTimeout(retryTimer);
            retryTimer = setTimeout(refresh, 5000);
        } finally { refreshing = false; }
    }

    connection.on('StockChanged', refresh);
    connection.onreconnecting(() => {
        status.textContent = 'Reconectando. La disponibilidad mostrada puede estar desactualizada.';
    });
    connection.onreconnected(refresh);
    connection.onclose(() => {
        status.textContent = 'Sin actualizaciones en vivo. Recarga la pagina para reconectar.';
    });
    async function start() {
        try {
            await connection.start();
            await refresh();
        } catch {
            status.textContent = 'Conectando disponibilidad en vivo. Reintentando...';
            setTimeout(start, 5000);
        }
    }
    // Respaldo ante un evento perdido sin desconexion visible del navegador.
    setInterval(refresh, 30000);
    document.addEventListener('visibilitychange', () => { if (!document.hidden) void refresh(); });
    void start();
})();
