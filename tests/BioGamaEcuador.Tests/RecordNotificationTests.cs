using System.Security.Claims;
using BioGamaEcuador.Controllers;
using BioGamaEcuador.Data;
using BioGamaEcuador.Hubs;
using BioGamaEcuador.Models;
using BioGamaEcuador.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;
using FieldRecord = BioGamaEcuador.Models.Record;

namespace BioGamaEcuador.Tests;

public sealed class RecordNotificationTests : IDisposable
{
    private readonly AppDbContext db = new(new DbContextOptionsBuilder<AppDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
    private readonly Mock<IClientProxy> proxy = new();

    private RecordsController Controller(string role = "Administrador")
    {
        var hub = new Mock<IHubContext<ActivityHub>>();
        hub.Setup(h => h.Clients.All).Returns(proxy.Object);
        proxy.Setup(p => p.SendCoreAsync(It.IsAny<string>(), It.IsAny<object?[]>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        return new RecordsController(db, Mock.Of<IAuditService>(), hub.Object, NullLogger<RecordsController>.Instance)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.Name, "researcher@example.test"), new Claim(ClaimTypes.Role, role)
                }, "test")) }
            }
        };
    }

    [Fact]
    public async Task Creating_record_persists_utc_dates_and_notifies_clients()
    {
        var controller = Controller();
        var record = new FieldRecord { ObservedQuantity = 2, DiscoveryDate = new DateTime(2026, 1, 2) };
        Assert.IsType<RedirectToActionResult>(await controller.Create(record));
        Assert.Single(db.Records);
        Assert.Equal(DateTimeKind.Utc, record.DiscoveryDate.Kind);
        proxy.Verify(p => p.SendCoreAsync("Activity", It.IsAny<object?[]>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Notification_error_does_not_discard_saved_field_record()
    {
        var controller = Controller();
        proxy.Setup(p => p.SendCoreAsync(It.IsAny<string>(), It.IsAny<object?[]>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromException(new IOException("Notifications unavailable")));
        Assert.IsType<RedirectToActionResult>(await controller.Create(new FieldRecord { ObservedQuantity = 1 }));
        Assert.Single(db.Records);
    }

    [Fact]
    public async Task Investigator_without_registered_profile_cannot_create_record()
    {
        var controller = Controller("Investigador");
        Assert.IsType<ForbidResult>(await controller.Create(new FieldRecord { ObservedQuantity = 1 }));
        Assert.Empty(db.Records);
    }

    [Fact]
    public async Task Investigator_cannot_assign_record_to_another_researcher()
    {
        var researcher = new Researcher { Name = "Test", Email = "researcher@example.test" };
        db.Researchers.Add(researcher); await db.SaveChangesAsync();
        var controller = Controller("Investigador");
        var record = new FieldRecord { ResearcherId = researcher.Id + 100, ObservedQuantity = 1 };
        await controller.Create(record);
        Assert.Equal(researcher.Id, record.ResearcherId);
    }

    public void Dispose() => db.Dispose();
}
