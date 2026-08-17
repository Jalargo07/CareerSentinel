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
    }

    public async Task<List<JobOffer>> SearchAsync(string keyword, string location, CancellationToken ct = default)
    {
        var sanitizedKeyword = SanitizeKeyword(keyword);
        var url = $"{BaseSearchUrl}/trabajo-de-{sanitizedKeyword}";

        _logger.LogInformation("Buscando CompuTrabajo: keyword={Keyword}, location={Location}, url={Url}",
            keyword, location, url);

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
            var html = await _httpClient.GetStringAsync(jobUrl, ct);
            var parser = new HtmlParser();
            var document = await parser.ParseDocumentAsync(html, ct);

            var descriptionElement = document.QuerySelector(".fc-base, .box_description, .job-description, #description");
            return descriptionElement?.TextContent.Trim() ?? string.Empty;
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

        var cards = document.QuerySelectorAll("div.box_list, article.box_list, div.bx, li.jobs-list-item");

        foreach (var card in cards)
        {
            var titleEl = card.QuerySelector("a.js-o-link, h2 a, h3 a, a[href*='computrabajo']");
            var companyEl = card.QuerySelector("a.empresa, p.empresa, span.company-name, div.fc_base a");
            var locationEl = card.QuerySelector("span.location, p.location, span.fc_base span");
            var dateEl = card.QuerySelector("span.fecha, time, span.date, span.time");
            var linkEl = card.QuerySelector("a.js-o-link, h2 a, h3 a");

            if (titleEl is null || linkEl is null) continue;

            var href = linkEl.GetAttribute("href") ?? string.Empty;
            if (!href.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                href = $"{BaseSearchUrl}{href}";
            }

            var id = IdFromUrl(href);

            if (string.IsNullOrEmpty(id)) continue;

            jobs.Add(new JobOffer
            {
                Id = id,
                Title = titleEl.TextContent.Trim(),
                Company = companyEl?.TextContent.Trim() ?? "Desconocida",
                Url = href,
                Location = locationEl?.TextContent.Trim() ?? location,
                PostedDate = dateEl?.TextContent.Trim() ?? string.Empty,
                SourceKeyword = keyword,
            });
        }

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

    [GeneratedRegex(@"[^a-z0-9\s-]")]
    private static partial Regex KeywordRegex();

    [GeneratedRegex(@"-{2,}")]
    private static partial Regex MultipleDashRegex();

    [GeneratedRegex(@"ofertas-de-trabajo-de-.*?\/([\w-]+)\/(\d+)")]
    private static partial Regex CompuTrabajoIdRegex();

    [GeneratedRegex(@"\/([\w-]+)\/(\d+)")]
    private static partial Regex GenericIdRegex();

    private static string IdFromUrl(string url)
    {
        var match = CompuTrabajoIdRegex().Match(url);
        if (match.Success)
        {
            return match.Groups[2].Value;
        }

        var genericMatch = GenericIdRegex().Match(url);
        if (genericMatch.Success)
        {
            return genericMatch.Groups[2].Value;
        }

        return string.Empty;
    }
}
