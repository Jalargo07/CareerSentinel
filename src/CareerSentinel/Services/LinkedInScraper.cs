using System.Net;
using System.Text.RegularExpressions;
using AngleSharp.Html.Parser;
using CareerSentinel.Configuration;
using CareerSentinel.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CareerSentinel.Services;

public partial class LinkedInScraper : IJobScraper
{
    public string PortalName => "LinkedIn";

    private readonly HttpClient _httpClient;
    private readonly ILogger<LinkedInScraper> _logger;
    private readonly LinkedInSettings _settings;
    private readonly RateLimitSettings _rateLimit;
    private readonly CandidateProfile _candidateProfile;
    private readonly HtmlParser _htmlParser;
    private readonly ILinkedInAuthService _authService;

    private const int MaxAuthWallRetries = 2;

    private static readonly string[] UserAgents =
    [
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/133.0.0.0 Safari/537.36",
        "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/133.0.0.0 Safari/537.36",
        "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/133.0.0.0 Safari/537.36"
    ];

    public LinkedInScraper(
        IHttpClientFactory httpClientFactory,
        IOptions<AppSettings> settings,
        ILogger<LinkedInScraper> logger,
        ILinkedInAuthService authService)
    {
        _httpClient = httpClientFactory.CreateClient("LinkedIn");
        _logger = logger;
        _settings = settings.Value.LinkedIn;
        _rateLimit = settings.Value.RateLimiting;
        _candidateProfile = settings.Value.Candidate;
        _htmlParser = new HtmlParser();
        _authService = authService;
    }

    public async Task<List<JobOffer>> SearchAsync(string keyword, string location, CancellationToken ct = default)
    {
        // Asegurar autenticación antes de hacer requests
        await _authService.EnsureAuthenticatedAsync(ct);

        // Obtener cookies de autenticación
        var cookies = await _authService.GetCookiesAsync(ct);
        var cookieHeader = string.Join("; ", cookies.Select(c => $"{c.Name}={c.Value}"));

        // Determinar niveles de experiencia segun el perfil del candidato
        var experienceFilter = _candidateProfile.YearsExperience switch
        {
            <= 2 => "f_E=2",      // Entry Level
            <= 4 => "f_E=2,3",    // Entry + Associate
            _ => "f_E=3,4"        // Associate + Mid-Senior
        };

        var keywordQuery = Uri.EscapeDataString($"\"{keyword}\"");
        var locationQuery = Uri.EscapeDataString(string.Join(" OR ", _candidateProfile.PreferredRegions));
        
        var url = $"{_settings.BaseUrl}?" +
                  $"keywords={keywordQuery}" +
                  $"&location={locationQuery}" +
                  $"&{experienceFilter}" +
                  $"&f_WT=2" + // Solo remotos
                  $"&start=0";

        _logger.LogInformation("Buscando en LinkedIn: Keyword='{Keyword}', Nivel='{Level}'", keyword, _candidateProfile.Level);

        await RandomDelayAsync(ct);

        try
        {
            var authwallRetries = 0;
            bool authwallDetected;

            do
            {
                authwallDetected = false;

                using var request = BuildHttpRequest(url, cookieHeader);
                using var response = await _httpClient.SendAsync(request, ct);

                if (response.StatusCode == HttpStatusCode.TooManyRequests)
                {
                    _logger.LogWarning("Rate limit (429) alcanzado en LinkedIn para {Keyword}.", keyword);
                    return [];
                }

                response.EnsureSuccessStatusCode();

                var html = await response.Content.ReadAsStringAsync(ct);

                if (ContainsAuthWall(html))
                {
                    authwallRetries++;
                    authwallDetected = true;

                    _logger.LogWarning(
                        "LinkedIn authwall detectado para '{Keyword}' (intento {Retry}/{Max}). Re-autenticando...",
                        keyword, authwallRetries, MaxAuthWallRetries);

                    if (authwallRetries >= MaxAuthWallRetries)
                    {
                        _logger.LogWarning(
                            "LinkedIn authwall persistente después de {Max} reintentos para '{Keyword}'. " +
                            "Fallback: se omitirá LinkedIn (use CompuTrabajo como alternativa).",
                            MaxAuthWallRetries, keyword);
                        return [];
                    }

                    // Re-autenticar y regenerar cookies
                    await _authService.EnsureAuthenticatedAsync(ct);
                    var freshCookies = await _authService.GetCookiesAsync(ct);
                    cookieHeader = string.Join("; ", freshCookies.Select(c => $"{c.Name}={c.Value}"));

                    // Delay adicional antes de reintentar
                    await RandomDelayAsync(ct);
                }
                else
                {
                    return await ParseJobListingsAsync(html, keyword, ct);
                }
            } while (authwallDetected);

            // Nunca debería llegar aquí, pero por seguridad
            return [];
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al procesar la busqueda en LinkedIn para Keyword='{Keyword}'", keyword);
            return [];
        }
    }

