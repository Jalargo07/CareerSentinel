using CareerSentinel.Configuration;
using CareerSentinel.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CareerSentinel.Services;

public class JobOrchestrator
{
    private readonly IEnumerable<IJobScraper> _scrapers;
    private readonly LocalLlmService _localLlmService;
    private readonly NotionService _notionService;
    private readonly TelegramAlertService _telegramAlertService;
    private readonly IJobCacheService _jobCacheService;
    private readonly AppSettings _settings;
    private readonly ILogger<JobOrchestrator> _logger;

    public JobOrchestrator(
        IEnumerable<IJobScraper> scrapers,
        LocalLlmService localLlmService,
        NotionService notionService,
        TelegramAlertService telegramAlertService,
        IJobCacheService jobCacheService,
        IOptions<AppSettings> settings,
        ILogger<JobOrchestrator> logger)
    {
        _scrapers = scrapers;
        _localLlmService = localLlmService;
        _notionService = notionService;
        _telegramAlertService = telegramAlertService;
        _jobCacheService = jobCacheService;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task RunAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Inicio del ciclo de orquestación de empleos");

        var allMatches = new List<(JobOffer Job, EvaluationResult Result)>();
        var threshold = _settings.Scoring.Threshold;

        // Count enabled scrapers
        var enabledScrapers = new List<(IJobScraper Scraper, JobSourceSettings Config)>();
        foreach (var scraper in _scrapers)
        {
            if (_settings.JobSources.TryGetValue(scraper.PortalName, out var config) && config.Enabled)
            {
                enabledScrapers.Add((scraper, config));
            }
            else
            {
                _logger.LogInformation("Scraper {PortalName} deshabilitado o sin configuración, saltando", scraper.PortalName);
            }
        }

        _logger.LogInformation("Scrapers habilitados: {Count}", enabledScrapers.Count);

        if (enabledScrapers.Count == 0)
        {
            _logger.LogWarning("No hay scrapers habilitados, terminando ciclo");
            return;
        }

        try
        {
            // Also pull from Notion to avoid duplicates with already-saved records
            var existingNotionIds = new HashSet<string>();
            try
            {
                existingNotionIds = await _notionService.GetExistingJobIdsAsync(ct);
                _logger.LogInformation("Se cargaron {Count} IDs existentes de Notion para deduplicación", existingNotionIds.Count);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "No se pudieron obtener IDs de Notion, se usa solo caché local");
            }

            foreach (var (scraper, sourceConfig) in enabledScrapers)
            {
                if (ct.IsCancellationRequested)
                {
                    _logger.LogInformation("Cancelación solicitada, deteniendo ciclo de orquestación");
                    break;
                }

                var keywords = sourceConfig.Keywords;
                var location = sourceConfig.Location;

                _logger.LogInformation(
                    "Iniciando scraping en {PortalName} con {KeywordCount} keywords, ubicación: {Location}",
                    scraper.PortalName, keywords.Count, location);

                foreach (var keyword in keywords)
                {
                    if (ct.IsCancellationRequested)
                    {
                        _logger.LogInformation("Cancelación solicitada, deteniendo ciclo de orquestación");
                        break;
                    }

                    _logger.LogInformation(
                        "[{PortalName}] Buscando ofertas con keyword: {Keyword} en {Location}",
                        scraper.PortalName, keyword, location);

                    List<JobOffer> offers;
                    try
                    {
                        offers = await scraper.SearchAsync(keyword, location, ct);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "[{PortalName}] Error al buscar ofertas con keyword: {Keyword}",
                            scraper.PortalName, keyword);
                        continue;
                    }

                    _logger.LogInformation(
                        "[{PortalName}] Se encontraron {Count} ofertas para keyword: {Keyword}",
                        scraper.PortalName, offers.Count, keyword);

                    foreach (var offer in offers)
                    {
                        if (ct.IsCancellationRequested) break;

                        try
                        {
                            // Deduplicate against local cache and Notion
                            if (await _jobCacheService.ContainsAsync(offer.Id, ct))
                            {
                                _logger.LogDebug("Oferta {Id} ya fue procesada, se omite", offer.Id);
                                continue;
                            }

                            if (existingNotionIds.Contains(offer.Id))
                            {
                                _logger.LogDebug("Oferta {Id} ya existe en Notion, se omite", offer.Id);
                                await _jobCacheService.AddSeenIdAsync(offer.Id, ct);
                                continue;
                            }

                            _logger.LogInformation(
                                "[{PortalName}] Procesando oferta nueva: {Title} @ {Company}",
                                scraper.PortalName, offer.Title, offer.Company);

                            // Rate limit before scraping description
                            await Task.Delay(_settings.RateLimiting.DelayBetweenRequestsMs, ct);

                            // Get full description
                            var description = await scraper.GetDescriptionAsync(offer.Url, ct);
                            var offerWithDescription = offer with { Description = description };

                            // Rate limit before LLM call
                            await Task.Delay(_settings.RateLimiting.DelayBetweenRequestsMs, ct);

                            // Evaluate with local LLM
                            var evaluation = await _localLlmService.EvaluateJobAsync(
                                description,
                                _settings.Scoring.CvText,
                                ct);

                            // Mark as seen regardless of evaluation result
                            await _jobCacheService.AddSeenIdAsync(offer.Id, ct);

                            if (evaluation is null)
                            {
                                _logger.LogWarning(
                                    "[{PortalName}] El LLM no pudo evaluar la oferta: {Title}",
                                    scraper.PortalName, offer.Title);
                                continue;
                            }

                            _logger.LogInformation(
                                "[{PortalName}] Evaluación completada: {Title} - Score: {Score}/{Threshold}",
                                scraper.PortalName, offer.Title, evaluation.Score, threshold);

                            if (evaluation.Score >= threshold)
                            {
                                _logger.LogInformation(
                                    "[{PortalName}] Match fuerte detectado: {Title} @ {Company} (Score: {Score})",
                                    scraper.PortalName, offer.Title, offer.Company, evaluation.Score);

                                allMatches.Add((offerWithDescription, evaluation));

                                // Save to Notion
                                await _notionService.SaveJobAsync(offerWithDescription, evaluation, ct);

                                // Send Telegram alert
                                await _telegramAlertService.SendAlertAsync(offerWithDescription, evaluation, ct);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex,
                                "[{PortalName}] Error al procesar oferta: {Title}",
                                scraper.PortalName, offer.Title);
                        }
                    }

                    // Rate limit between keyword searches
                    await Task.Delay(_settings.RateLimiting.DelayBetweenSearchesMs, ct);
                }
            }

            // Send daily summary
            _logger.LogInformation(
                "Ciclo completado. Total de matches fuertes: {Count}", allMatches.Count);

            if (allMatches.Count > 0)
            {
                await _telegramAlertService.SendDailySummaryAsync(allMatches, ct);
            }
            else
            {
                _logger.LogInformation("No se encontraron matches por encima del umbral de {Threshold}", threshold);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fatal en el ciclo de orquestación de empleos");
            throw;
        }
    }
}
