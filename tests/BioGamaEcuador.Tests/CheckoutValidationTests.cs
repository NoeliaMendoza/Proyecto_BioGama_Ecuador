using System.Security.Claims;
using BioGamaEcuador.Controllers;
using BioGamaEcuador.Data;
using BioGamaEcuador.Models.Sales;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BioGamaEcuador.Tests;

public sealed class CheckoutValidationTests : IDisposable
{
    private readonly TestApplication application = new();

    [Theory]
    [InlineData("123")]
    [InlineData("not-a-phone")]
    [InlineData("+59399999999999999999")]
    public async Task Invalid_phone_prevents_payment_creation(string phone)
    {
        using var client = application.NewClient();
        await using var scope = application.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var order = new Order { UserId = "test-user", Total = 10 };
        db.Orders.Add(order); await db.SaveChangesAsync();
        var controller = Controller(scope.ServiceProvider);
        var result = await controller.Checkout(order.Id, "PayPal", null, phone, null, null, null, null);
        Assert.IsType<ViewResult>(result);
        Assert.False(controller.ModelState.IsValid);
        Assert.Empty(db.Payments);
    }

    [Theory]
    [InlineData("0999999999")]
    [InlineData("+593999999999")]
    [InlineData("999999999")]
    public async Task Valid_phone_creates_pending_payment_and_redirects_without_charging(string phone)
    {
        using var client = application.NewClient();
        await using var scope = application.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var order = new Order { UserId = "test-user", Total = 10 };
        db.Orders.Add(order); await db.SaveChangesAsync();
        var controller = Controller(scope.ServiceProvider);
        var result = Assert.IsType<RedirectToActionResult>(await controller.Checkout(order.Id, "PayPal", null,
            phone, "Direccion", null, null, null));
        Assert.Equal("PayPalButton", result.ActionName);
        var payment = await db.Payments.SingleAsync();
        Assert.Equal("Pending", payment.Status);
        Assert.Equal(10, payment.Amount);
        Assert.Equal("Direccion", order.ShippingAddress);
    }

    [Fact]
    public async Task Checkout_of_another_users_order_returns_not_found()
    {
        using var client = application.NewClient();
        await using var scope = application.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var order = new Order { UserId = "another-user", Total = 10 };
        db.Orders.Add(order); await db.SaveChangesAsync();
        Assert.IsType<NotFoundResult>(await Controller(scope.ServiceProvider).Checkout(order.Id, "PayPal", null,
            "0999999999", null, null, null, null));
        Assert.Empty(db.Payments);
    }

    private static OrdersController Controller(IServiceProvider services)
    {
        var controller = ActivatorUtilities.CreateInstance<OrdersController>(services);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, "test-user"), new Claim(ClaimTypes.Name, "tester@biogama.test")
                }, "test"))
            }
        };
        return controller;
    }

    public void Dispose() => application.Dispose();
}
