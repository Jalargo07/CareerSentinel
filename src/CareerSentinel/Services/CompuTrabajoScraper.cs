using System.Text.RegularExpressions;
using AngleSharp.Html.Parser;
using CareerSentinel.Configuration;
using CareerSentinel.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CareerSentinel.Services;

public partial class CompuTrabajoScraper : IJobScraper
{
    public string PortalName => "CompuTrabajo";

    private readonly HttpClient _httpClient;
    private readonly ILogger<CompuTrabajoScraper> _logger;
    private readonly RateLimitSettings _rateLimit;
    private readonly string _defaultLocation;

    private const string BaseSearchUrl = "https://www.computrabajo.com.co";

    private static readonly string[] UserAgents =
    [
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36",
        "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36",
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:133.0) Gecko/20100101 Firefox/133.0",
        "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36",
    ];

    private static readonly Random Rng = new();

    public CompuTrabajoScraper(
        IHttpClientFactory httpClientFactory,
        IOptions<AppSettings> settings,
        ILogger<CompuTrabajoScraper> logger)
    {
        _httpClient = httpClientFactory.CreateClient("CompuTrabajo");
        _logger = logger;
        _rateLimit = settings.Value.RateLimiting;
        _defaultLocation = SanitizeLocation(settings.Value.JobSources.TryGetValue("CompuTrabajo", out var source) ? source.Location : string.Empty);
    }

    public async Task<List<JobOffer>> SearchAsync(string keyword, string location, CancellationToken ct = default)
    {
        var sanitizedKeyword = SanitizeKeyword(keyword);
        var sanitizedLocation = SanitizeLocation(location);
        var locationSegment = !string.IsNullOrEmpty(sanitizedLocation) ? sanitizedLocation : _defaultLocation;
        var url = !string.IsNullOrEmpty(locationSegment)
            ? $"{BaseSearchUrl}/trabajo-de-{sanitizedKeyword}-en-{locationSegment}"
            : $"{BaseSearchUrl}/trabajo-de-{sanitizedKeyword}";

        _logger.LogInformation("Buscando CompuTrabajo: keyword={Keyword}, url={Url} (URL completa: {FullUrl})", keyword, url, url);

        SetRandomUserAgent();

        try
        {
            var html = await _httpClient.GetStringAsync(url, ct);
            return ParseJobListings(html, keyword, location);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Busqueda en CompuTrabajo cancelada para keyword={Keyword}", keyword);
            return [];
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Error HTTP al buscar ofertas en CompuTrabajo para keyword={Keyword}", keyword);
            return [];
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado al buscar ofertas en CompuTrabajo para keyword={Keyword}", keyword);
            return [];
        }
    }

    public async Task<string> GetDescriptionAsync(string jobUrl, CancellationToken ct = default)
    {
        _logger.LogInformation("Obteniendo detalle de CompuTrabajo: {Url}", jobUrl);

        SetRandomUserAgent();

        try
        {
            // Usar HttpRequestMessage para incluir header Referer
            using var request = new HttpRequestMessage(HttpMethod.Get, jobUrl);
            request.Headers.TryAddWithoutValidation("Referer", "https://www.computrabajo.com.co/");

            var response = await _httpClient.SendAsync(request, ct);
            response.EnsureSuccessStatusCode();
            var html = await response.Content.ReadAsStringAsync(ct);

            // Estrategia 1: Buscar por el patrón real de CompuTrabajo
            // Buscar "Descripción de la oferta" y extraer el contenido siguiente
            var descriptionMatch = DescriptionRegex().Match(html);
            if (descriptionMatch.Success)
            {
                var rawText = descriptionMatch.Groups[1].Value;
                // Limpiar HTML tags y normalizar espacios
                var cleanText = HtmlTagRegex().Replace(rawText, " ");
                cleanText = MultipleSpaceRegex().Replace(cleanText, " ").Trim();
                if (cleanText.Length > 50)
                {
                    return cleanText;
                }
            }

            // Fallback: intentar con AngleSharp - selector p.mbB después de h3 con "Descripción"
            var parser = new HtmlParser();
            var document = await parser.ParseDocumentAsync(html, ct);

            // Buscar el h3 que contiene "Descripción"
            var h3Elements = document.QuerySelectorAll("h3");
            foreach (var h3 in h3Elements)
            {
                if (h3.TextContent.Contains("Descripci", StringComparison.OrdinalIgnoreCase))
                {
                    // El contenido está en el siguiente hermano o padre
                    var parent = h3.ParentElement;
                    if (parent is not null)
                    {
                        var paragraphs = parent.QuerySelectorAll("p.mbB");
                        if (paragraphs.Length > 0)
                        {
                            return string.Join("\n\n", paragraphs.Select(p => p.TextContent.Trim()));
                        }
                    }
                }
            }

            // Fallback 2: buscar div.mbB
            var mbBElements = document.QuerySelectorAll("div.mbB");
            if (mbBElements.Length > 0)
            {
                return string.Join("\n\n", mbBElements.Select(e => e.TextContent.Trim()));
            }

            return string.Empty;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Obtencion de descripcion cancelada para {Url}", jobUrl);
            return string.Empty;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Error HTTP al obtener descripcion de {Url}", jobUrl);
            return string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado al obtener descripcion de {Url}", jobUrl);
            return string.Empty;
        }
    }

    private List<JobOffer> ParseJobListings(string html, string keyword, string location)
    {
        var jobs = new List<JobOffer>();
        var parser = new HtmlParser();
        var document = parser.ParseDocument(html);

        var cards = document.QuerySelectorAll(".box_offer");

        _logger.LogInformation("CompuTrabajo: HTML parseado, {Count} articles encontrados", cards.Length);

        foreach (var card in cards)
        {
            // Selector basado en HTML real verificado con curl
            var linkEl = card.QuerySelector("a.js-o-link");
            var companyEl = card.QuerySelector("a[offer-grid-article-company-url]");
            var locationEl = card.QuerySelector("p.fs16.fc_base span.mr10");
            var dateEl = card.QuerySelector("p.fs13.fc_aux");

            if (linkEl is null) continue;

            var title = linkEl.TextContent.Trim();
            if (string.IsNullOrEmpty(title)) continue;

            // Usar data-id del article (más confiable que extraer del href)
            var id = card.GetAttribute("data-id");
            if (string.IsNullOrEmpty(id)) continue;

            var href = linkEl.GetAttribute("href") ?? string.Empty;
            if (!href.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                href = $"{BaseSearchUrl}{href}";
            }

            // Limpiar ubicación: "Funza, Cundinamarca" → "Funza, Cundinamarca"
            var rawLocation = locationEl?.TextContent.Trim() ?? string.Empty;
            var cleanLocation = string.IsNullOrEmpty(rawLocation) ? location : rawLocation;

            jobs.Add(new JobOffer
            {
                Id = id,
                Title = title,
                Company = companyEl?.TextContent.Trim() ?? "Desconocida",
                Url = href,
                Location = cleanLocation,
                PostedDate = dateEl?.TextContent.Trim() ?? string.Empty,
                SourceKeyword = keyword,
            });
        }

        _logger.LogInformation("CompuTrabajo: {Valid}/{Total} ofertas válidas para keyword={Keyword}", jobs.Count, cards.Length, keyword);
        _logger.LogInformation("Encontradas {Count} ofertas en CompuTrabajo para keyword={Keyword}", jobs.Count, keyword);
        return jobs;
    }

    private void SetRandomUserAgent()
    {
        _httpClient.DefaultRequestHeaders.Remove("User-Agent");
        _httpClient.DefaultRequestHeaders.Add("User-Agent", UserAgents[Rng.Next(UserAgents.Length)]);
    }

    private static string SanitizeKeyword(string keyword)
    {
        var sanitized = keyword
            .ToLowerInvariant()
            .Trim();

        sanitized = KeywordRegex().Replace(sanitized, "-");
        sanitized = MultipleDashRegex().Replace(sanitized, "-");
        sanitized = sanitized.Trim('-');

        return sanitized;
    }

    private static string SanitizeLocation(string location)
    {
        if (string.IsNullOrWhiteSpace(location))
            return string.Empty;

        var sanitized = location
            .ToLowerInvariant()
            .Trim()
            .Normalize(System.Text.NormalizationForm.FormD);

        // Remove accents (e.g. Medellín → medellin)
        sanitized = new string(sanitized.Where(c => System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c) != System.Globalization.UnicodeCategory.NonSpacingMark).ToArray());

        // Replace non-alphanumeric characters (except spaces and dashes) with spaces
        sanitized = Regex.Replace(sanitized, @"[^a-z0-9\s-]", " ");

        // Replace spaces with hyphens
        sanitized = sanitized.Replace(" ", "-");

        // Collapse multiple hyphens
        sanitized = MultipleDashRegex().Replace(sanitized, "-");
        sanitized = sanitized.Trim('-');

        return sanitized;
    }

    [GeneratedRegex(@"[^a-z0-9\s-]")]
    private static partial Regex KeywordRegex();

    [GeneratedRegex(@"-{2,}")]
    private static partial Regex MultipleDashRegex();

    [GeneratedRegex(@"/([\w]{16,})(?:#|$)")]
    private static partial Regex IdRegex();

    // Regex para extraer descripción de CompuTrabajo: busca el contenido del
    // <p class="mbB"> después de "Descripción de la oferta" (después del <h3>)
    [GeneratedRegex(@"Descripci.{0,5}n de la oferta</h3>[\s\S]*?<p[^>]*class=""mbB""[^>]*>([\s\S]*?)</p>", RegexOptions.IgnoreCase)]
    private static partial Regex DescriptionRegex();

    [GeneratedRegex(@"<[^>]+>")]
    private static partial Regex HtmlTagRegex();

    [GeneratedRegex(@"\s{2,}")]
    private static partial Regex MultipleSpaceRegex();

    private static string IdFromUrl(string url)
    {
        var match = IdRegex().Match(url);
        return match.Success ? match.Groups[1].Value : string.Empty;
    }
}
