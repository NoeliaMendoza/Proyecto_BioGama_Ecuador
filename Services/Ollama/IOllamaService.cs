namespace BioGamaEcuador.Services.Ollama
{
    public interface IOllamaService
    {
        Task<OllamaResponseDto> GenerateResponseAsync(string prompt, string? systemPrompt = null, CancellationToken cancellationToken = default);
    }

    public class OllamaRequestDto
    {
        public string model { get; set; } = "gemma2:2b";
        public string prompt { get; set; } = string.Empty;
        public string? system { get; set; }
        public bool stream { get; set; } = false;
        public string keep_alive { get; set; } = "2m";
        public OllamaGenerationOptions options { get; set; } = new();
    }

    public class OllamaGenerationOptions
    {
        public int num_ctx { get; set; } = 1024;
        public int num_predict { get; set; } = 256;
        public int num_thread { get; set; } = 4;
    }

    public class OllamaResponseDto
    {
        public string model { get; set; } = string.Empty;
        public string response { get; set; } = string.Empty;
        public bool done { get; set; }
        public bool success { get; set; } = true;
        public string? error { get; set; }
    }
}
