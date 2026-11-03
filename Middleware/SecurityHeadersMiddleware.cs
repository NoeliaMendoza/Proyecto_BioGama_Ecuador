namespace BioGamaEcuador.Middleware;

public sealed class SecurityHeadersMiddleware(RequestDelegate next)
{
    public const string NonceKey = "BioGama.CspNonce";

    public async Task InvokeAsync(HttpContext context)
    {
        var nonce = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));
        context.Items[NonceKey] = nonce;
        context.Response.OnStarting(() =>
        {
            context.Response.Headers.XContentTypeOptions = "nosniff";
            context.Response.Headers.XFrameOptions = "SAMEORIGIN";
            context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
            if (context.Response.ContentType?.StartsWith("text/html", StringComparison.OrdinalIgnoreCase) == true)
                context.Response.Headers.CacheControl = "no-store";
            // Los estilos inline se mantienen hasta migrarlos a clases CSS.
            context.Response.Headers.ContentSecurityPolicy =
                "default-src 'self'; " +
                "base-uri 'self'; object-src 'none'; frame-ancestors 'self'; " +
                "script-src 'self' 'nonce-" + nonce + "' https://www.paypal.com https://*.paypal.com https://www.paypalobjects.com https://*.paypalobjects.com; script-src-attr 'none'; " +
                "style-src 'self' 'unsafe-inline'; " +
                "font-src 'self'; img-src 'self' data: https:; " +
                "connect-src 'self' https://*.paypal.com https://*.paypalobjects.com " +
                (context.Request.IsHttps ? "wss://" : "ws://") + context.Request.Host + "; " +
                "frame-src https://*.paypal.com https://*.paypalobjects.com; " +
                "form-action 'self' https://*.paypal.com;";
            return Task.CompletedTask;
        });
        await next(context);
    }
}
