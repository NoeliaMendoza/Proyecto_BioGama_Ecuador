using System.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Hosting;
using Xunit;

namespace BioGamaEcuador.Tests;

public sealed class SecurityHeaderTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Google_form_redirect_is_allowed_only_when_provider_is_configured(bool enabled)
    {
        await using var application = new TestApplication();
        using var configured = application.WithWebHostBuilder(builder => builder.ConfigureAppConfiguration((_, config) =>
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Authentication:Google:ClientId"] = enabled ? "unit-test-client" : "",
                ["Authentication:Google:ClientSecret"] = enabled ? "unit-test-only" : ""
            })));
        using var client = configured.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });
        var response = await client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var policy = Assert.Single(response.Headers.GetValues("Content-Security-Policy"));
        Assert.Equal(enabled, policy.Contains("https://accounts.google.com", StringComparison.Ordinal));
        Assert.DoesNotContain("'unsafe-inline'", policy);
    }

    [Fact]
    public async Task Https_antiforgery_cookie_is_secure_and_production_cookie_policies_are_strict()
    {
        await using var application = new TestApplication();
        using var client = application.NewClient(null);
        var response = await client.GetAsync("/_test/antiforgery");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var cookies = Microsoft.Net.Http.Headers.SetCookieHeaderValue.ParseList(response.Headers.GetValues("Set-Cookie").ToList());
        var cookie = Assert.Single(cookies, value => value.Name.ToString().StartsWith(".AspNetCore.Antiforgery", StringComparison.Ordinal));
        Assert.True(cookie.Secure);
        Assert.True(cookie.HttpOnly);
        using var scope = application.Services.CreateScope();
        var sessions = scope.ServiceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.Builder.SessionOptions>>();
        Assert.Equal(Microsoft.AspNetCore.Http.CookieSecurePolicy.Always, sessions.Value.Cookie.SecurePolicy);
        var authentication = scope.ServiceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptionsMonitor<Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationOptions>>();
        Assert.Equal(Microsoft.AspNetCore.Http.CookieSecurePolicy.Always,
            authentication.Get(Microsoft.AspNetCore.Identity.IdentityConstants.ApplicationScheme).Cookie.SecurePolicy);
    }

    [Theory]
    [InlineData("localhost", false)]
    [InlineData("biogama.example.test", true)]
    public async Task Hsts_is_emitted_for_production_hosts_and_keeps_loopback_excluded(string host, bool expected)
    {
        await using var application = new TestApplication();
        using var client = application.NewClient(null);
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://" + host + "/health");
        var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(expected, response.Headers.Contains("Strict-Transport-Security"));
    }

    [Theory]
    [InlineData("https://*.example.com")]
    [InlineData("http://example.com")]
    [InlineData("https://example.com/image.jpg")]
    [InlineData("https://user@example.com")]
    public void Image_allowlist_rejects_wildcards_insecure_origins_and_paths(string source)
    {
        var config = new Microsoft.Extensions.Configuration.ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Security:ImageOrigins:0"] = source }).Build();
        Assert.Throws<InvalidOperationException>(() => new BioGamaEcuador.Middleware.SecurityHeadersMiddleware(
            _ => Task.CompletedTask, config));
    }

    [Theory]
    [InlineData("/Products")]
    [InlineData("/health")]
    [InlineData("/lib/marked/marked.umd.js")]
    [InlineData("/lib/dompurify/purify.min.js")]
    [InlineData("/lib/chartjs/chart.umd.js")]
    [InlineData("/lib/bootstrap-icons/font/bootstrap-icons.min.css")]
    [InlineData("/lib/plus-jakarta-sans/font.css")]
    public async Task Public_pages_and_local_dependencies_have_security_headers(string path)
    {
        await using var application = new TestApplication();
        using var client = application.NewClient(null);
        var response = await client.GetAsync(path);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("nosniff", Assert.Single(response.Headers.GetValues("X-Content-Type-Options")));
        Assert.Equal("SAMEORIGIN", Assert.Single(response.Headers.GetValues("X-Frame-Options")));
        var policy = Assert.Single(response.Headers.GetValues("Content-Security-Policy"));
        Assert.Contains("frame-ancestors 'self'", policy);
        Assert.Contains("object-src 'none'", policy);
        Assert.DoesNotContain("cdn.jsdelivr.net", policy);
        Assert.DoesNotContain("'unsafe-inline'", policy.Split("script-src ")[1].Split(';')[0]);
        Assert.Contains("script-src-attr 'none'", policy);
        Assert.Contains("style-src-attr 'none'", policy);
        Assert.DoesNotContain("'unsafe-inline'", policy);
        var imageSources = policy.Split("img-src ")[1].Split(';')[0].Split(' ', StringSplitOptions.RemoveEmptyEntries);
        Assert.Contains("'self'", imageSources);
        Assert.DoesNotContain("https:", imageSources);
        Assert.DoesNotContain("*", imageSources);
    }

    [Fact]
    public async Task Inline_scripts_have_a_nonce_that_matches_the_response_policy_and_changes_each_time()
    {
        await using var application = new TestApplication();
        using var client = application.NewClient(null);
        string? previous = null;
        for (var iteration = 0; iteration < 2; iteration++)
        {
            var response = await client.GetAsync("/Ia");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var policy = Assert.Single(response.Headers.GetValues("Content-Security-Policy"));
            var nonce = policy.Split("'nonce-")[1].Split('\'')[0];
            Assert.NotEqual(previous, nonce);
            Assert.Equal(32, Convert.FromBase64String(nonce).Length);
            Assert.Contains("no-store", response.Headers.CacheControl!.ToString());
            var html = await response.Content.ReadAsStringAsync();
            var scripts = System.Text.RegularExpressions.Regex.Matches(html, "<script\\b[^>]*>",
                System.Text.RegularExpressions.RegexOptions.None, TimeSpan.FromSeconds(1));
            Assert.NotEmpty(scripts);
            foreach (System.Text.RegularExpressions.Match script in scripts)
                Assert.Contains($"nonce=\"{nonce}\"", WebUtility.HtmlDecode(script.Value));
            Assert.DoesNotContain("onclick=", html);
            Assert.DoesNotContain(" style=", html);
            previous = nonce;
        }
    }
}
