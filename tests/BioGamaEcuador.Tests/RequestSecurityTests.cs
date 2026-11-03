using System.Net;
using System.Text;
using System.Net.Http.Json;
using BioGamaEcuador.Data;
using BioGamaEcuador.Models.Sales;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BioGamaEcuador.Tests;

public sealed class RequestSecurityTests : IDisposable
{
    private readonly TestApplication application = new();

    private static async Task<string> Token(HttpClient client)
    {
        var tokens = await client.GetFromJsonAsync<Dictionary<string, string>>("/_test/antiforgery");
        return tokens!["token"];
    }

    [Theory]
    [InlineData("CreatePayPalOrder")]
    [InlineData("CapturePayPalOrder")]
    public async Task Payment_requests_without_antiforgery_token_are_rejected(string action)
    {
        using var client = application.NewClient();
        var response = await client.PostAsync($"/Orders/{action}", new StringContent("{}", Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Manual_payment_approval_requires_antiforgery_even_for_admin()
    {
        using var client = application.NewClient();
        var response = await client.PostAsync("/Payment/MarkAsPaid", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["id"] = Guid.NewGuid().ToString()
        }));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData("CreatePayPalOrder", "{}")]
    [InlineData("CreatePayPalOrder", "null")]
    [InlineData("CreatePayPalOrder", "{\"orderId\":\"00000000-0000-0000-0000-000000000000\"}")]
    [InlineData("CapturePayPalOrder", "{}")]
    [InlineData("CapturePayPalOrder", "{\"payPalOrderId\":\"\"}")]
    [InlineData("CapturePayPalOrder", "null")]
    public async Task Incomplete_payment_requests_are_rejected_even_with_valid_csrf(string action, string json)
    {
        using var client = application.NewClient();
        client.DefaultRequestHeaders.Add("RequestVerificationToken", await Token(client));
        var response = await client.PostAsync($"/Orders/{action}", new StringContent(json, Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Adjustment_without_new_stock_cannot_reset_inventory_to_zero()
    {
        using var client = application.NewClient();
        var token = await Token(client);
        var response = await client.PostAsync("/Admin/Inventory/Adjustment", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["ProductId"] = TestApplication.ProductId.ToString(),
            ["Quantity"] = "1", ["Reference"] = "incomplete-adjustment", ["Notes"] = "test"
        }));
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        using var scope = application.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Equal(10, (await db.PhysicalProducts.SingleAsync()).Stock);
        Assert.Empty(db.InventoryMovements);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("UsuarioPublico")]
    public async Task Inventory_is_not_accessible_without_admin_role(string? role)
    {
        using var client = application.NewClient(role);
        var response = await client.GetAsync("/Admin/Inventory");
        Assert.Contains(response.StatusCode, new[] { HttpStatusCode.Unauthorized, HttpStatusCode.Forbidden });
    }

    [Fact]
    public async Task Public_availability_reflects_reserved_units()
    {
        using var client = application.NewClient(null);
        using (var scope = application.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            (await db.PhysicalProducts.SingleAsync()).ReservedStock = 3;
            await db.SaveChangesAsync();
        }
        var response = await client.GetAsync("/Products/Availability");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = System.Text.Json.JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(7, json.RootElement.GetProperty("total").GetInt32());
        Assert.Equal(7, json.RootElement.GetProperty("products")[0].GetProperty("available").GetInt32());
    }

    [Theory]
    [InlineData(1, 1000, "USD", "verified-test", HttpStatusCode.Accepted)]
    [InlineData(3, 1, "USD", "verified-test", HttpStatusCode.BadRequest)]
    [InlineData(3, 1000, "EUR", "verified-test", HttpStatusCode.BadRequest)]
    [InlineData(3, 1000, "USD", "other-id", HttpStatusCode.BadRequest)]
    public async Task Forged_webhook_cannot_approve_an_unverified_payment(int status, int cents, string currency,
        string verifiedId, HttpStatusCode expected)
    {
        application.PayPhoneResponse = System.Text.Json.JsonSerializer.Serialize(new
        {
            clientTransactionId = verifiedId, statusCode = status, amount = cents, currency
        });
        using var client = application.NewClient(null);
        await SeedPendingPayment();
        var response = await client.PostAsJsonAsync("/api/payments/payphone/webhook", new
        {
            clientTransactionId = "verified-test", status = "APPROVED", amount = 10, currency = "USD"
        });
        Assert.Equal(expected, response.StatusCode);
        await AssertPaymentStillPending();
    }

    [Fact]
    public async Task Provider_unavailable_does_not_approve_payment()
    {
        application.PayPhoneHttpStatus = HttpStatusCode.ServiceUnavailable;
        using var client = application.NewClient(null);
        await SeedPendingPayment();
        var response = await client.PostAsJsonAsync("/api/payments/payphone/webhook", new
        {
            clientTransactionId = "verified-test", status = "APPROVED"
        });
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        await AssertPaymentStillPending();
    }

    [Theory]
    [InlineData("null")]
    [InlineData("{}")]
    [InlineData("not-json")]
    [InlineData("{\"clientTransactionId\":\"verified-test\",\"statusCode\":3,\"amount\":1000}")]
    public async Task Malformed_provider_reply_never_approves_payment(string reply)
    {
        application.PayPhoneResponse = reply;
        using var client = application.NewClient(null);
        await SeedPendingPayment();
        var response = await client.PostAsJsonAsync("/api/payments/payphone/webhook", new
        {
            clientTransactionId = "verified-test", status = "APPROVED"
        });
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        await AssertPaymentStillPending();
    }

    private async Task SeedPendingPayment()
    {
        using var scope = application.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var order = new Order { UserId = "test-user", Total = 10 };
        db.Payments.Add(new Payment { Order = order, Provider = "PayPhone", ExternalId = "verified-test", Amount = 10 });
        await db.SaveChangesAsync();
    }

    private async Task AssertPaymentStillPending()
    {
        using var scope = application.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Equal("Pending", (await db.Payments.SingleAsync()).Status);
        Assert.Equal("Pending", (await db.Orders.SingleAsync()).Status);
        Assert.Empty(db.InventoryMovements);
    }

    [Theory]
    [InlineData("Details")]
    [InlineData("CreateLink")]
    [InlineData("ConfirmPayPal")]
    public async Task Users_cannot_read_or_capture_someone_elses_payment(string action)
    {
        using var client = application.NewClient("UsuarioPublico");
        Guid orderId;
        Guid paymentId;
        using (var scope = application.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var order = new Order { UserId = "another-user", Total = 10 };
            var payment = new Payment { Order = order, Provider = "PayPal", ExternalId = "other-users-payment", Amount = 10 };
            db.Payments.Add(payment);
            await db.SaveChangesAsync();
            orderId = order.Id;
            paymentId = payment.Id;
        }
        var url = action switch
        {
            "Details" => $"/Payment/Details/{paymentId}",
            "CreateLink" => $"/Payment/CreateLink?orderId={orderId}",
            _ => "/api/payments/paypal/confirm?token_q=other-users-payment"
        };
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync(url)).StatusCode);
        await AssertPaymentStillPending();
    }

    public void Dispose() => application.Dispose();
}
