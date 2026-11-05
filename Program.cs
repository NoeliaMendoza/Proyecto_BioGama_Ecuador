using System.Globalization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using BioGamaEcuador.Data;
using BioGamaEcuador.Data.Seeders;
using BioGamaEcuador.Workers;
using BioGamaEcuador.Hubs;

AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

static string? ReadSecret(string name)
{
    var path = $"/run/secrets/{name}";
    return File.Exists(path) ? File.ReadAllText(path).Trim() : null;
}

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.ConfigureKestrel(options => options.AddServerHeader = false);

var dbPassword = ReadSecret("db_password");
var emailPass = ReadSecret("email_password");
if (emailPass != null)
{
    builder.Configuration["EmailSettings:Password"] = emailPass;
}

var isMailWorker = string.Equals(
    builder.Configuration["SERVICE_TYPE"], "mailworker",
    StringComparison.OrdinalIgnoreCase);

var connStr = builder.Configuration.GetConnectionString("DefaultConnection");
if (dbPassword != null && connStr != null)
{
    var csb = new Npgsql.NpgsqlConnectionStringBuilder(connStr) { Password = dbPassword };
    connStr = csb.ConnectionString;
}

builder.Services.AddControllersWithViews()
    .AddDataAnnotationsLocalization();
builder.Services.AddRazorPages();
// En produccion las cookies solo viajan por HTTPS. El desarrollo HTTP conserva
// compatibilidad; no relajar esta politica en el despliegue de Kubernetes.
var secureCookies = builder.Environment.IsDevelopment()
    ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
builder.Services.AddAntiforgery(options => options.Cookie.SecurePolicy = secureCookies);
builder.Services.ConfigureApplicationCookie(options => options.Cookie.SecurePolicy = secureCookies);
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<BioGamaEcuador.Services.ChatCredentials>();
builder.Services.AddScoped<BioGamaEcuador.Services.CartSummary>();
builder.Services.AddScoped<BioGamaEcuador.Services.StockNotifications>();
var signalR = builder.Services.AddSignalR();
var redisConnection = builder.Configuration.GetConnectionString("Redis");
if (!string.IsNullOrWhiteSpace(redisConnection))
{
    signalR.AddStackExchangeRedis(redisConnection, options =>
        options.Configuration.ChannelPrefix = StackExchange.Redis.RedisChannel.Literal("BioGamaEcuador"));
}

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connStr!));

builder.Services
    .AddDefaultIdentity<IdentityUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = true;
        options.Password.RequireDigit = true;
        options.Password.RequireUppercase = true;
        options.Password.RequiredLength = 6;
        options.Password.RequireNonAlphanumeric = false;
    })
    .AddRoles<IdentityRole>()
    .AddErrorDescriber<BioGamaEcuador.Services.SpanishIdentityErrorDescriber>()
    .AddEntityFrameworkStores<AppDbContext>();

builder.Services
    .AddAuthentication();
var googleClientId = builder.Configuration["Authentication:Google:ClientId"];
var googleClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
if (!string.IsNullOrEmpty(googleClientId) && !string.IsNullOrEmpty(googleClientSecret))
{
    builder.Services.AddAuthentication().AddGoogle(options =>
    {
        options.ClientId = googleClientId;
        options.ClientSecret = googleClientSecret;
    });
}

// Configuración y Servicios de Pasarelas de Pago
builder.Services.Configure<BioGamaEcuador.Settings.PayPhoneSettings>(
    builder.Configuration.GetSection("PayPhone"));
builder.Services.Configure<BioGamaEcuador.Settings.PayPalSettings>(
    builder.Configuration.GetSection("PayPal"));

builder.Services.AddHttpClient<BioGamaEcuador.Services.Payments.PayPhoneApiLinkService>(client =>
    client.Timeout = TimeSpan.FromSeconds(15));
