using CareerSentinel.Configuration;
using CareerSentinel.Models;
using CareerSentinel.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Extensions.Http;

// ---------------------------------------------------------------------------
// 0. Ensure appsettings.json exists (first-time user experience)
// ---------------------------------------------------------------------------
EnsureAppSettingsExists();

// ---------------------------------------------------------------------------
// 1. Build configuration (appsettings.json + environment overlay + User Secrets)
// ---------------------------------------------------------------------------
var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false)
    .AddJsonFile(
        $"appsettings.{Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Production"}.json",
        optional: true)
    .AddUserSecrets<Program>(optional: true)
    .Build();

// ---------------------------------------------------------------------------
// 2. Read processing mode from config
// ---------------------------------------------------------------------------
var processingMode = configuration.GetValue<string>("AppSettings:ProcessingMode") ?? "Local";
Console.WriteLine($"[Config] ProcessingMode: {processingMode}");

// ---------------------------------------------------------------------------
// 3. ServiceCollection + ServiceProvider
// ---------------------------------------------------------------------------
var services = new ServiceCollection();

// 4. Bind AppSettings via IOptions<AppSettings>
services.Configure<AppSettings>(configuration.GetSection("AppSettings"));

// Logging
services.AddLogging(builder => builder.AddConsole());

// ---------------------------------------------------------------------------
// 4-6. Named HttpClients with Polly policies
// ---------------------------------------------------------------------------

// Shared policies
var jitterRandom = new Random();
var retryPolicy = HttpPolicyExtensions
    .HandleTransientHttpError()
    .WaitAndRetryAsync(
        retryCount: 3,
        sleepDurationProvider: retryAttempt =>
        {
            var baseDelay = Math.Pow(2, retryAttempt);
            var jitter = 1 + jitterRandom.NextDouble() * 0.2; // +20% randomness
            return TimeSpan.FromSeconds(baseDelay * jitter);
        },
        onRetry: (outcome, delay, retryCount, _) =>
        {
            Console.WriteLine($"  [Polly] Retry {retryCount}/3 - waiting {delay.TotalSeconds:F1}s");
        });

var circuitBreakerPolicy = HttpPolicyExtensions
    .HandleTransientHttpError()
    .CircuitBreakerAsync(
        handledEventsAllowedBeforeBreaking: 5,
        durationOfBreak: TimeSpan.FromSeconds(30),
        onBreak: (_, breakDelay) =>
        {
            Console.WriteLine($"  [Polly] Circuit OPEN - pausing for {breakDelay.TotalSeconds:F0}s");
        },
        onReset: () =>
        {
            Console.WriteLine("  [Polly] Circuit CLOSED - normal operation resumed");
        },
        onHalfOpen: () =>
        {
            Console.WriteLine("  [Polly] Circuit HALF-OPEN - testing...");
        });

// 4. LinkedIn HttpClient: retry + circuit breaker
services.AddHttpClient("LinkedIn")
    .AddPolicyHandler(retryPolicy)
    .AddPolicyHandler(circuitBreakerPolicy);

// 5. Ollama HttpClient: retry only (local, no circuit breaker needed)
services.AddHttpClient("Ollama", client =>
{
    client.Timeout = TimeSpan.FromSeconds(120);
})
    .AddPolicyHandler(retryPolicy);

// 6a. Notion HttpClient: retry + circuit breaker
services.AddHttpClient("Notion")
    .AddPolicyHandler(retryPolicy)
    .AddPolicyHandler(circuitBreakerPolicy);

// 6b. Telegram HttpClient: retry + circuit breaker
services.AddHttpClient("Telegram")
    .AddPolicyHandler(retryPolicy)
    .AddPolicyHandler(circuitBreakerPolicy);

// 6c. CompuTrabajo HttpClient: retry only
services.AddHttpClient("CompuTrabajo", client =>
{
    client.DefaultRequestHeaders.Add("Accept-Language", "es-CO,es;q=0.9");
    client.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
})
    .AddPolicyHandler(retryPolicy);

// 6d. OpenCodeGo HttpClient: retry + circuit breaker
services.AddHttpClient("OpenCodeGo", (sp, client) =>
{
    var settings = sp.GetRequiredService<IOptions<AppSettings>>().Value.OpenCodeGo;
    client.BaseAddress = new Uri(settings.BaseUrl);
    client.Timeout = TimeSpan.FromSeconds(settings.TimeoutSeconds);

    if (!string.IsNullOrEmpty(settings.ApiKey))
    {
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {settings.ApiKey}");
    }
})
    .AddPolicyHandler(retryPolicy)
    .AddPolicyHandler(circuitBreakerPolicy);

// ---------------------------------------------------------------------------
// 7. Register all services
// ---------------------------------------------------------------------------

// Configuration persistence service
services.AddSingleton<ConfigurationService>();

// LinkedIn Authentication services
services.AddSingleton<CookiesManager>();
services.AddSingleton<ILinkedInAuthService, LinkedInAuthService>();

