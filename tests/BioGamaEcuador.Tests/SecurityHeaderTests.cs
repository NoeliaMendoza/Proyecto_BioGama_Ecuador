using System.Net;
using Xunit;

namespace BioGamaEcuador.Tests;

public sealed class SecurityHeaderTests
{
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
            previous = nonce;
        }
    }
}
