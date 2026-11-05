using BioGamaEcuador.Services.Ollama;

namespace BioGamaEcuador.Services;

public class AIService : IAIService
{
    private readonly IOllamaService _ollama;
    private readonly ILogger<AIService> _logger;

    public AIService(IOllamaService ollama, ILogger<AIService> logger)
    {
        _ollama = ollama;
        _logger = logger;
    }

    public async Task<AIResult> GenerateAsync(string instruction, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _ollama.GenerateResponseAsync(instruction, "Eres un biologo experto en biodiversidad ecuatoriana. Responde en espanol, conciso.", cancellationToken);
            return new AIResult
            {
                Success = result.success,
                Response = result.response,
                ErrorMessage = result.success ? null : AiAvailability.UnavailableMessage
            };
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            LogEvents.OllamaUnavailable(_logger, ex);
            return new AIResult
            {
                Success = false,
                ErrorMessage = AiAvailability.UnavailableMessage
            };
        }
    }
}
