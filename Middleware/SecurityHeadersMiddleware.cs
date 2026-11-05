namespace BioGamaEcuador.Middleware;

public sealed class SecurityHeadersMiddleware(RequestDelegate next, IConfiguration configuration)
{
    public const string NonceKey = "BioGama.CspNonce";
    private readonly string imageOrigins = AllowedImageOrigins(configuration);
    private readonly string googleFormOrigin =
        string.IsNullOrWhiteSpace(configuration["Authentication:Google:ClientId"]) ||
        string.IsNullOrWhiteSpace(configuration["Authentication:Google:ClientSecret"])
            ? string.Empty : " https://accounts.google.com";

    private static string AllowedImageOrigins(IConfiguration configuration)
    {
        var sources = configuration.GetSection("Security:ImageOrigins").Get<string[]>() ?? ["https://biogama.ec"];
        var origins = new List<string>();
        foreach (var source in sources)
        {
            if (!Uri.TryCreate(source, UriKind.Absolute, out var uri) || uri.Scheme != "https" ||
                uri.UserInfo.Length != 0 || uri.AbsolutePath != "/" || uri.Query.Length != 0 ||
                uri.Fragment.Length != 0 || uri.Host.Contains('*'))
                throw new InvalidOperationException("Security:ImageOrigins debe contener solo origenes HTTPS concretos.");
            origins.Add(uri.GetLeftPart(UriPartial.Authority));
        }
        return string.Join(' ', origins.Distinct(StringComparer.OrdinalIgnoreCase));
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var nonce = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));
        context.Items[NonceKey] = nonce;
        context.Response.OnStarting(() =>
        {
            var chat = context.RequestServices.GetRequiredService<BioGamaEcuador.Services.ChatCredentials>().PublicUrl;
            var chatSource = chat is null ? "" : " " + chat + " " +
                (chat.StartsWith("https://", StringComparison.Ordinal) ? "wss://" + chat[8..] : "ws://" + chat[7..]);
            context.Response.Headers.XContentTypeOptions = "nosniff";
            context.Response.Headers.XFrameOptions = "SAMEORIGIN";
            context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
            if (context.Response.ContentType?.StartsWith("text/html", StringComparison.OrdinalIgnoreCase) == true)
                context.Response.Headers.CacheControl = "no-store";
            context.Response.Headers.ContentSecurityPolicy =
                "default-src 'self'; " +
                "base-uri 'self'; object-src 'none'; frame-ancestors 'self'; " +
                "script-src 'self' 'nonce-" + nonce + "' https://www.paypal.com https://*.paypal.com https://www.paypalobjects.com https://*.paypalobjects.com; script-src-attr 'none'; " +
                "style-src 'self' 'nonce-" + nonce + "'; style-src-attr 'none'; " +
                "font-src 'self'; img-src 'self' data: " + imageOrigins + "; " +
                "connect-src 'self' https://*.paypal.com https://*.paypalobjects.com " +
                (context.Request.IsHttps ? "wss://" : "ws://") + context.Request.Host + chatSource + "; " +
                "frame-src https://*.paypal.com https://*.paypalobjects.com; " +
                "form-action 'self' https://*.paypal.com" + googleFormOrigin + ";";
            return Task.CompletedTask;
        });
        await next(context);
    }
}