builder.Services.AddHttpClient<BioGamaEcuador.Services.Payments.PayPalService>();
builder.Services.AddScoped<BioGamaEcuador.Services.IInventoryService, BioGamaEcuador.Services.InventoryMovementService>();
builder.Services.AddScoped<BioGamaEcuador.Services.IEmailService, BioGamaEcuador.Services.EmailService>();
builder.Services.AddScoped<BioGamaEcuador.Services.IAuditService, BioGamaEcuador.Services.AuditService>();
builder.Services.AddScoped<BioGamaEcuador.Services.IPaymentGateway, BioGamaEcuador.Services.Payments.PayPalPaymentGateway>();
builder.Services.AddScoped<BioGamaEcuador.Services.IPaymentGateway, BioGamaEcuador.Services.Payments.PayPhonePaymentGateway>();
builder.Services.AddScoped<BioGamaEcuador.Services.IPaymentService, BioGamaEcuador.Services.PaymentService>();
builder.Services.AddScoped<BioGamaEcuador.Services.IAccountService, BioGamaEcuador.Services.AccountService>();
builder.Services.AddScoped<BioGamaEcuador.Services.IAIService, BioGamaEcuador.Services.AIService>();

builder.Services.Configure<BioGamaEcuador.Settings.EmailSettings>(
    builder.Configuration.GetSection("EmailSettings"));

// Configuración y Servicio de IA Local Ollama
builder.Services.Configure<BioGamaEcuador.Settings.OllamaSettings>(
    builder.Configuration.GetSection("Ollama"));
builder.Services.AddHttpClient<BioGamaEcuador.Services.Ollama.IOllamaService, BioGamaEcuador.Services.Ollama.OllamaService>();

// ── Compartir cookies de autenticación entre réplicas ──────
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(
        builder.Configuration["DataProtection:KeysPath"] ?? Path.Combine(builder.Environment.ContentRootPath, ".local", "keys")))
    .SetApplicationName("BioGamaEcuador");

// ── Sesiones distribuidas (PostgreSQL) ─────────────────────
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SecurePolicy = secureCookies;
});

// ── Worker de correo (solo en modo mailworker) ─────────────
if (isMailWorker)
{
    builder.Services.AddHostedService<MailWorker>();
}

var app = builder.Build();
app.UseMiddleware<BioGamaEcuador.Middleware.SecurityHeadersMiddleware>();

var cultura = new CultureInfo("es-EC");
CultureInfo.DefaultThreadCurrentCulture = cultura;
CultureInfo.DefaultThreadCurrentUICulture = cultura;
app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture(cultura),
    SupportedCultures = new[] { cultura },
    SupportedUICultures = new[] { cultura },
    RequestCultureProviders = new List<IRequestCultureProvider> { new CustomRequestCultureProvider(_ => Task.FromResult<ProviderCultureResult?>(new ProviderCultureResult("es-EC"))) }
});

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

// Health check para Docker Swarm (sin DI para evitar errores de autenticación)
app.MapGet("/health", () =>
{
    return Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow });
});

app.MapGet("/api/info", (HttpContext context) =>
{
    context.Response.Headers.CacheControl = "no-store";
    return Results.Ok(new
    {
        application = "BioGamaEcuador",
        version = builder.Configuration["APP_VERSION"] ?? "local",
        pod = Environment.GetEnvironmentVariable("HOSTNAME") ?? Environment.MachineName,
        timestamp = DateTime.UtcNow
    });
});

app.MapGet("/health/ready", async (AppDbContext db) =>
{
    try
    {
        return await db.Database.CanConnectAsync()
            ? Results.Ok(new { status = "ready" })
            : Results.StatusCode(503);
    }
    catch
    {
        return Results.StatusCode(503);
    }
});

// En modo mailworker no se inicia el servidor web
if (!isMailWorker)
{
    app.MapHub<ActivityHub>("/hubs/activity");
    app.MapHub<StockHub>("/hubs/stock");
    app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}");
    app.MapRazorPages();

    if (builder.Configuration.GetValue("Database:InitializeOnStartup", true))
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
        await IdentitySeeder.SeedAsync(scope.ServiceProvider);
        await SalesModuleSeeder.SeedAsync(scope.ServiceProvider);
    }

    await app.RunAsync();
}
else
{
    // En modo mailworker solo corre el BackgroundService
    await app.RunAsync();
}
