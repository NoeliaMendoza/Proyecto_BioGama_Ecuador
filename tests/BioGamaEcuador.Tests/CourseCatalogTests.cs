using System.Net;
using BioGamaEcuador.Data;
using BioGamaEcuador.Models.Sales;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BioGamaEcuador.Tests;

public sealed class CourseCatalogTests
{
    [Fact]
    public async Task Stylesheets_are_versioned_individually_to_refresh_cached_components()
    {
        await using var application = new TestApplication();
        using var client = application.NewClient(null);
        var html = await client.GetStringAsync("/Courses");
        Assert.Matches("href=\"/css/components\\.css\\?v=[^\"]+\"", html);
        Assert.Matches("href=\"/css/layout\\.css\\?v=[^\"]+\"", html);
    }

    [Theory]
    [InlineData("abierto", "Abierto de prueba")]
    [InlineData("agotado", "Agotado de prueba")]
    [InlineData("finalizado", "Finalizado de prueba")]
    [InlineData("en-curso", "En curso de prueba")]
    public async Task Status_filters_distinguish_dates_from_full_capacity(string filter, string expectedTitle)
    {
        await using var application = new TestApplication();
        using var scope = application.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var today = DateTime.UtcNow.Date;
        db.Courses.AddRange(
            Create("Abierto de prueba", today.AddDays(3), today.AddDays(4)),
            Create("Agotado de prueba", today.AddDays(1), today.AddDays(2), full: true),
            Create("Finalizado de prueba", today.AddDays(-3), today.AddDays(-1)),
            Create("En curso de prueba", today.AddDays(-1), today.AddDays(1)));
        await db.SaveChangesAsync();
        using var client = application.NewClient(null);
        var response = await client.GetAsync("/Courses?estado=" + filter);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync();
        var titles = new[] { "Abierto de prueba", "Agotado de prueba", "Finalizado de prueba", "En curso de prueba" };
        foreach (var title in titles)
            if (title == expectedTitle) Assert.Contains(title, html);
            else Assert.DoesNotContain(title, html);
        Assert.DoesNotContain("btn btn-primary btn-sm disabled", html);
        if (filter == "finalizado")
        {
            Assert.Contains("badge-soft-secondary\">Finalizado</span>", html);
            Assert.DoesNotContain("badge-soft-danger\">Agotado</span>", html);
        }
    }

    private static Course Create(string title, DateTime start, DateTime end, bool full = false) => new()
    {
        Title = title, StartDate = start, EndDate = end, TotalSeats = 20,
        ConfirmedSeats = full ? 20 : 0, IsActive = true
    };
}