// Async file logger for evaluation logs (shared across LLM services)
var evaluationLogPath = Path.Combine(AppContext.BaseDirectory, "logs", "evaluaciones.log");
services.AddSingleton(new AsyncFileLogger(evaluationLogPath));

// Job scrapers (IJobScraper - polymorphic resolution)
services.AddTransient<IJobScraper, LinkedInScraper>();
services.AddTransient<IJobScraper, CompuTrabajoScraper>();

// Register all LLM services
services.AddSingleton<LocalLlmService>();
services.AddSingleton<OpenCodeGoService>();
services.AddSingleton<HybridLlmService>();

// Register ILlmService factory for runtime resolution
// This allows the ProcessingMode to be changed at runtime via ConsoleMenu
// without requiring an application restart.
services.AddSingleton<ILlmService>(sp =>
{
    var settings = sp.GetRequiredService<IOptions<AppSettings>>().Value;
    return settings.ProcessingMode switch
    {
        ProcessingMode.Local => sp.GetRequiredService<LocalLlmService>(),
        ProcessingMode.API => sp.GetRequiredService<OpenCodeGoService>(),
        ProcessingMode.Hybrid => sp.GetRequiredService<HybridLlmService>(),
        _ => sp.GetRequiredService<OpenCodeGoService>()
    };
});

services.AddSingleton<NotionService>();
services.AddSingleton<TelegramAlertService>();
services.AddSingleton<IJobCacheService, JobCacheService>();
services.AddSingleton<JobOrchestrator>();

// 8. Build provider and resolve root services
var provider = services.BuildServiceProvider();
var orchestrator = provider.GetRequiredService<JobOrchestrator>();
var settings = provider.GetRequiredService<IOptions<AppSettings>>().Value;
var configService = provider.GetRequiredService<ConfigurationService>();
var linkedInAuthService = provider.GetRequiredService<ILinkedInAuthService>();

// ---------------------------------------------------------------------------
// 9. First-time setup wizard
// ---------------------------------------------------------------------------
if (ConsoleMenu.IsFirstTimeSetup(settings))
{
    ConsoleMenu.ShowSetupWizard(settings, configService);
}

// ---------------------------------------------------------------------------
// 10. Main menu loop
// ---------------------------------------------------------------------------
ConsoleMenu.ShowMessage("CareerSentinel v1.0 - Buscador Inteligente de Empleo");

// Ctrl+C handling for clean exit
var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    ConsoleMenu.ShowMessage("Saliendo...");
    cts.Cancel();
};

while (true)
{
    if (cts.Token.IsCancellationRequested)
        break;

    var option = ConsoleMenu.ShowMainMenu();

    switch (option)
    {
        // Option 1: Configure Telegram
        case 1:
            ConsoleMenu.ShowTelegramConfig(settings);
            configService.Save(settings);
            Console.WriteLine("  ✅ Configuración guardada en appsettings.json");
            break;

        // Option 2: Configure candidate profile
        case 2:
            ConsoleMenu.ShowCandidateConfig(settings);
            configService.Save(settings);
            Console.WriteLine("  ✅ Configuración guardada en appsettings.json");
            break;

        // Option 3: Configure LLM
        case 3:
            ConsoleMenu.ShowLlmConfig(settings);
            configService.Save(settings);
            Console.WriteLine("  ✅ Configuración guardada en appsettings.json");
            break;

        // Option 4: Run full search (all sources)
        case 4:
            var result4 = await RunSearchAsync(orchestrator, "Iniciando búsqueda completa...", null, "Búsqueda completada exitosamente.");
            if (result4 is not null)
                HandlePostSearchMenu(result4, orchestrator, settings, configService);
            break;

        // Option 5: Run only LinkedIn
        case 5:
            var result5 = await RunSearchAsync(orchestrator, "Iniciando búsqueda en SOLO LinkedIn...", new List<string> { "LinkedIn" }, "Búsqueda en LinkedIn completada exitosamente.");
            if (result5 is not null)
                HandlePostSearchMenu(result5, orchestrator, settings, configService);
            break;

        // Option 6: Run only CompuTrabajo
        case 6:
            var result6 = await RunSearchAsync(orchestrator, "Iniciando búsqueda en SOLO CompuTrabajo...", new List<string> { "CompuTrabajo" }, "Búsqueda en CompuTrabajo completada exitosamente.");
            if (result6 is not null)
                HandlePostSearchMenu(result6, orchestrator, settings, configService);
            break;

        // Option 7: Show current configuration (read-only)
        case 7:
            ConsoleMenu.ShowConfig(settings);
            break;

        // Option 8: Edit candidate profile
        case 8:
            ConsoleMenu.ShowCandidateConfig(settings);
            configService.Save(settings);
            Console.WriteLine("  ✅ Configuración guardada en appsettings.json");
            break;

        // Option 9: Edit LLM / provider
        case 9:
            ConsoleMenu.ShowLlmConfig(settings);
            configService.Save(settings);
            Console.WriteLine("  ✅ Configuración guardada en appsettings.json");
            break;

        // Option 0: Sources submenu
        case 0:
            ConsoleMenu.ShowSourcesSubmenu(settings, configService);
            break;

        // Option S: LinkedIn Authentication
        case ConsoleMenu.LinkedInAuthOption:
            await ConsoleMenu.ShowLinkedInAuthMenu(linkedInAuthService);
            break;
    }
}

