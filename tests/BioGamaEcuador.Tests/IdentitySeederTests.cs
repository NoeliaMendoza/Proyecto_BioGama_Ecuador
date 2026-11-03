using BioGamaEcuador.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BioGamaEcuador.Tests;

public sealed class IdentitySeederTests
{
    private static ServiceProvider Services(Dictionary<string, string?>? values = null)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().AddInMemoryCollection(values ?? []).Build());
        var database = Guid.NewGuid().ToString();
        services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase(database));
        services.AddIdentityCore<IdentityUser>().AddRoles<IdentityRole>().AddEntityFrameworkStores<AppDbContext>();
        return services.BuildServiceProvider();
    }

    [Fact]
    public async Task Missing_passwords_create_roles_but_no_accounts_with_default_credentials()
    {
        await using var services = Services();
        await using var scope = services.CreateAsyncScope();
        await IdentitySeeder.SeedAsync(scope.ServiceProvider);
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Empty(db.Users);
        Assert.Equal(3, await db.Roles.CountAsync());
        await IdentitySeeder.SeedAsync(scope.ServiceProvider);
        Assert.Equal(3, await db.Roles.CountAsync());
    }

    [Fact]
    public async Task Configured_accounts_receive_only_their_expected_roles()
    {
        var secret = "T-" + Guid.NewGuid().ToString("N") + "aA1!";
        await using var services = Services(new()
        {
            ["SeedUsers:AdminPassword"] = secret,
            ["SeedUsers:ResearcherPassword"] = secret,
            ["SeedUsers:PublicPassword"] = secret,
            ["SeedUsers:AdditionalAdminEmail"] = "extra@example.test",
            ["SeedUsers:AdditionalAdminPassword"] = secret
        });
        await using var scope = services.CreateAsyncScope();
        await IdentitySeeder.SeedAsync(scope.ServiceProvider);
        var users = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
        foreach (var expected in new[] { ("admin@biogama.ec", "Administrador"), ("investigador@biogama.ec", "Investigador"),
            ("usuario@biogama.ec", "UsuarioPublico"), ("extra@example.test", "Administrador") })
        {
            var user = await users.FindByEmailAsync(expected.Item1);
            Assert.NotNull(user);
            Assert.True(await users.CheckPasswordAsync(user, secret));
            Assert.Equal(new[] { expected.Item2 }, await users.GetRolesAsync(user));
        }
    }

    [Fact]
    public async Task Existing_account_password_and_privileges_are_not_overwritten()
    {
        var originalSecret = "T-" + Guid.NewGuid().ToString("N") + "aA1!";
        await using var services = Services(new() { ["SeedUsers:AdminPassword"] = "a-new-value-that-must-not-be-used" });
        await using var scope = services.CreateAsyncScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
        var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        await roles.CreateAsync(new IdentityRole("UsuarioPublico"));
        var existing = new IdentityUser { UserName = "admin@biogama.ec", Email = "admin@biogama.ec" };
        Assert.True((await users.CreateAsync(existing, originalSecret)).Succeeded);
        await users.AddToRoleAsync(existing, "UsuarioPublico");
        await IdentitySeeder.SeedAsync(scope.ServiceProvider);
        Assert.True(await users.CheckPasswordAsync(existing, originalSecret));
        Assert.False(await users.IsInRoleAsync(existing, "Administrador"));
    }

    [Fact]
    public async Task Invalid_configured_password_fails_explicitly_instead_of_creating_broken_account()
    {
        await using var services = Services(new() { ["SeedUsers:AdminPassword"] = "x" });
        await using var scope = services.CreateAsyncScope();
        await Assert.ThrowsAsync<InvalidOperationException>(() => IdentitySeeder.SeedAsync(scope.ServiceProvider));
        Assert.Empty(scope.ServiceProvider.GetRequiredService<AppDbContext>().Users);
    }
}
