using BioGamaEcuador.Data;
using BioGamaEcuador.Hubs;
using BioGamaEcuador.Models.Sales;
using BioGamaEcuador.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace BioGamaEcuador.Tests;

public sealed class InventoryTests : IDisposable
{
    private readonly AppDbContext db = new(new DbContextOptionsBuilder<AppDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
    private readonly Mock<IClientProxy> proxy = new();
    private readonly InventoryMovementService inventory;
    private readonly PhysicalProduct product = new() { Name = "Kit", SKU = "KIT", Price = 10, Stock = 10 };

    public InventoryTests()
    {
        var hub = new Mock<IHubContext<StockHub>>();
        hub.Setup(h => h.Clients.All).Returns(proxy.Object);
        proxy.Setup(p => p.SendCoreAsync(It.IsAny<string>(), It.IsAny<object?[]>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        inventory = new InventoryMovementService(db, new StockNotifications(hub.Object, NullLogger<StockNotifications>.Instance));
        db.PhysicalProducts.Add(product);
        db.SaveChanges();
    }

    [Theory]
    [InlineData("Entrada", 0)] [InlineData("Entrada", -2)]
    [InlineData("Salida", 0)] [InlineData("Salida", -2)]
    [InlineData("Reserva", 0)] [InlineData("Reserva", -2)]
    [InlineData("Devolucion", 0)] [InlineData("Devolucion", -2)]
    public async Task Invalid_quantities_do_not_change_stock_or_create_movements(string operation, int quantity)
    {
        Func<Task> action = operation switch
        {
            "Entrada" => () => inventory.EntryAsync(product.Id, quantity, "test", "user"),
            "Salida" => () => inventory.ExitAsync(product.Id, quantity, "test", "user"),
            "Reserva" => () => inventory.ReserveAsync(product.Id, quantity, "test", "user"),
            _ => () => inventory.ReturnAsync(product.Id, quantity, "test", "user")
        };
        await Assert.ThrowsAsync<InvalidOperationException>(action);
        Assert.Equal(10, product.Stock);
        Assert.Equal(0, product.ReservedStock);
        Assert.Empty(db.InventoryMovements);
        proxy.Verify(p => p.SendCoreAsync(It.IsAny<string>(), It.IsAny<object?[]>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Reservation_confirmation_and_release_preserve_available_stock()
    {
        await inventory.ReserveAsync(product.Id, 4, "cart", "user");
        Assert.Equal(6, product.AvailableStock);
        await inventory.ConfirmAsync(product.Id, 2, "payment", "user");
        Assert.Equal(8, product.Stock);
        Assert.Equal(2, product.ReservedStock);
        Assert.Equal(6, product.AvailableStock);
        await inventory.ReleaseAsync(product.Id, 2, "cancel", "user");
        Assert.Equal(8, product.AvailableStock);
        Assert.Equal(3, await db.InventoryMovements.CountAsync());
    }

    [Fact]
    public async Task Cannot_reserve_or_exit_more_than_available()
    {
        await inventory.ReserveAsync(product.Id, 8, "cart", "user");
        await Assert.ThrowsAsync<InvalidOperationException>(() => inventory.ReserveAsync(product.Id, 3, "cart", "user"));
        await Assert.ThrowsAsync<InvalidOperationException>(() => inventory.ExitAsync(product.Id, 3, "exit", "user"));
        Assert.Equal(10, product.Stock);
        Assert.Equal(8, product.ReservedStock);
    }

    [Fact]
    public async Task Cannot_adjust_below_reserved_or_to_negative_stock()
    {
        await inventory.ReserveAsync(product.Id, 3, "cart", "user");
        await Assert.ThrowsAsync<InvalidOperationException>(() => inventory.AdjustAsync(product.Id, 2, "adjust", "user", "test"));
        await Assert.ThrowsAsync<InvalidOperationException>(() => inventory.AdjustAsync(product.Id, -1, "adjust", "user", "test"));
        Assert.Equal(10, product.Stock);
    }

    [Fact]
    public async Task Entry_and_exit_record_stock_and_notify_after_saving()
    {
        await inventory.EntryAsync(product.Id, 5, "entry", "user");
        Assert.Equal(15, product.Stock);
        await inventory.ExitAsync(product.Id, 2, "exit", "user");
        Assert.Equal(13, product.Stock);
        var movements = await db.InventoryMovements.ToListAsync();
        Assert.Contains(movements, m => m.StockAnterior == 10 && m.StockPosterior == 15);
        Assert.Contains(movements, m => m.StockAnterior == 15 && m.StockPosterior == 13);
        proxy.Verify(p => p.SendCoreAsync("StockChanged", It.IsAny<object?[]>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task Notification_failure_does_not_undo_committed_inventory()
    {
        proxy.Setup(p => p.SendCoreAsync(It.IsAny<string>(), It.IsAny<object?[]>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromException(new IOException("Redis unavailable")));
        await inventory.EntryAsync(product.Id, 1, "entry", "user");
        db.ChangeTracker.Clear();
        Assert.Equal(11, (await db.PhysicalProducts.SingleAsync()).Stock);
        Assert.Single(db.InventoryMovements);
    }

    [Fact]
    public async Task Empty_and_unknown_product_are_rejected()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() => inventory.EntryAsync(Guid.Empty, 1, "entry", "user"));
        await Assert.ThrowsAsync<KeyNotFoundException>(() => inventory.EntryAsync(Guid.NewGuid(), 1, "entry", "user"));
    }

    public void Dispose() => db.Dispose();
}
