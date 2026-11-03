using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BioGamaEcuador.Data;
using BioGamaEcuador.Models.Sales;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BioGamaEcuador.Tests;

public sealed class CatalogPageTests : IDisposable
{
    private readonly TestApplication application = new();

    [Theory]
    [InlineData(10, 3, "Disponible")]
    [InlineData(5, 3, "unidades")]
    [InlineData(3, 3, "Agotado")]
    public async Task Catalog_renders_available_units_and_purchase_controls(int stock, int reserved, string status)
    {
        using var client = application.NewClient(null);
        using (var scope = application.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var product = await db.PhysicalProducts.SingleAsync();
            product.Stock = stock;
            product.ReservedStock = reserved;
            await db.SaveChangesAsync();
        }

        var response = await client.GetAsync("/Products");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync();
        var available = stock - reserved;
        Assert.Contains($"data-product-id=\"{TestApplication.ProductId}\"", html);
        Assert.Contains($"data-available-total>{available}</span>", html);
        Assert.Contains($"max=\"{available}\"", html);
        Assert.Contains(status, html);
        Assert.Contains("name=\"__RequestVerificationToken\"", html);
        Assert.Contains("/js/product-stock.js", html);
        Assert.Equal(available == 0, html.Contains("product-card--sold-out", StringComparison.Ordinal));
        Assert.Equal(available == 0, html.Contains("product-card-buy d-none", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Inactive_and_deleted_products_are_hidden_from_page_and_live_availability()
    {
        using var client = application.NewClient(null);
        using (var scope = application.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            (await db.PhysicalProducts.SingleAsync()).IsActive = false;
            db.PhysicalProducts.Add(new PhysicalProduct
            {
                Name = "Deleted product", Stock = 99, Price = 10, DeletedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
        }

        var html = await client.GetStringAsync("/Products?page=-1");
        Assert.Contains("No encontramos productos", html);
        Assert.DoesNotContain("data-product-id=", html);
        Assert.Contains("data-available-total>0</span>", html);
        var json = await client.GetFromJsonAsync<JsonElement>("/Products/Availability?page=99");
        Assert.Equal(0, json.GetProperty("total").GetInt32());
        Assert.Empty(json.GetProperty("products").EnumerateArray());
    }

    [Theory]
    [InlineData(-10, 12)]
    [InlineData(99, 2)]
    public async Task Page_and_live_endpoint_agree_when_page_is_out_of_range(int page, int count)
    {
        using var client = application.NewClient(null);
        using (var scope = application.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.PhysicalProducts.RemoveRange(db.PhysicalProducts);
            db.PhysicalProducts.AddRange(Enumerable.Range(1, 14).Select(i => new PhysicalProduct
            {
                Name = $"Product {i:D2}", Price = 10, Stock = 2, ReservedStock = 1
            }));
            await db.SaveChangesAsync();
        }

        var json = await client.GetFromJsonAsync<JsonElement>($"/Products/Availability?page={page}");
        var html = await client.GetStringAsync($"/Products?page={page}");
        Assert.Equal(14, json.GetProperty("total").GetInt32());
        Assert.Contains("data-available-total>14</span>", html);
        var products = json.GetProperty("products").EnumerateArray().ToArray();
        Assert.Equal(count, products.Length);
        foreach (var product in products)
            Assert.Contains($"data-product-id=\"{product.GetProperty("id").GetGuid()}\"", html);
        Assert.Equal(count, html.Split("data-product-id=", StringSplitOptions.None).Length - 1);
    }

    public void Dispose() => application.Dispose();
}
