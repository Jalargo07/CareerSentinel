using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using CareerSentinel.Configuration;
using CareerSentinel.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Retry;

namespace CareerSentinel.Services;

public class LocalLlmService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<LocalLlmService> _logger;
    private readonly OllamaSettings _ollamaSettings;
    private readonly ScoringSettings _scoringSettings;
    private readonly AsyncRetryPolicy<HttpResponseMessage> _retryPolicy;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public LocalLlmService(
        IHttpClientFactory httpClientFactory,
        IOptions<AppSettings> settings,
        ILogger<LocalLlmService> logger)
    {
        _httpClient = httpClientFactory.CreateClient("Ollama");
        _logger = logger;
        _ollamaSettings = settings.Value.Ollama;
        _scoringSettings = settings.Value.Scoring;

        _retryPolicy = Policy<HttpResponseMessage>
            .Handle<HttpRequestException>()
            .OrResult(r => !r.IsSuccessStatusCode)
            .WaitAndRetryAsync(
                _scoringSettings.MaxRetries,
                retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (outcome, delay, retryCount, _) =>
                {
                    _logger.LogWarning(
                        "Reintento LLM {RetryCount}/{MaxRetries} tras {Delay}s (StatusCode: {StatusCode})",
                        retryCount, _scoringSettings.MaxRetries, delay.TotalSeconds, outcome.Result?.StatusCode);
                });
    }

    public async Task<EvaluationResult?> EvaluateJobAsync(
        string jobDescription,
        string myCv,
        CancellationToken ct = default)
    {
        var prompt = BuildPrompt(jobDescription, myCv);

        var requestBody = new
        {
            model = _ollamaSettings.ModelName,
            prompt,
            stream = false,
            format = "json",
            options = new
            {
                temperature = 0.3,
                num_predict = 256,
            },
        };

        try
        {
            var response = await _retryPolicy.ExecuteAsync(() =>
                _httpClient.PostAsJsonAsync($"{_ollamaSettings.BaseUrl}/api/generate", requestBody, ct));

            response.EnsureSuccessStatusCode();

            var ollamaResponse = await response.Content.ReadFromJsonAsync<OllamaGenerateResponse>(JsonOptions, ct);

            if (ollamaResponse?.Response is null)
            {
                _logger.LogError("Respuesta de Ollama vacia");
                return null;
            }

            return ParseEvaluationResult(ollamaResponse.Response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al evaluar oferta con LLM local");
            return null;
        }
    }

    private static string BuildPrompt(string jobDescription, string myCv)
    {
        return "Eres un reclutador tecnico senior. Evalua coincidencia CV vs Oferta.\n"
            + "Responde UNICAMENTE en JSON: {\"score\": 0-100, \"summary\": \"1 frase\", \"matching_skills\": [\"skill1\"]}\n\n"
            + "CV: " + myCv + "\n"
            + "OFERTA: " + jobDescription;
    }

    private EvaluationResult? ParseEvaluationResult(string rawResponse)
    {
        try
        {
            var result = JsonSerializer.Deserialize<EvaluationResult>(rawResponse, JsonOptions);
            if (result is { Score: >= 0 and <= 100 }) return result;
        }
        catch (JsonException)
        {
        }

        try
        {
            var jsonMatch = System.Text.RegularExpressions.Regex.Match(
                rawResponse,
                @"\{[\s\S]*""score""[\s\S]*\}",
                System.Text.RegularExpressions.RegexOptions.Singleline);

            if (jsonMatch.Success)
            {
                var result = JsonSerializer.Deserialize<EvaluationResult>(jsonMatch.Value, JsonOptions);
                if (result is { Score: >= 0 and <= 100 }) return result;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Fallback regex tambien fallo");
        }

        _logger.LogError("No se pudo parsear la respuesta del LLM: {Response}", rawResponse[..Math.Min(200, rawResponse.Length)]);
        return null;
    }

    private sealed class OllamaGenerateResponse
    {
        public string? Response { get; set; }
        public bool Done { get; set; }
    }
}