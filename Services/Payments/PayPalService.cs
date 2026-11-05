using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using BioGamaEcuador.Settings;

namespace BioGamaEcuador.Services.Payments
{
    public class PayPalOrderResult
    {
        public string OrderId { get; set; } = string.Empty;
        public string ApprovalUrl { get; set; } = string.Empty;
        public string RawResponse { get; set; } = string.Empty;
    }

    public class PayPalCaptureResult
    {
        public string Status { get; set; } = string.Empty;
        public string CaptureId { get; set; } = string.Empty;
        public string RawResponse { get; set; } = string.Empty;
    }

    public class PayPalService
    {
        private readonly HttpClient _httpClient;
        private readonly PayPalSettings _settings;
        private readonly ILogger<PayPalService> _logger;

        public PayPalService(HttpClient httpClient, IOptions<PayPalSettings> options, ILogger<PayPalService> logger)
        {
            _httpClient = httpClient;
            _settings = options.Value;
            _logger = logger;
        }

        public async Task<PayPalOrderResult> CreateOrderAsync(decimal total, string reference, string? returnUrl = null, string? cancelUrl = null)
        {
            try
            {
                var accessToken = await GetAccessTokenAsync();

                var payload = new
                {
                    intent = "CAPTURE",
                    purchase_units = new[]
                    {
                        new
                        {
                            reference_id = reference,
                            description = reference,
                            amount = new
                            {
                                currency_code = "USD",
                                value = total.ToString("0.00", CultureInfo.InvariantCulture)
                            }
                        }
                    },
                    application_context = new
                    {
                        brand_name = "BioGama Ecuador Conservation",
                        landing_page = "LOGIN",
                        user_action = "PAY_NOW",
                        return_url = returnUrl ?? _settings.ReturnUrl,
                        cancel_url = cancelUrl ?? _settings.CancelUrl
                    }
                };

                using var request = new HttpRequestMessage(HttpMethod.Post, $"{_settings.BaseUrl}/v2/checkout/orders");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                request.Content = JsonContent.Create(payload);

                using var response = await _httpClient.SendAsync(request);
                var content = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    throw new InvalidOperationException("PayPal no pudo crear la orden.");
                }

                using var document = JsonDocument.Parse(content);
                var root = document.RootElement;
                var orderId = root.GetProperty("id").GetString() ?? string.Empty;

                string approvalUrl = string.Empty;
                if (root.TryGetProperty("links", out var linksElement))
                {
                    foreach (var link in linksElement.EnumerateArray())
                    {
                        if (link.TryGetProperty("rel", out var rel) && rel.GetString() == "approve")
                        {
                            approvalUrl = link.GetProperty("href").GetString() ?? string.Empty;
                            break;
                        }
                    }
                }

                return new PayPalOrderResult
                {
                    OrderId = orderId,
                    ApprovalUrl = approvalUrl,
                    RawResponse = content
                };
            }
            catch (Exception ex)
            {
                BioGamaEcuador.Services.LogEvents.PayPalRequestFailed(_logger, ex);
                return new PayPalOrderResult
                {
                    RawResponse = "{\"status\":\"UNAVAILABLE\"}"
                };
            }
        }

        public async Task<PayPalCaptureResult> CaptureOrderAsync(string orderId)
        {
            if (string.IsNullOrWhiteSpace(orderId) || orderId.StartsWith("PAYPAL-MOCK-", StringComparison.Ordinal))
            {
                return new PayPalCaptureResult
                {
                    Status = "REJECTED",
                    RawResponse = "{\"status\":\"REJECTED\"}"
                };
            }

            try
            {
                var accessToken = await GetAccessTokenAsync();

                using var request = new HttpRequestMessage(HttpMethod.Post, $"{_settings.BaseUrl}/v2/checkout/orders/{Uri.EscapeDataString(orderId)}/capture");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                request.Content = new StringContent("{}", Encoding.UTF8, "application/json");

                using var response = await _httpClient.SendAsync(request);
                var content = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    throw new InvalidOperationException("PayPal no pudo confirmar la captura.");
                }

                using var document = JsonDocument.Parse(content);
                var root = document.RootElement;

                string status = root.GetProperty("status").GetString() ?? string.Empty;
                string captureId = string.Empty;

                if (root.TryGetProperty("purchase_units", out var units))
                {
                    var firstUnit = units.EnumerateArray().FirstOrDefault();
                    if (firstUnit.ValueKind != JsonValueKind.Undefined &&
                        firstUnit.TryGetProperty("payments", out var payments) &&
                        payments.TryGetProperty("captures", out var captures))
                    {
                        var firstCapture = captures.EnumerateArray().FirstOrDefault();
                        if (firstCapture.ValueKind != JsonValueKind.Undefined &&
                            firstCapture.TryGetProperty("id", out var idElement))
                        {
                            captureId = idElement.GetString() ?? string.Empty;
                        }
                    }
                }

                if (status == "COMPLETED" && string.IsNullOrWhiteSpace(captureId))
                    throw new InvalidOperationException("PayPal no devolvio identificador de captura.");

                return new PayPalCaptureResult
                {
                    Status = status,
                    CaptureId = captureId,
                    RawResponse = content
                };
            }
            catch (Exception ex)
            {
                BioGamaEcuador.Services.LogEvents.PayPalRequestFailed(_logger, ex);
                return new PayPalCaptureResult
                {
                    Status = "UNAVAILABLE",
                    RawResponse = "{\"status\":\"UNAVAILABLE\"}"
                };
            }
        }

        private async Task<string> GetAccessTokenAsync()
        {
            if (string.IsNullOrWhiteSpace(_settings.ClientId) || string.IsNullOrWhiteSpace(_settings.ClientSecret))
                throw new InvalidOperationException("PayPal no esta configurado.");
            var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_settings.ClientId}:{_settings.ClientSecret}"));

            using var request = new HttpRequestMessage(HttpMethod.Post, $"{_settings.BaseUrl}/v1/oauth2/token");
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                { "grant_type", "client_credentials" }
            });

            using var response = await _httpClient.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException("No se pudo autenticar la solicitud a PayPal.");
            }

            using var document = JsonDocument.Parse(content);
            return document.RootElement.GetProperty("access_token").GetString()
                ?? throw new InvalidOperationException("PayPal no devolvió access_token.");
        }
    }
}
