using Microsoft.AspNetCore.Identity;

namespace BioGamaEcuador.Data;

public static class IdentitySeeder
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = serviceProvider.GetRequiredService<UserManager<IdentityUser>>();
        var configuration = serviceProvider.GetRequiredService<IConfiguration>();
        var logger = serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(IdentitySeeder));

        string[] roles = { "Administrador", "Investigador", "UsuarioPublico" };
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
                EnsureSuccess(await roleManager.CreateAsync(new IdentityRole(role)), "crear un rol inicial");
        }

        await CreateAccountAsync("admin@biogama.ec", "Administrador", "Admin");
        await CreateAccountAsync("investigador@biogama.ec", "Investigador", "Researcher");
        await CreateAccountAsync("usuario@biogama.ec", "UsuarioPublico", "Public");

        var additionalEmail = configuration["SeedUsers:AdditionalAdminEmail"];
        if (!string.IsNullOrWhiteSpace(additionalEmail))
            await CreateAccountAsync(additionalEmail, "Administrador", "AdditionalAdmin");

        async Task CreateAccountAsync(string email, string role, string accountKey)
        {
            // No cambia claves ni privilegios de cuentas existentes.
            if (await userManager.FindByEmailAsync(email) is not null) return;

            var password = configuration[$"SeedUsers:{accountKey}Password"];
            if (string.IsNullOrWhiteSpace(password))
            {
                logger.LogWarning("Cuenta inicial {AccountKey} omitida: no se configuro su clave", accountKey);
                return;
            }

            var user = new IdentityUser { UserName = email, Email = email, EmailConfirmed = true };
            EnsureSuccess(await userManager.CreateAsync(user, password), "crear una cuenta inicial");
            EnsureSuccess(await userManager.AddToRoleAsync(user, role), "asignar un rol inicial");
        }
    }

    private static void EnsureSuccess(IdentityResult result, string operation)
    {
        if (!result.Succeeded)
            throw new InvalidOperationException($"No se pudo {operation}: {string.Join(", ", result.Errors.Select(e => e.Code))}");
    }
}