    public async Task<string> GetDescriptionAsync(string jobUrl, CancellationToken ct = default)
    {
        _logger.LogInformation("Obteniendo descripcion detallada: {Url}", jobUrl);

        // Obtener cookies de autenticación
        var cookies = await _authService.GetCookiesAsync(ct);
        var cookieHeader = string.Join("; ", cookies.Select(c => $"{c.Name}={c.Value}"));

        await RandomDelayAsync(ct);

        try
        {
            var authwallRetries = 0;
            bool authwallDetected;

            do
            {
                authwallDetected = false;

                using var request = BuildHttpRequest(jobUrl, cookieHeader);
                using var response = await _httpClient.SendAsync(request, ct);

                if (response.StatusCode == HttpStatusCode.TooManyRequests)
                {
                    _logger.LogWarning("HTTP 429 (Too Many Requests) al obtener descripcion de {Url}", jobUrl);
                    return string.Empty;
                }

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("HTTP {StatusCode} al obtener descripcion de {Url}", response.StatusCode, jobUrl);
                    return string.Empty;
                }

                var html = await response.Content.ReadAsStringAsync(ct);
                _logger.LogInformation("HTML de descripcion: {Length} caracteres", html.Length);

                if (ContainsAuthWall(html))
                {
                    authwallRetries++;
                    authwallDetected = true;

                    _logger.LogWarning(
                        "LinkedIn authwall detectado al obtener descripción de {Url} (intento {Retry}/{Max}). Re-autenticando...",
                        jobUrl, authwallRetries, MaxAuthWallRetries);

                    if (authwallRetries >= MaxAuthWallRetries)
                    {
                        _logger.LogWarning(
                            "LinkedIn authwall persistente después de {Max} reintentos para descripción de {Url}. " +
                            "Retornando cadena vacía.",
                            MaxAuthWallRetries, jobUrl);
                        return string.Empty;
                    }

                    // Re-autenticar y regenerar cookies
                    await _authService.EnsureAuthenticatedAsync(ct);
                    var freshCookies = await _authService.GetCookiesAsync(ct);
                    cookieHeader = string.Join("; ", freshCookies.Select(c => $"{c.Name}={c.Value}"));

                    // Delay adicional antes de reintentar
                    await RandomDelayAsync(ct);
                    continue;
                }

                using var document = await _htmlParser.ParseDocumentAsync(html, ct);

                var descriptionElement = document.QuerySelector(".show-more-less-html__markup, .description__text, .job-description, .markup");
                var description = descriptionElement?.TextContent.Trim() ?? string.Empty;

                // Limpiar artefactos de LinkedIn y normalizar espacios
                description = description
                    .Replace("Show more", "")
                    .Replace("Show less", "");
                description = Regex.Replace(description, @"\s+", " ").Trim();

                _logger.LogInformation("Descripcion extraida: {Length} caracteres", description.Length);
            
                if (description.Length > 0)
                {
                    _logger.LogDebug("Preview descripcion: {Preview}", description[..Math.Min(200, description.Length)]);
                }

                return description;
            } while (authwallDetected);

            // Nunca debería llegar aquí
            return string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al extraer descripcion de la oferta {Url}", jobUrl);
            return string.Empty;
        }
    }

    private static bool ContainsAuthWall(string html)
    {
        if (string.IsNullOrEmpty(html))
            return false;

        // Normalizar a minúsculas para comparación case-insensitive
        var htmlLower = html.Length > 10_000
            ? html[..10_000].ToLowerInvariant()   // Solo revisar los primeros 10K chars
            : html.ToLowerInvariant();

        return htmlLower.Contains("authwall")
            || htmlLower.Contains("captcha")
            || htmlLower.Contains("linkedinlogin");
    }

    private async Task<List<JobOffer>> ParseJobListingsAsync(string html, string keyword, CancellationToken ct)
    {
        var jobs = new List<JobOffer>();
        using var document = await _htmlParser.ParseDocumentAsync(html, ct);

        // Log: tamaño del HTML
        _logger.LogInformation("HTML recibido: {Length} caracteres", html.Length);

        var cards = document.QuerySelectorAll("li, .base-card, .job-search-card");
        _logger.LogInformation("Cards encontrados por CSS selector: {Count}", cards.Count);

        // Log: primeros 500 chars del HTML para diagnóstico
        if (html.Length > 0)
        {
            var preview = html.Length > 500 ? html[..500] : html;
            _logger.LogDebug("Preview del HTML: {Preview}", preview);
        }

        int skippedTitle = 0, skippedLink = 0, skippedId = 0, skippedSeniorFilter = 0, skippedLocationFilter = 0;

        foreach (var card in cards)
        {
            var titleEl = card.QuerySelector(".base-search-card__title, .job-search-card__title, h3");
            var companyEl = card.QuerySelector(".base-search-card__subtitle, .job-search-card__subtitle, h4");
            var linkEl = card.QuerySelector("a[href*='/jobs/view/'], a.base-card__full-link");
            var dateEl = card.QuerySelector("time");
            var locationEl = card.QuerySelector(".job-search-card__location");

            if (titleEl is null) { skippedTitle++; continue; }
            if (linkEl is null) { skippedLink++; continue; }

            var rawHref = linkEl.GetAttribute("href") ?? string.Empty;
            var cleanUrl = rawHref.Split('?')[0].Trim();
            var id = IdFromUrl(cleanUrl);

            if (string.IsNullOrEmpty(id)) { skippedId++; continue; }

            var title = titleEl.TextContent.Trim();
            var locText = locationEl?.TextContent.Trim() ?? string.Empty;
            var description = card.QuerySelector(".job-search-card__snippet")?.TextContent.Trim() ?? string.Empty;

            // Log: cada oferta encontrada (Debug para evitar ruido en loops)
            _logger.LogDebug("Oferta encontrada: {Title} | {Location} | ID={Id}", title, locText, id);

            // Pre-filtro: descartar ofertas Senior si el candidato es Junior/Mid
            if (!ShouldEvaluateJob(title, description))
            {
                skippedSeniorFilter++;
                _logger.LogWarning("FILTRO SENIOR: {Title} | Descartada", title);
                continue;
            }

            // Filtro geográfico
            if (!IsValidRegionAndModality(locText, title, description))
            {
                skippedLocationFilter++;
                _logger.LogWarning("FILTRO UBICACION: {Title} en {Location} | Descartada", title, locText);
                continue;
            }

            // Si pasó ambos filtros (Debug para evitar ruido en loops)
            _logger.LogDebug("FILTRO OK: {Title} en {Location} | Aceptada", title, locText);

            jobs.Add(new JobOffer
            {
                Id = id,
                Title = title,
                Company = companyEl?.TextContent.Trim() ?? "Desconocida",
                Url = cleanUrl,
                Location = locText,
                PostedDate = dateEl?.GetAttribute("datetime") ?? string.Empty,
                SourceKeyword = keyword
            });
        }

        _logger.LogInformation("Resumen parsing: {Total} cards | {Valid} válidos | {SkipTitle} sin título | {SkipLink} sin link | {SkipId} sin ID | {SkipSenior} por Senior | {SkipLocation} por Ubicación",
            cards.Count, jobs.Count, skippedTitle, skippedLink, skippedId, skippedSeniorFilter, skippedLocationFilter);

        return jobs;
    }

    private bool ShouldEvaluateJob(string title, string description)
    {
        // Keywords que indican nivel superior al que buscas (Junior/Entry)
        var excludeKeywords = new[] { 
            // Nivel Senior+
            "senior", "sr.", "sr ", "lead", "principal", "architect", "staff", "director", 
            "head of", "vp of", "cto", "chief", "expert",
            // Años de experiencia explícitos
            "5+ years", "5+ anios", "5+ años", "7+ years", "7+ anios", "7+ años",
            "10+ years", "10+ anios", "10+ años", "8+ years", "8+ anios", "8+ años",
            "3+ years", "3+ anios", "3+ años", // Mid-level threshold
            // Nivel explícito
            "mid-level", "mid level", "mid senior", "pleno", // pleno = mid en portugués
            "experienced", "specialist"
        };
        
        var combinedText = $"{title} {description}".ToLowerInvariant();

        foreach (var kw in excludeKeywords)
        {
            if (combinedText.Contains(kw))
            {
                _logger.LogWarning("ShouldEvaluateJob: '{Keyword}' encontrado en '{Text}'", kw, combinedText[..Math.Min(80, combinedText.Length)]);
                return false;
            }
        }

        return true;
    }

    private static HttpRequestMessage BuildHttpRequest(string url, string? cookieHeader = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        var randomAgent = UserAgents[Random.Shared.Next(UserAgents.Length)];

        request.Headers.Add("User-Agent", randomAgent);
        request.Headers.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8");
        request.Headers.Add("Accept-Language", "es-ES,es;q=0.9,en;q=0.8");
        request.Headers.Add("Sec-Fetch-Dest", "document");
        request.Headers.Add("Sec-Fetch-Mode", "navigate");
        request.Headers.Add("Sec-Fetch-Site", "none");
        request.Headers.Add("Sec-Fetch-User", "?1");

        if (!string.IsNullOrEmpty(cookieHeader))
        {
            request.Headers.Add("Cookie", cookieHeader);
        }

        return request;
    }

    private static async Task RandomDelayAsync(CancellationToken ct)
    {
        var delayMs = Random.Shared.Next(2500, 5000);
        await Task.Delay(delayMs, ct);
    }

    [GeneratedRegex(@"-(\d{8,})")]
    private static partial Regex JobIdRegex();

    private static string IdFromUrl(string url)
    {
        var match = JobIdRegex().Match(url);
        return match.Success ? match.Groups[1].Value : string.Empty;
    }

    private static bool IsValidRegionAndModality(string location, string title, string description)
    {
        // NOTA: LinkedIn YA filtra remotas con f_WT=2 en la query.
        // Este método solo verifica que la ubicación no sea claramente incompatible.

        if (string.IsNullOrWhiteSpace(location)) return true; // Si no hay ubicación, asumir válido

        var locLower = location.ToLowerInvariant();

        // Descartar solo ubicaciones claramente presenciales en países no deseados
        // (que NO estén en Colombia, LATAM ni Europa)
        var blockedLocations = new[] { "china", "india", "japan", "korea", "russia",
                                       "australia", "africa", "middle east" };

        if (blockedLocations.Any(blocked => locLower.Contains(blocked)))
        {
            return false;
        }

        // Todo lo demás se acepta (LinkedIn ya filtró remotas con f_WT=2)
        return true;
    }
}
