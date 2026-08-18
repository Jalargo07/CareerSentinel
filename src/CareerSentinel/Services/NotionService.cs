using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using CareerSentinel.Configuration;
using CareerSentinel.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CareerSentinel.Services;

public class NotionService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<NotionService> _logger;
    private readonly NotionSettings _settings;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public NotionService(
        IHttpClientFactory httpClientFactory,
        IOptions<AppSettings> settings,
        ILogger<NotionService> logger)
    {
        _httpClient = httpClientFactory.CreateClient("Notion");
        _logger = logger;
        _settings = settings.Value.Notion;
    }

    public async Task<HashSet<string>> GetExistingJobIdsAsync(CancellationToken ct = default)
    {
        var ids = new HashSet<string>();

        try
        {
            var requestBody = new { page_size = 100 };
            var response = await _httpClient.PostAsJsonAsync(
                $"https://api.notion.com/v1/databases/{_settings.DatabaseId}/query",
                requestBody, ct);

            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(ct);
            var queryResult = JsonSerializer.Deserialize<NotionQueryResponse>(json, JsonOptions);

            if (queryResult?.Results is null) return ids;

            foreach (var page in queryResult.Results)
            {
                if (page.Properties?.TryGetValue("id_externo", out var idProp) == true
                    && idProp.RichText is { Count: > 0 } textArr)
                {
                    var id = textArr[0]?.PlainText;
                    if (!string.IsNullOrEmpty(id))
                        ids.Add(id);
                }
            }

            _logger.LogInformation("Encontradas {Count} ofertas existentes en Notion", ids.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener IDs existentes de Notion");
        }

        return ids;
    }

    public async Task SaveJobAsync(JobOffer job, EvaluationResult evaluation, CancellationToken ct = default)
    {
        try
        {
            var properties = new Dictionary<string, object>
            {
                ["Titulo"] = new { title = new[] { new { text = new { content = job.Title } } } },
                ["Empresa"] = new { rich_text = new[] { new { text = new { content = job.Company } } } },
                ["URL"] = new { url = job.Url },
                ["id_externo"] = new { rich_text = new[] { new { text = new { content = job.Id } } } },
                ["Score"] = new { number = evaluation.Score },
                ["Resumen"] = new { rich_text = new[] { new { text = new { content = Truncate(evaluation.Razon, 2000) } } } },
                ["Fecha deteccion"] = new { date = new { start = DateTime.UtcNow.ToString("yyyy-MM-dd") } },
                ["Cumple"] = new { rich_text = new[] { new { text = new { content = Truncate(string.Join(", ", evaluation.Cumple), 2000) } } } },
                ["No Cumple"] = new { rich_text = new[] { new { text = new { content = Truncate(string.Join(", ", evaluation.NoCumple), 2000) } } } },
            };

            var requestBody = new
            {
                parent = new { database_id = _settings.DatabaseId },
                properties,
            };

            var response = await _httpClient.PostAsJsonAsync(
                "https://api.notion.com/v1/pages", requestBody, ct);

            response.EnsureSuccessStatusCode();

            _logger.LogInformation("Oferta guardada en Notion: {Title} (Score: {Score})", job.Title, evaluation.Score);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al guardar oferta en Notion: {Title}", job.Title);
        }
    }

    private static string Truncate(string text, int maxLength) =>
        text.Length <= maxLength ? text : text[..maxLength] + "...";

    private sealed class NotionQueryResponse
    {
        [JsonPropertyName("results")]
        public List<NotionPage>? Results { get; set; }
    }

    private sealed class NotionPage
    {
        [JsonPropertyName("properties")]
        public Dictionary<string, NotionProperty>? Properties { get; set; }
    }

    private sealed class NotionProperty
    {
        [JsonPropertyName("rich_text")]
        public List<NotionRichText>? RichText { get; set; }
    }

    private sealed class NotionRichText
    {
        [JsonPropertyName("plain_text")]
        public string? PlainText { get; set; }
    }
}