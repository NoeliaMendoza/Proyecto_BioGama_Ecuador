using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;
using BioGamaEcuador.Settings;

namespace BioGamaEcuador.Services.Ollama
{
    public class OllamaService : IOllamaService
    {
        private static readonly JsonSerializerOptions ResponseJsonOptions = new() { PropertyNameCaseInsensitive = true };
        private readonly HttpClient _httpClient;
        private readonly OllamaSettings _settings;
        private readonly ILogger<OllamaService> _logger;

        public OllamaService(HttpClient httpClient, IOptions<OllamaSettings> options, ILogger<OllamaService> logger)
        {
            _httpClient = httpClient;
            _settings = options.Value;
            _logger = logger;
            
            if (_httpClient.BaseAddress == null)
            {
                _httpClient.BaseAddress = new Uri(_settings.BaseUrl.TrimEnd('/') + "/");
            }
            _httpClient.Timeout = TimeSpan.FromSeconds(_settings.TimeoutSeconds);
        }

        public async Task<OllamaResponseDto> GenerateResponseAsync(string prompt, string? systemPrompt = null, CancellationToken cancellationToken = default)
        {
            var requestBody = new OllamaRequestDto
            {
                model = _settings.Model,
                prompt = prompt,
                system = systemPrompt ?? "Eres un biólogo experto en biodiversidad ecuatoriana. Responde en español, conciso.",
                stream = false
            };

            try
            {
                using var response = await _httpClient.PostAsJsonAsync("api/generate", requestBody, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    BioGamaEcuador.Services.LogEvents.OllamaRejected(_logger, (int)response.StatusCode);
                    return Unavailable();
                }

                var result = await response.Content.ReadFromJsonAsync<OllamaResponseDto>(ResponseJsonOptions, cancellationToken);

                if (result == null || !result.done || string.IsNullOrWhiteSpace(result.response))
                {
                    BioGamaEcuador.Services.LogEvents.OllamaInvalidResponse(_logger);
                    return Unavailable();
                }

                result.success = true;
                result.error = null;
                return result;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                BioGamaEcuador.Services.LogEvents.OllamaUnavailable(_logger, ex);
                return Unavailable();
            }
        }

        private static OllamaResponseDto Unavailable() => new()
        {
            success = false,
            error = BioGamaEcuador.Services.AiAvailability.UnavailableMessage
        };
    }
}
