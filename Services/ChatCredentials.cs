using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace BioGamaEcuador.Services;

// Contrato compartido con el middleware de autenticación del servicio Node.js.
public sealed class ChatCredentials(IConfiguration configuration, TimeProvider clock)
{
    public static bool IsAdministrator(ClaimsPrincipal user) =>
        user.IsInRole("Administrador") || user.IsInRole("Admin");

    public string? PublicUrl
    {
        get
        {
            var value = configuration["Chat:PublicUrl"];
            return Uri.TryCreate(value, UriKind.Absolute, out var uri) &&
                (uri.Scheme == "http" || uri.Scheme == "https") &&
                string.IsNullOrEmpty(uri.UserInfo) && uri.AbsolutePath == "/" &&
                string.IsNullOrEmpty(uri.Query) && string.IsNullOrEmpty(uri.Fragment)
                ? uri.GetLeftPart(UriPartial.Authority) : null;
        }
    }

    public bool Enabled => PublicUrl is not null &&
        Encoding.UTF8.GetByteCount(configuration["Chat:SigningKey"] ?? "") >= 32;

    public object Issue(ClaimsPrincipal user)
    {
        if (!Enabled) throw new InvalidOperationException("El chat no está configurado.");
        var id = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(id)) throw new InvalidOperationException("Se requiere una cuenta válida.");
        var now = clock.GetUtcNow().ToUnixTimeSeconds();
        var administrator = IsAdministrator(user);
        var payload = JsonSerializer.SerializeToUtf8Bytes(new
        {
            iss = "biogama-web", aud = "biogama-chat", sub = id,
            name = administrator ? "Equipo BioGama" : user.Identity?.Name ?? "Visitante",
            role = administrator ? "admin" : "visitor", iat = now, exp = now + 300,
            jti = Guid.NewGuid().ToString("N")
        });
        var encoded = Base64Url(payload);
        var signature = HMACSHA256.HashData(
            Encoding.UTF8.GetBytes(configuration["Chat:SigningKey"]!), Encoding.ASCII.GetBytes(encoded));
        return new { token = encoded + "." + Base64Url(signature), expiresAt = now + 300 };
    }

    private static string Base64Url(byte[] bytes) => Convert.ToBase64String(bytes)
        .TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
