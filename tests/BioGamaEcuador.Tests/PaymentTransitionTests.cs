using System.Net;
using System.Net.Http.Json;
using BioGamaEcuador.Data;
using BioGamaEcuador.Models.Sales;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BioGamaEcuador.Tests;

// Pruebas de estado e idempotencia, no de transacciones ni concurrencia PostgreSQL.
public sealed class PaymentTransitionTests
{
    [Theory]
    [InlineData(3, "Approved", "Confirmed", 8)]
    [InlineData(2, "Failed", "Cancelled", 10)]
    public async Task Verified_terminal_payment_updates_stock_once_even_when_webhook_is_repeated(
        int providerStatus, string paymentStatus, string orderStatus, int remainingStock)
    {
        await using var application = new TestApplication { SimulatePaymentTransactions = true };
        application.PayPhoneResponse = System.Text.Json.JsonSerializer.Serialize(new
        {
            clientTransactionId = "verified-test", statusCode = providerStatus, amount = 1000, currency = "USD"
        });
        using var client = application.NewClient(null);
        using (var scope = application.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            (await db.PhysicalProducts.SingleAsync()).ReservedStock = 2;
            var order = new Order { UserId = "test-user", Total = 10 };
            order.Details.Add(new OrderDetail { PhysicalProductId = TestApplication.ProductId, Quantity = 2, UnitPrice = 5 });
            db.Payments.Add(new Payment { Order = order, Provider = "PayPhone", ExternalId = "verified-test", Amount = 10 });
            await db.SaveChangesAsync();
        }

        // El estado declarado por el remitente contradice al proveedor simulado.
        for (var repeat = 0; repeat < 2; repeat++)
        {
            var response = await client.PostAsJsonAsync("/api/payments/payphone/webhook", new
            {
                clientTransactionId = "verified-test", status = providerStatus == 3 ? "FAILED" : "APPROVED"
            });
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        using var verification = application.Services.CreateScope();
        var saved = verification.ServiceProvider.GetRequiredService<AppDbContext>();
        var payment = await saved.Payments.SingleAsync();
        Assert.Equal(paymentStatus, payment.Status);
        Assert.Equal(providerStatus == 3, payment.ConfirmedAt.HasValue);
        Assert.Equal(orderStatus, (await saved.Orders.SingleAsync()).Status);
        var product = await saved.PhysicalProducts.SingleAsync();
        Assert.Equal(remainingStock, product.Stock);
        Assert.Equal(0, product.ReservedStock);
        var movement = await saved.InventoryMovements.SingleAsync();
        Assert.Equal(providerStatus == 3 ? "Confirmacion" : "Liberacion", movement.TipoMovimiento);
    }
}
