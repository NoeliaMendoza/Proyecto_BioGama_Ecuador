using System.Security.Cryptography;
using System.Text;
using BioGamaEcuador.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Xunit;
using Xunit.Abstractions;

namespace BioGamaEcuador.Tests;

public sealed class MigrationSqlContractTests(ITestOutputHelper output)
{
    [Fact]
    public void Migration_scripts_preserve_the_database_contract()
    {
        AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
        using var database = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql("Host=localhost;Database=unused;Username=unused").Options);
        var migrator = database.GetService<IMigrator>();
        var forward = migrator.GenerateScript(options: MigrationsSqlGenerationOptions.Idempotent);
        var last = database.Database.GetMigrations().Last();
        var backward = migrator.GenerateScript(fromMigration: last, toMigration: "0");
        Assert.NotEmpty(forward);
        Assert.NotEmpty(backward);
        output.WriteLine("FORWARD_SHA256=" + Hash(forward));
        output.WriteLine("BACKWARD_SHA256=" + Hash(backward));
        Assert.Equal("70755EB68672891B89A9A796FD2F5845A759E88F644AA4982F33B846A40C91E1", Hash(forward));
        Assert.Equal("D73E3F48C7A6D654ED331E9F1DCE50D09FE819AD075488260944413AA0CAE698", Hash(backward));
    }

    // El generador de EF usa Environment.NewLine; se normaliza para obtener el mismo hash en Windows y Linux.
    private static string Hash(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value.ReplaceLineEndings("\n"))));
}
