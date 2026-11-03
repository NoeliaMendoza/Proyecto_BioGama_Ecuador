using Microsoft.AspNetCore.SignalR;

namespace BioGamaEcuador.Hubs;

// Solo recepcion: el inventario se modifica mediante las acciones autorizadas.
public sealed class StockHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        await Clients.Caller.SendAsync("Connected", new
        {
            pod = Environment.GetEnvironmentVariable("HOSTNAME") ?? Environment.MachineName
        });
        await base.OnConnectedAsync();
    }
}
