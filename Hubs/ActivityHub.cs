using Microsoft.AspNetCore.SignalR;

namespace BioGamaEcuador.Hubs;

public class ActivityHub : Hub
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
