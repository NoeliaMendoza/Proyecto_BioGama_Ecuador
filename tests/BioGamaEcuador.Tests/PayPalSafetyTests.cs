using System.Net;
using System.Text;
using BioGamaEcuador.Services.Payments;
using BioGamaEcuador.Settings;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace BioGamaEcuador.Tests;

public sealed class PayPalSafetyTests
{
    [Fact]
    public async Task Simulated_identifiers_are_rejected_without_contacting_provider()
    {
        using var client = new HttpClient(new Stub(_ => throw new InvalidOperationException("Unexpected request")));
        var result = await Service(client).CaptureOrderAsync("PAYPAL-MOCK-OLD");
        Assert.Equal("REJECTED", result.Status);
        Assert.Empty(result.CaptureId);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Provider_failures_never_create_fake_orders_or_confirm_payments(bool capture)
    {
        using var client = new HttpClient(new Stub(request => request.RequestUri!.AbsolutePath.EndsWith("/token", StringComparison.Ordinal)
            ? Json(HttpStatusCode.OK, "{\"access_token\":\"test-token\"}")
            : Json(HttpStatusCode.BadGateway, "private provider error")));
        if (capture)
        {
            var result = await Service(client).CaptureOrderAsync("REAL-ORDER-ID");
            Assert.Equal("UNAVAILABLE", result.Status);
            Assert.Empty(result.CaptureId);
            Assert.DoesNotContain("private", result.RawResponse);
        }
        else
        {
            var result = await Service(client).CreateOrderAsync(10m, "reference");
            Assert.Empty(result.OrderId);
            Assert.Empty(result.ApprovalUrl);
            Assert.DoesNotContain("private", result.RawResponse);
        }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Completed_capture_requires_an_actual_capture_identifier(bool includeCapture)
    {
        var capture = includeCapture
            ? "{\"status\":\"COMPLETED\",\"purchase_units\":[{\"payments\":{\"captures\":[{\"id\":\"PROVIDER-CAPTURE\"}]}}]}"
            : "{\"status\":\"COMPLETED\",\"purchase_units\":[]}";
        using var client = new HttpClient(new Stub(request => Json(HttpStatusCode.OK,
            request.RequestUri!.AbsolutePath.EndsWith("/token", StringComparison.Ordinal) ? "{\"access_token\":\"test-token\"}" : capture)));
        var result = await Service(client).CaptureOrderAsync("REAL-ORDER-ID");
        Assert.Equal(includeCapture ? "COMPLETED" : "UNAVAILABLE", result.Status);
        Assert.Equal(includeCapture ? "PROVIDER-CAPTURE" : "", result.CaptureId);
    }

    private static PayPalService Service(HttpClient client) => new(client,
        Options.Create(new PayPalSettings { ClientId = "unit-test-client", ClientSecret = "unit-test-only" }),
        NullLogger<PayPalService>.Instance);

    private static HttpResponseMessage Json(HttpStatusCode status, string body) => new(status)
    {
        Content = new StringContent(body, Encoding.UTF8, "application/json")
    };

    private sealed class Stub(Func<HttpRequestMessage, HttpResponseMessage> response) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(response(request));
    }
}
