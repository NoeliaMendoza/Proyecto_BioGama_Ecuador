using System.Text.Json;
using BioGamaEcuador.Models;
using BioGamaEcuador.Models.Sales;
using Microsoft.EntityFrameworkCore;

namespace BioGamaEcuador.Data.Seeders;

public static class SalesModuleSeeder
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        if (await context.Courses.AnyAsync()) return;

        var species = await context.Species.Where(value => value.IsActive).Take(10).ToListAsync();
        using var source = typeof(SalesModuleSeeder).Assembly.GetManifestResourceStream(
            "BioGamaEcuador.Data.Seeders.sales-catalog.json")
            ?? throw new InvalidOperationException("No se encontró el catálogo inicial de ventas.");
        var catalog = await JsonSerializer.DeserializeAsync<SeedCatalog>(source)
            ?? throw new InvalidOperationException("El catálogo inicial de ventas está vacío.");
        var now = DateTime.UtcNow;
        foreach (var entry in catalog.Courses)
        {
            var course = entry.Value;
            course.StartDate = now.AddDays(entry.StartAfterDays);
            course.EndDate = now.AddDays(entry.EndAfterDays);
            course.CreatedAt = now;
            course.SpeciesId = FindSpecies(species, entry.SpeciesReference);
            context.Courses.Add(course);
        }
        foreach (var entry in catalog.Products)
        {
            var product = entry.Value;
            product.CreatedAt = now;
            product.SpeciesId = FindSpecies(species, entry.SpeciesReference);
            context.PhysicalProducts.Add(product);
        }
        await context.SaveChangesAsync();
    }

    private static int? FindSpecies(List<Species> species, string? reference) => reference switch
    {
        "colibri" => species.Find(value => value.CommonName.Contains("Colibrí"))?.Id,
        "orquidea" => species.Find(value => value.CommonName.Contains("Orquídea"))?.Id,
        "rana" => species.Find(value => value.CommonName.Contains("Rana") || value.CommonName.Contains("rana"))?.Id,
        _ => null
    };

    private sealed class SeedCatalog
    {
        public List<CourseEntry> Courses { get; set; } = [];
        public List<ProductEntry> Products { get; set; } = [];
    }

    private sealed class CourseEntry
    {
        public Course Value { get; set; } = new();
        public int StartAfterDays { get; set; }
        public int EndAfterDays { get; set; }
        public string? SpeciesReference { get; set; }
    }

    private sealed class ProductEntry
    {
        public PhysicalProduct Value { get; set; } = new();
        public string? SpeciesReference { get; set; }
    }
}
