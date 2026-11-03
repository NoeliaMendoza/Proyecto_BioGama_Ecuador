using System.Net;
using BioGamaEcuador.Hubs;
using Microsoft.AspNetCore.SignalR;
using Moq;
using Xunit;

namespace BioGamaEcuador.Tests;

public sealed class HubAndHealthTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Connection_metadata_is_sent_only_to_the_connecting_client(bool stock)
    {
        var proxy = new Mock<ISingleClientProxy>();
        object? payload = null;
        proxy.Setup(p => p.SendCoreAsync("Connected", It.IsAny<object?[]>(), It.IsAny<CancellationToken>()))
            .Callback<string, object?[], CancellationToken>((_, args, _) => payload = args[0])
            .Returns(Task.CompletedTask);
        var clients = new Mock<IHubCallerClients>();
        clients.SetupGet(c => c.Caller).Returns(proxy.Object);
        using Hub hub = stock ? new StockHub() : new ActivityHub();
        hub.Clients = clients.Object;
        await hub.OnConnectedAsync();
        Assert.NotNull(payload);
        var pod = payload.GetType().GetProperty("pod")!.GetValue(payload) as string;
        Assert.False(string.IsNullOrWhiteSpace(pod));
        proxy.Verify(p => p.SendCoreAsync("Connected", It.IsAny<object?[]>(), It.IsAny<CancellationToken>()), Times.Once);
        clients.VerifyGet(c => c.All, Times.Never);
    }

    [Fact]
    public async Task Health_and_readiness_respond_when_the_database_is_available()
    {
        await using var application = new TestApplication();
        using var client = application.NewClient(null);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health/ready")).StatusCode);
        var info = await client.GetStringAsync("/api/info");
        Assert.Contains("BioGamaEcuador", info);
    }
}
