using System.ComponentModel.DataAnnotations;
using BioGamaEcuador.Areas.Identity.Pages.Account.Manage;
using BioGamaEcuador.Data;
using BioGamaEcuador.Data.Seeders;
using BioGamaEcuador.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BioGamaEcuador.Tests;

public sealed class DuplicationContractTests
{
    private static readonly decimal[] ExpectedPrices = [60m, 95m, 120m, 180m];
    private static readonly int[] ExpectedStocks = [50, 30, 20];

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Both_password_pages_preserve_validation_and_confirmation(bool setPassword)
    {
        NewPasswordInputModel model = setPassword
            ? new SetPasswordModel.InputModel()
            : new ChangePasswordModel.InputModel { OldPassword = "Old-password-1!" };
        model.NewPassword = "short";
        model.ConfirmPassword = "different";
        var errors = new List<ValidationResult>();
        Assert.False(Validator.TryValidateObject(model, new ValidationContext(model), errors, true));
        Assert.Contains(errors, error => error.MemberNames.Contains("NewPassword"));
        Assert.Contains(errors, error => error.MemberNames.Contains("ConfirmPassword"));
        model.NewPassword = "Valid-password-2!";
        model.ConfirmPassword = model.NewPassword;
        Assert.True(Validator.TryValidateObject(model, new ValidationContext(model), [], true));
    }

    [Fact]
    public async Task Embedded_catalog_preserves_courses_products_links_and_seed_idempotence()
    {
        var databaseName = Guid.NewGuid().ToString();
        await using var services = new ServiceCollection()
            .AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase(databaseName))
            .BuildServiceProvider();
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Species.AddRange(
            new Species { Id = 31, CommonName = "Colibrí", FamilyId = 1, IsActive = true },
            new Species { Id = 32, CommonName = "Orquídea", FamilyId = 1, IsActive = true },
            new Species { Id = 33, CommonName = "Rana", FamilyId = 1, IsActive = true });
        await db.SaveChangesAsync();
        await SalesModuleSeeder.SeedAsync(services);
        await SalesModuleSeeder.SeedAsync(services);
        var courses = await db.Courses.OrderBy(value => value.Price).ToListAsync();
        Assert.Equal(4, courses.Count);
        Assert.Equal(ExpectedPrices, courses.Select(value => value.Price));
        Assert.All(courses, value => Assert.Equal(DateTimeKind.Utc, value.StartDate.Kind));
        var birds = Assert.Single(courses, value => value.SpeciesId == 31);
        Assert.Equal("Identificación de Aves del Chocó Andino", birds.Title);
        Assert.Equal(20, birds.TotalSeats);
        Assert.Equal(TimeSpan.FromHours(8), birds.StartTime);
        Assert.Equal(1, (birds.EndDate - birds.StartDate).TotalDays);
        Assert.Contains("Día 2", birds.Syllabus);
        var products = await db.PhysicalProducts.OrderBy(value => value.Price).ToListAsync();
        Assert.Equal(3, products.Count);
        Assert.Equal(ExpectedStocks, products.Select(value => value.Stock));
        Assert.Equal("GUI-ORQ-AN-001", products[1].SKU);
        Assert.Equal(32, products[1].SpeciesId);
    }
}
