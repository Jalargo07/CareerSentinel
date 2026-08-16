using System.Text.RegularExpressions;
using AngleSharp;
using AngleSharp.Html.Parser;
using CareerSentinel.Configuration;
using CareerSentinel.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CareerSentinel.Services;

public partial class LinkedInScraper : ILinkedInScraper
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<LinkedInScraper> _logger;
    private readonly LinkedInSettings _settings;
    private readonly RateLimitSettings _rateLimit;

    private static readonly string[] UserAgents =
    [
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36",
        "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36",
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:133.0) Gecko/20100101 Firefox/133.0",
        "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36",
    ];

    private static readonly Random Rng = new();

    public LinkedInScraper(
        IHttpClientFactory httpClientFactory,
        IOptions<AppSettings> settings,
        ILogger<LinkedInScraper> logger)
    {
        _httpClient = httpClientFactory.CreateClient("LinkedIn");
        _logger = logger;
        _settings = settings.Value.LinkedIn;
        _rateLimit = settings.Value.RateLimiting;
    }

    public async Task<List<JobOffer>> SearchAsync(string keyword, string location, CancellationToken ct = default)
    {
        var url = $"{_settings.BaseUrl}?keywords={Uri.EscapeDataString(keyword)}&location={Uri.EscapeDataString(location)}&start=0";

        _logger.LogInformation("Buscando LinkedIn: keyword={Keyword}, location={Location}", keyword, location);

        SetRandomUserAgent();

        try
        {
            var html = await _httpClient.GetStringAsync(url, ct);
            return ParseJobListings(html, keyword);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al buscar ofertas en LinkedIn para keyword={Keyword}", keyword);
            return [];
        }
    }

    public async Task<string> GetDescriptionAsync(string jobUrl, CancellationToken ct = default)
    {
        _logger.LogInformation("Obteniendo detalle: {Url}", jobUrl);

        SetRandomUserAgent();

        try
        {
            var html = await _httpClient.GetStringAsync(jobUrl, ct);
            var parser = new HtmlParser();
            var document = await parser.ParseDocumentAsync(html, ct);

            var descriptionElement = document.QuerySelector(".show-more-less-html__markup, .description__text, .job-description");
            return descriptionElement?.TextContent.Trim() ?? string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener descripcion de {Url}", jobUrl);
            return string.Empty;
        }
    }

    private List<JobOffer> ParseJobListings(string html, string keyword)
    {
        var jobs = new List<JobOffer>();
        var parser = new HtmlParser();
        var document = parser.ParseDocument(html);

        var cards = document.QuerySelectorAll(".base-card, .job-search-card, li.job-search-card");

        foreach (var card in cards)
        {
            var titleEl = card.QuerySelector(".base-search-card__title, h3");
            var companyEl = card.QuerySelector(".base-search-card__subtitle, h4");
            var linkEl = card.QuerySelector("a.base-card__full-link, a[href*='linkedin.com/jobs']");
            var dateEl = card.QuerySelector("time");
            var locationEl = card.QuerySelector(".job-search-card__location");

            if (titleEl is null || linkEl is null) continue;

            var href = linkEl.GetAttribute("href")?.Split('?')[0] ?? string.Empty;
            var id = IdFromUrl(href);

            if (string.IsNullOrEmpty(id)) continue;

            jobs.Add(new JobOffer
            {
                Id = id,
                Title = titleEl.TextContent.Trim(),
                Company = companyEl?.TextContent.Trim() ?? "Desconocida",
                Url = href,
                Location = locationEl?.TextContent.Trim() ?? string.Empty,
                PostedDate = dateEl?.GetAttribute("datetime") ?? string.Empty,
                SourceKeyword = keyword,
            });
        }

        _logger.LogInformation("Encontradas {Count} ofertas para keyword={Keyword}", jobs.Count, keyword);
        return jobs;
    }

    private void SetRandomUserAgent()
    {
        _httpClient.DefaultRequestHeaders.Remove("User-Agent");
        _httpClient.DefaultRequestHeaders.Add("User-Agent", UserAgents[Rng.Next(UserAgents.Length)]);
    }

    [GeneratedRegex(@"jobs-view\/(\d+)")]
    private static partial Regex JobIdRegex();

    private static string IdFromUrl(string url)
    {
        var match = JobIdRegex().Match(url);
        return match.Success ? match.Groups[1].Value : string.Empty;
    }
}