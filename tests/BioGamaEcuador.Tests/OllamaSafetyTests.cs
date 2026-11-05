using System.Net;
using System.Text;
using System.Text.Json;
using BioGamaEcuador.Services;
using BioGamaEcuador.Services.Ollama;
using BioGamaEcuador.Settings;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;
using BioGamaEcuador.Controllers.Api;
using BioGamaEcuador.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace BioGamaEcuador.Tests;

public sealed class OllamaSafetyTests
{
    [Fact]
    public async Task Api_masks_provider_diagnostics_and_reports_temporary_unavailability()
    {
        var provider = new Mock<IOllamaService>();
        provider.Setup(value => value.GenerateResponseAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OllamaResponseDto { success = false, error = "private-host:11434 private credential" });
        await using var database = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var controller = new IaApiController(provider.Object, database)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
        var result = Assert.IsType<ObjectResult>(await controller.Generar(new IaQueryRequest { Prompt = "Consulta" }));
        Assert.Equal(StatusCodes.Status503ServiceUnavailable, result.StatusCode);
        Assert.Equal(AiAvailability.UnavailableMessage, Assert.IsType<IaQueryResponse>(result.Value).Error);
        Assert.Equal("30", controller.Response.Headers.RetryAfter.ToString());
    }

    [Theory]
    [InlineData("connection")]
    [InlineData("http-error")]
    [InlineData("invalid-json")]
    [InlineData("incomplete")]
    public async Task Provider_failures_never_expose_internal_details(string failure)
    {
        using var handler = new Stub(async (_, _) =>
        {
            await Task.CompletedTask;
            if (failure == "connection") throw new HttpRequestException("private-server:11434 secret=test-password");
            return JsonResponse(failure == "http-error" ? HttpStatusCode.BadGateway : HttpStatusCode.OK,
                failure == "invalid-json" ? "{" : failure == "incomplete" ? "{}" : "private-path=/root/secret");
        });
        using var client = new HttpClient(handler);
        var service = Service(client);
        var result = await service.GenerateResponseAsync("Consulta pública");
        Assert.False(result.success);
        Assert.Equal(AiAvailability.UnavailableMessage, result.error);
        Assert.Empty(result.response);
    }

    [Fact]
    public async Task Successful_requests_keep_model_output_and_send_bounded_generation_options()
    {
        using var handler = new Stub(async (request, cancellationToken) =>
        {
            Assert.Equal("/api/generate", request.RequestUri!.AbsolutePath);
            using var body = JsonDocument.Parse(await request.Content!.ReadAsStringAsync(cancellationToken));
            Assert.False(body.RootElement.GetProperty("stream").GetBoolean());
            Assert.Equal("gemma2:2b", body.RootElement.GetProperty("model").GetString());
            Assert.Equal(256, body.RootElement.GetProperty("options").GetProperty("num_predict").GetInt32());
            Assert.False(body.RootElement.TryGetProperty("num_predict", out _));
            return JsonResponse(HttpStatusCode.OK, "{\"model\":\"gemma2:2b\",\"response\":\"Protege el hábitat.\",\"done\":true}");
        });
        using var client = new HttpClient(handler);
        var result = await Service(client).GenerateResponseAsync("Consulta");
        Assert.True(result.success);
        Assert.Equal("Protege el hábitat.", result.response);
        Assert.Null(result.error);
    }

    [Fact]
    public async Task Client_cancellation_is_propagated_instead_of_becoming_provider_failure()
    {
        using var handler = new Stub((_, cancellationToken) => Task.FromCanceled<HttpResponseMessage>(cancellationToken));
        using var client = new HttpClient(handler);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Service(client)
            .GenerateResponseAsync("Consulta", cancellationToken: cancellation.Token));
    }

    private static OllamaService Service(HttpClient client) => new(client,
        Options.Create(new OllamaSettings()), NullLogger<OllamaService>.Instance);

    private static HttpResponseMessage JsonResponse(HttpStatusCode status, string content) => new(status)
    {
        Content = new StringContent(content, Encoding.UTF8, "application/json")
    };

    private sealed class Stub(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handle) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => handle(request, cancellationToken);
    }
}
