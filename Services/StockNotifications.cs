using BioGamaEcuador.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace BioGamaEcuador.Services;

public sealed class StockNotifications(IHubContext<StockHub> hub, ILogger<StockNotifications> logger)
{
    // Llamar despues del commit: una notificacion fallida no revierte una compra.
    public async Task PublishAsync()
    {
        try
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(3));
            await hub.Clients.All.SendAsync("StockChanged", new
            {
                id = Guid.NewGuid().ToString("N"),
                timestamp = DateTime.UtcNow,
                pod = Environment.GetEnvironmentVariable("HOSTNAME") ?? Environment.MachineName
            }, timeout.Token);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Inventario guardado; no se pudo notificar el cambio de disponibilidad");
        }
    }
}
