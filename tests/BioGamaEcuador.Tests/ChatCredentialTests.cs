using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BioGamaEcuador.Services;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace BioGamaEcuador.Tests;

public sealed class ChatCredentialTests
{
    private static ChatCredentials Credentials(Dictionary<string, string?> values) => new(
        new ConfigurationBuilder().AddInMemoryCollection(values).Build(), TimeProvider.System);

    [Theory]
    [InlineData("UsuarioPublico", "visitor")]
    [InlineData("Investigador", "visitor")]
    [InlineData("Administrador", "admin")]
    [InlineData("Admin", "admin")]
    public void Credentials_are_signed_short_lived_and_use_the_authenticated_role(string role, string expectedRole)
    {
        var key = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        var service = Credentials(new() { ["Chat:PublicUrl"] = "http://localhost:30081", ["Chat:SigningKey"] = key });
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[] {
            new Claim(ClaimTypes.NameIdentifier, "visitor-123"), new Claim(ClaimTypes.Name, "Test Visitor"),
            new Claim(ClaimTypes.Role, role)
        }, "test"));
        using var result = JsonDocument.Parse(JsonSerializer.Serialize(service.Issue(user)));
        var parts = result.RootElement.GetProperty("token").GetString()!.Split('.');
        var signature = HMACSHA256.HashData(Encoding.UTF8.GetBytes(key), Encoding.ASCII.GetBytes(parts[0]));
        Assert.Equal(Convert.ToBase64String(signature).TrimEnd('=').Replace('+', '-').Replace('/', '_'), parts[1]);
        var base64 = parts[0].Replace('-', '+').Replace('_', '/');
        using var payload = JsonDocument.Parse(Convert.FromBase64String(base64.PadRight((base64.Length + 3) / 4 * 4, '=')));
        var root = payload.RootElement;
        Assert.Equal("visitor-123", root.GetProperty("sub").GetString());
        Assert.Equal(expectedRole, root.GetProperty("role").GetString());
        Assert.Equal("biogama-chat", root.GetProperty("aud").GetString());
        Assert.Equal(300, root.GetProperty("exp").GetInt64() - root.GetProperty("iat").GetInt64());
        Assert.DoesNotContain(key, result.RootElement.GetProperty("token").GetString()!);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("javascript:alert(1)")]
    [InlineData("http://user:password@localhost:30081")]
    [InlineData("http://localhost:30081/path")]
    [InlineData("http://localhost:30081/?token=test")]
    public void Invalid_service_origins_are_not_enabled(string? url)
    {
        Assert.False(Credentials(new() { ["Chat:PublicUrl"] = url, ["Chat:SigningKey"] = new string('x', 32) }).Enabled);
    }

    [Fact]
    public async Task Token_endpoint_requires_authentication_and_csrf()
    {
        using var app = new TestApplication();
        using var anonymous = app.NewClient(null);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/Chat")).StatusCode);
        using var visitor = app.NewClient("UsuarioPublico");
        Assert.Equal(HttpStatusCode.BadRequest, (await visitor.PostAsync("/Chat/Token", null)).StatusCode);
        var tokens = await visitor.GetFromJsonAsync<Dictionary<string, string>>("/_test/antiforgery");
        visitor.DefaultRequestHeaders.Add("RequestVerificationToken", tokens!["token"]);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, (await visitor.PostAsync("/Chat/Token", null)).StatusCode);
    }
}
