using CareerSentinel.Configuration;
using CareerSentinel.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CareerSentinel.Services;

public class JobOrchestrator
{
    private readonly ILinkedInScraper _scraper;
    private readonly LocalLlmService _llmService;
    private readonly NotionService _notionService;
    private readonly TelegramAlertService _telegramService;
    private readonly IJobCacheService _cacheService;
    private readonly AppSettings _settings;
    private readonly ILogger<JobOrchestrator> _logger;

    public JobOrchestrator(
        ILinkedInScraper scraper,
        LocalLlmService llmService,
        NotionService notionService,
        TelegramAlertService telegramService,
        IJobCacheService cacheService,
        IOptions<AppSettings> settings,
        ILogger<JobOrchestrator> logger)
    {
        _scraper = scraper;
        _llmService = llmService;
        _notionService = notionService;
        _telegramService = telegramService;
        _cacheService = cacheService;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task RunAsync(CancellationToken ct = default)
    {
        var allJobs = new List<JobOffer>();
        var matchedJobs = new List<(JobOffer Job, EvaluationResult Result)>();

        // Phase 1: Scrape LinkedIn for each keyword
        foreach (var keyword in _settings.LinkedIn.Keywords)
        {
            _logger.LogInformation("Buscando: {Keyword}", keyword);

            var jobs = await _scraper.SearchAsync(keyword, _settings.LinkedIn.Location, ct);
            allJobs.AddRange(jobs);

            await Task.Delay(_settings.RateLimiting.DelayBetweenSearchesMs, ct);
        }

        Console.WriteLine($"  Encontradas {allJobs.Count} ofertas en total");

        // Phase 2: Dedup against cache
        var newJobs = new List<JobOffer>();
        foreach (var job in allJobs)
        {
            if (!await _cacheService.ContainsAsync(job.Id, ct))
            {
                newJobs.Add(job);
            }
        }

        Console.WriteLine($"  {newJobs.Count} ofertas nuevas (sin duplicados)");

        // Phase 3: Process each new job
        int processed = 0;
        foreach (var job in newJobs)
        {
            processed++;
            Console.Write($"  [{processed}/{newJobs.Count}] {job.Title}... ");

            // Get full description
            var description = await _scraper.GetDescriptionAsync(job.Url, ct);
            await Task.Delay(_settings.RateLimiting.DelayBetweenRequestsMs, ct);

            if (string.IsNullOrWhiteSpace(description))
            {
                Console.WriteLine("SKIP (sin descripcion)");
                await _cacheService.AddSeenIdAsync(job.Id, ct);
                continue;
            }

            // Evaluate with LLM
            var result = await _llmService.EvaluateJobAsync(description, _settings.Scoring.CvText, ct);
            await _cacheService.AddSeenIdAsync(job.Id, ct);

            if (result is null)
            {
                Console.WriteLine("SKIP (error LLM)");
                continue;
            }

            Console.WriteLine($"Score: {result.Score}");

            // Save to Notion if above threshold
            if (result.Score >= _settings.Scoring.Threshold)
            {
                await _notionService.SaveJobAsync(job, result, ct);
                await _telegramService.SendAlertAsync(job, result, ct);
                matchedJobs.Add((job, result));
            }
        }

        // Phase 4: Send daily summary
        if (matchedJobs.Count > 0)
        {
            await _telegramService.SendDailySummaryAsync(matchedJobs, ct);
        }

        Console.WriteLine();
        Console.WriteLine($"  Resumen: {processed} procesadas, {matchedJobs.Count} matches (umbral: {_settings.Scoring.Threshold})");
    }
}