// ---------------------------------------------------------------------------
// Local helper functions
// ---------------------------------------------------------------------------

static async Task<SearchResult?> RunSearchAsync(JobOrchestrator orchestrator, string message, List<string>? sources, string successMessage)
{
    ConsoleMenu.ShowMessage(message);
    try
    {
        var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

        var result = await orchestrator.RunAsync(sources, cts.Token);

        ConsoleMenu.ShowMessage(successMessage);
        return result;
    }
    catch (OperationCanceledException)
    {
        ConsoleMenu.ShowMessage("Búsqueda cancelada por el usuario.");
        return null;
    }
    catch (Exception ex)
    {
        ConsoleMenu.ShowMessage($"Error durante la búsqueda: {ex.Message}");
        return null;
    }
}

static void HandlePostSearchMenu(SearchResult result, JobOrchestrator orchestrator, AppSettings settings, ConfigurationService configService)
{
    while (true)
    {
        var option = ConsoleMenu.ShowResultsMenu(result.TotalProcessed, result.Matched, result.Saved);

        switch (option)
        {
            case 1:
                ConsoleMenu.ShowMessage("Revisa Notion para ver las ofertas guardadas.");
                break;

            case 2:
                return; // Volver al loop principal para re-elegir opción de búsqueda

            case 3:
                ConsoleMenu.ShowCandidateConfig(settings);
                configService.Save(settings);
                Console.WriteLine("  ✅ Configuración guardada en appsettings.json");
                break;

            case 4:
                ConsoleMenu.ShowConfig(settings);
                break;

            case 0:
                return; // Volver al menú principal
        }
    }
}

static void EnsureAppSettingsExists()
{
    var path = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
    if (File.Exists(path)) return;

    var defaultConfig = @"{
  ""AppSettings"": {
    ""ProcessingMode"": ""API"",
    ""Ollama"": {
      ""BaseUrl"": ""http://localhost:11434"",
      ""ModelName"": ""qwen2.5:3b""
    },
    ""OpenCodeGo"": {
      ""BaseUrl"": ""https://generativelanguage.googleapis.com/v1beta/openai/"",
      ""ModelName"": ""gemini-3.5-flash-lite"",
      ""MaxConcurrentRequests"": 2,
      ""BatchSize"": 5,
      ""TimeoutSeconds"": 120,
      ""MaxTokensBatch"": 3500,
      ""ApiKey"": """"
    },
    ""LinkedIn"": {
      ""BaseUrl"": ""https://www.linkedin.com/jobs-guest/jobs/api/seeMoreJobPostings/search"",
      ""Location"": ""Argentina"",
      ""Keywords"": [],
      ""CookiesPath"": ""linkedin-cookies.json""
    },
    ""AntiBot"": {
      ""EnableUserAgentRotation"": true,
      ""MinDelayMs"": 1000,
      ""MaxDelayMs"": 3000
    },
    ""Candidate"": {
      ""Name"": ""Tu nombre"",
      ""Level"": ""Junior"",
      ""YearsExperience"": 2,
      ""CoreSkills"": [""Node.js"", ""JavaScript"", ""TypeScript"", ""Python""],
      ""PreferredModality"": ""Remoto"",
      ""PreferredRegions"": [""Colombia"", ""Latin America"", ""Europe""],
      ""CvDescription"": """"
    },
    ""JobSources"": {
      ""LinkedIn"": {
        ""Enabled"": true,
        ""Keywords"": [""Node.js"", ""JavaScript"", ""TypeScript"", ""Backend"", ""Full-Stack"", ""Python""],
        ""Location"": ""Medellin, Colombia""
      },
      ""CompuTrabajo"": {
        ""Enabled"": true,
        ""Keywords"": [""Node.js"", ""JavaScript"", ""TypeScript"", ""Backend"", ""Full-Stack"", ""Python""],
        ""Location"": ""Medellin, Colombia""
      }
    },
    ""Scoring"": {
      ""Threshold"": 75,
      ""MaxRetries"": 3
    },
    ""RateLimiting"": {
      ""DelayBetweenRequestsMs"": 2000,
      ""DelayBetweenSearchesMs"": 5000
    },
    ""Notion"": {
      ""ApiKey"": """",
      ""DatabaseId"": """"
    },
    ""Telegram"": {
      ""BotToken"": ""PLACEHOLDER_BOT_TOKEN"",
      ""ChatId"": """"
    }
  }
}";
    File.WriteAllText(path, defaultConfig);
    Console.WriteLine("  [Config] appsettings.json creado con valores por defecto");
}
