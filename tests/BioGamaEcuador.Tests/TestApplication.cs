using System.Security.Claims;
using System.Text.Encodings.Web;
using BioGamaEcuador.Data;
using BioGamaEcuador.Models.Sales;
using BioGamaEcuador.Services.Payments;
using System.Net;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BioGamaEcuador.Tests;

public sealed class TestApplication : WebApplicationFactory<Program>
{
    public static readonly Guid ProductId = Guid.Parse("4a621d65-8e4f-4e92-9eef-f8900288825e");
    private readonly string database = Guid.NewGuid().ToString();
    public string PayPhoneResponse { get; set; } = "{\"clientTransactionId\":\"verified-test\",\"statusCode\":1,\"amount\":1000,\"currency\":\"USD\"}";
    public HttpStatusCode PayPhoneHttpStatus { get; set; } = HttpStatusCode.OK;
    // Solo para verificar transiciones de negocio; InMemory no demuestra atomicidad SQL.
    public bool SimulatePaymentTransactions { get; set; }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Database:InitializeOnStartup"] = "false",
            ["ConnectionStrings:Redis"] = "",
            ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=unused",
            ["PayPhone:Token"] = "test-only-not-a-real-credential",
            ["DataProtection:KeysPath"] = Path.Combine(Path.GetTempPath(), "biogama-test-keys")
        }));
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<AppDbContext>>();
            services.AddDbContext<AppDbContext>(options =>
            {
                options.UseInMemoryDatabase(database);
                if (SimulatePaymentTransactions)
                    options.ConfigureWarnings(warnings => warnings.Ignore(
                        Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning));
            });
            services.AddTransient<IStartupFilter, TestTokenStartupFilter>();
            services.AddHttpClient<PayPhoneApiLinkService>().ConfigurePrimaryHttpMessageHandler(() =>
                new ProviderStub(() => new HttpResponseMessage(PayPhoneHttpStatus)
                {
                    Content = new StringContent(PayPhoneResponse, Encoding.UTF8, "application/json")
                }));
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = "Test";
                options.DefaultChallengeScheme = "Test";
                options.DefaultForbidScheme = "Test";
            }).AddScheme<AuthenticationSchemeOptions, TestAuthentication>("Test", _ => { });
        });
    }

    public HttpClient NewClient(string? role = "Administrador")
    {
        var client = CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, BaseAddress = new Uri("https://localhost") });
        if (role is not null) client.DefaultRequestHeaders.Add("X-Test-Role", role);
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Database.EnsureCreated();
        if (!db.Users.Any()) db.Users.Add(new IdentityUser { Id = "test-user", UserName = "tester@biogama.test" });
        if (!db.PhysicalProducts.Any()) db.PhysicalProducts.Add(new PhysicalProduct
        {
            Id = ProductId, Name = "Kit de campo", SKU = "KIT-TEST", Price = 10, Stock = 10
        });
        db.SaveChanges();
        return client;
    }
}

internal sealed class ProviderStub(Func<HttpResponseMessage> response) : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        => Task.FromResult(response());
}

// Solo existe en el host de pruebas: genera tokens reales sin renderizar el
// layout, que usa consultas SQL especificas de PostgreSQL para el carrito.
internal sealed class TestTokenStartupFilter : IStartupFilter
{
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
    {
        app.Use(async (context, nextMiddleware) =>
        {
            if (context.Request.Path == "/_test/antiforgery")
            {
                var authentication = await context.AuthenticateAsync("Test");
                if (authentication.Principal is not null) context.User = authentication.Principal;
                var tokens = context.RequestServices.GetRequiredService<IAntiforgery>().GetAndStoreTokens(context);
                await context.Response.WriteAsJsonAsync(new { token = tokens.RequestToken });
                return;
            }
            await nextMiddleware();
        });
        next(app);
    };
}

internal sealed class TestAuthentication(IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger, UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue("X-Test-Role", out var role))
            return Task.FromResult(AuthenticateResult.NoResult());
        var identity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "test-user"),
            new Claim(ClaimTypes.Name, "tester@biogama.test"),
            new Claim(ClaimTypes.Role, role.ToString())
        }, Scheme.Name);
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name)));
    }
}
