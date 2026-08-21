using CareerSentinel.Configuration;
using CareerSentinel.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CareerSentinel.Services;

public class JobOrchestrator
{
    private readonly IEnumerable<IJobScraper> _scrapers;
    private readonly ILlmService _llmService;
    private readonly NotionService _notionService;
    private readonly TelegramAlertService _telegramAlertService;
    private readonly IJobCacheService _jobCacheService;
    private readonly AppSettings _settings;
    private readonly ILogger<JobOrchestrator> _logger;

    public JobOrchestrator(
        IEnumerable<IJobScraper> scrapers,
        ILlmService llmService,
        NotionService notionService,
        TelegramAlertService telegramAlertService,
        IJobCacheService jobCacheService,
        IOptions<AppSettings> settings,
        ILogger<JobOrchestrator> logger)
    {
        _scrapers = scrapers;
        _llmService = llmService;
        _notionService = notionService;
        _telegramAlertService = telegramAlertService;
        _jobCacheService = jobCacheService;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<SearchResult> RunAsync(List<string>? scraperFilter = null, CancellationToken ct = default)
    {
        _logger.LogInformation("Inicio del ciclo de orquestación de empleos");

        var allMatches = new List<(JobOffer Job, EvaluationResult Result)>();
        var threshold = _settings.Scoring.Threshold;
        int totalProcessed = 0;
        int savedCount = 0;

        // Count enabled scrapers
        var activeScrapers = new List<(IJobScraper Scraper, JobSourceSettings Config)>();
        foreach (var scraper in _scrapers)
        {
            if (_settings.JobSources.TryGetValue(scraper.PortalName, out var config) && config.Enabled)
            {
                // Si se especificó un filtro, solo incluir esos scrapers
                if (scraperFilter is not null && !scraperFilter.Contains(scraper.PortalName))
                {
                    _logger.LogInformation("Scraper {PortalName} no seleccionado, saltando", scraper.PortalName);
                    continue;
                }
                activeScrapers.Add((scraper, config));
            }
            else
            {
                _logger.LogInformation("Scraper {PortalName} deshabilitado o sin configuración, saltando", scraper.PortalName);
            }
        }

        _logger.LogInformation("Scrapers activos: {Count}", activeScrapers.Count);

        if (activeScrapers.Count == 0)
        {
            _logger.LogWarning("No hay scrapers habilitados, terminando ciclo");
            return new SearchResult { TotalProcessed = 0, Matched = 0, Saved = 0 };
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

            foreach (var (scraper, sourceConfig) in activeScrapers)
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

                    // ============================================
                    // PIPELINE BATCH: Paso 1 + Paso 2 en lotes de 5
                    // ============================================
                    const int batchSize = 5;
                    var pendingBatch = new List<(JobOffer Offer, string Description)>();

                    // Primero: scraping individual + dedup (sin LLM)
                    foreach (var offer in offers)
                    {
                        if (ct.IsCancellationRequested) break;

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
                            "[{PortalName}] [Scrape] {Title} @ {Company}",
                            scraper.PortalName, offer.Title, offer.Company);

                        // Rate limit before scraping description
                        await Task.Delay(_settings.RateLimiting.DelayBetweenRequestsMs, ct);

                        // Get full description
                        var description = await scraper.GetDescriptionAsync(offer.Url, ct);

                        // Guardrail: descripción muy corta = login wall
                        if (string.IsNullOrWhiteSpace(description) || description.Length < 150)
                        {
                            _logger.LogWarning(
                                "[{PortalName}] Descripción insuficiente ({Length} chars), saltando: {Title}",
                                scraper.PortalName, description?.Length ?? 0, offer.Title);
                            await _jobCacheService.AddSeenIdAsync(offer.Id, ct);
                            continue;
                        }

                        pendingBatch.Add((offer, description));
                    }

                    _logger.LogInformation(
                        "[{PortalName}] Scraping completado: {Count} ofertas con descripción válida",
                        scraper.PortalName, pendingBatch.Count);

                    totalProcessed += pendingBatch.Count;

                    if (pendingBatch.Count == 0)
                    {
                        // Rate limit between keyword searches
                        await Task.Delay(_settings.RateLimiting.DelayBetweenSearchesMs, ct);
                        continue;
                    }

                    // Procesar en lotes de batchSize
                    var batchCount = (int)Math.Ceiling((double)pendingBatch.Count / batchSize);

                    for (int batchIdx = 0; batchIdx < pendingBatch.Count; batchIdx += batchSize)
                    {
                        if (ct.IsCancellationRequested) break;

                        var batch = pendingBatch.Skip(batchIdx).Take(batchSize).ToList();
                        var batchNum = (batchIdx / batchSize) + 1;

                        _logger.LogInformation(
                            "[{PortalName}] [Paso1 batch {Batch}/{Total}] Analizando {Count} ofertas",
                            scraper.PortalName, batchNum, batchCount, batch.Count);

                        // === PASO 1: Análisis batch (1 llamada API) ===
                        List<JobAnalysis?> analyses;
                        try
                        {
                            analyses = await _llmService.AnalyzeBatchAsync(
                                batch.Select(b => (b.Offer.Title, b.Description)).ToList(), ct);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex,
                                "[{PortalName}] Error en AnalyzeBatchAsync para lote {Batch}/{Total}",
                                scraper.PortalName, batchNum, batchCount);

                            // Mark all offers in failed batch as seen to avoid retrying endlessly
                            foreach (var (offer, _) in batch)
                            {
                                await _jobCacheService.AddSeenIdAsync(offer.Id, ct);
                            }
                            continue;
                        }

                        // Filtrar análisis válidos
                        var validAnalyses = new List<(JobOffer Offer, string Description, JobAnalysis Analysis)>();
                        for (int i = 0; i < batch.Count && i < analyses.Count; i++)
                        {
                            var analysis = analyses[i];
                            if (analysis is not null && analysis.EsTextoValido)
                            {
                                validAnalyses.Add((batch[i].Offer, batch[i].Description, analysis));
                            }
                            else
                            {
                                _logger.LogInformation(
                                    "[{PortalName}] Oferta inválida o sin análisis: {Title}",
                                    scraper.PortalName, batch[i].Offer.Title);
                                await _jobCacheService.AddSeenIdAsync(batch[i].Offer.Id, ct);
                            }
                        }

                        if (validAnalyses.Count == 0)
                        {
                            _logger.LogInformation(
                                "[{PortalName}] [Paso1 batch {Batch}/{Total}] Ninguna oferta válida, saltando Paso 2",
                                scraper.PortalName, batchNum, batchCount);
                            continue;
                        }

                        _logger.LogInformation(
                            "[{PortalName}] [Paso1 batch {Batch}/{TotalBatches}] {ValidCount}/{TotalInBatch} ofertas válidas",
                            scraper.PortalName, batchNum, batchCount, validAnalyses.Count, batch.Count);

                        // Rate limit antes del Paso 2
                        await Task.Delay(_settings.RateLimiting.DelayBetweenRequestsMs, ct);

                        // === PASO 2: Evaluación batch (1 llamada API) ===
                        _logger.LogInformation(
                            "[{PortalName}] [Paso2 batch {Batch}/{Total}] Evaluando {Count} ofertas",
                            scraper.PortalName, batchNum, batchCount, validAnalyses.Count);

                        var batchRequest = new BatchEvaluationRequest
                        {
                            Offers = validAnalyses.Select((v, i) => new OfferToEvaluate
                            {
                                Indice = i + 1,
                                Title = v.Analysis.Titulo,
                                Company = v.Analysis.Empresa,
                                Modality = v.Analysis.Modalidad,
                                Location = v.Analysis.Ubicacion,
                                Seniority = v.Analysis.SeniorityRequerido,
                                ExperienceYears = v.Analysis.AnosExperiencia,
                                Technologies = v.Analysis.TecnologiasClave,
                                Description = v.Analysis.DescripcionOriginal
                            }).ToList(),
                            Candidate = new CandidateInfo
                            {
                                Level = _settings.Candidate.Level,
                                YearsExperience = _settings.Candidate.YearsExperience.ToString(),
                                PreferredModality = _settings.Candidate.PreferredModality,
                                PreferredRegions = _settings.Candidate.PreferredRegions.ToList(),
                                Skills = _settings.Candidate.CoreSkills.ToList(),
                                CvDescription = _settings.Candidate.CvDescription
                            }
                        };

                        List<EvaluationResult> evaluations;
                        try
                        {
                            evaluations = await _llmService.EvaluateBatchAsync(batchRequest, ct);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex,
                                "[{PortalName}] Error en EvaluateBatchAsync para lote {Batch}/{Total}",
                                scraper.PortalName, batchNum, batchCount);

                            // Mark valid analyses as seen to avoid retrying endlessly
                            foreach (var (offer, _, _) in validAnalyses)
                            {
                                await _jobCacheService.AddSeenIdAsync(offer.Id, ct);
                            }
                            continue;
                        }

                        // Procesar resultados
                        for (int j = 0; j < validAnalyses.Count && j < evaluations.Count; j++)
                        {
                            var (offer, description, analysis) = validAnalyses[j];
                            var evaluation = evaluations[j];

                            await _jobCacheService.AddSeenIdAsync(offer.Id, ct);

                            _logger.LogInformation(
                                "[{PortalName}] [Paso2 batch {Batch}/{Total}] Evaluación: {Title} - Score: {Score} - Match: {Match}",
                                scraper.PortalName, batchNum, batchCount, offer.Title, evaluation.Score, evaluation.Match);

                            // Respetar la decisión del LLM: match=true significa que el LLM ya evaluó que cumple
                            if (evaluation.Match)
                            {
                                // Apply C# logic for "Cualquiera" modality
                                if (_settings.Candidate.PreferredModality == "Cualquiera")
                                {
                                    var isRemote = analysis.Modalidad.Equals("Remoto", StringComparison.OrdinalIgnoreCase);
                                    var isInPreferredRegion = _settings.Candidate.PreferredRegions.Any(r =>
                                        analysis.Ubicacion.Contains(r, StringComparison.OrdinalIgnoreCase));

                                    if (!isRemote && !isInPreferredRegion)
                                    {
                                        evaluation = evaluation with { Match = false, Score = 10 };
                                        _logger.LogInformation(
                                            "[{PortalName}] C# Filter: Oferta {Title} descartada - {Modality} fuera de regiones preferidas",
                                            scraper.PortalName, offer.Title, analysis.Modalidad);
                                    }
                                }
                            }

                            if (evaluation.Match)
                            {
                                _logger.LogInformation(
                                    "[{PortalName}] Match fuerte detectado: {Title} @ {Company} (Score: {Score})",
                                    scraper.PortalName, offer.Title, offer.Company, evaluation.Score);

                                var offerWithDescription = offer with { Description = analysis.DescripcionOriginal };
                                allMatches.Add((offerWithDescription, evaluation));

                                // Save to Notion
                                await _notionService.SaveJobAsync(offerWithDescription, evaluation, ct);
                                savedCount++;

                                // Send Telegram alert
                                await _telegramAlertService.SendAlertAsync(offerWithDescription, evaluation, ct);
                            }
                        }

                        // Rate limit entre batches
                        if (batchIdx + batchSize < pendingBatch.Count)
                        {
                            await Task.Delay(_settings.RateLimiting.DelayBetweenRequestsMs, ct);
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

        return new SearchResult
        {
            TotalProcessed = totalProcessed,
            Matched = allMatches.Count,
            Saved = savedCount
        };
    }
}
