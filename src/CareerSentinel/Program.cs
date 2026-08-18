using CareerSentinel.Configuration;
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
    .SetBasePath(Directory.GetCurrentDirectory())
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
var retryPolicy = HttpPolicyExtensions
    .HandleTransientHttpError()
    .WaitAndRetryAsync(
        retryCount: 3,
        sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
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

// Job scrapers (IJobScraper - polymorphic resolution)
services.AddTransient<IJobScraper, LinkedInScraper>();
services.AddTransient<IJobScraper, CompuTrabajoScraper>();

// Legacy interface (to be removed in a future task)
services.AddSingleton<ILinkedInScraper, LinkedInScraper>();

// Register all LLM services
services.AddSingleton<LocalLlmService>();
services.AddSingleton<OpenCodeGoService>();
services.AddSingleton<HybridLlmService>();

// Register ILlmService according to processing mode
switch (processingMode.ToUpperInvariant())
{
    case "API":
        services.AddSingleton<ILlmService>(sp => sp.GetRequiredService<OpenCodeGoService>());
        break;
    case "HYBRID":
        // Hybrid uses Ollama for Paso1 and API for Paso2
        services.AddSingleton<ILlmService>(sp => sp.GetRequiredService<HybridLlmService>());
        break;
    default: // "LOCAL"
        services.AddSingleton<ILlmService>(sp => sp.GetRequiredService<LocalLlmService>());
        break;
}

services.AddSingleton<NotionService>();
services.AddSingleton<TelegramAlertService>();
services.AddSingleton<IJobCacheService, JobCacheService>();
services.AddSingleton<JobOrchestrator>();

// 8. Build provider and resolve root services
var provider = services.BuildServiceProvider();
var orchestrator = provider.GetRequiredService<JobOrchestrator>();
var settings = provider.GetRequiredService<IOptions<AppSettings>>().Value;

// ---------------------------------------------------------------------------
// 9. Main menu loop
// ---------------------------------------------------------------------------
ConsoleMenu.ShowMessage("CareerSentinel v1.0 - Buscador Inteligente de Empleo");

while (true)
{
    var option = ConsoleMenu.ShowMainMenu();

    switch (option)
    {
        // Option 1: Run full search (all sources)
        case 1:
            ConsoleMenu.ShowMessage("Iniciando búsqueda completa...");
            try
            {
                var cts = new CancellationTokenSource();
                Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

                await orchestrator.RunAsync(null, cts.Token);

                ConsoleMenu.ShowMessage("Búsqueda completada exitosamente.");
            }
            catch (OperationCanceledException)
            {
                ConsoleMenu.ShowMessage("Búsqueda cancelada por el usuario.");
            }
            catch (Exception ex)
            {
                ConsoleMenu.ShowMessage($"Error durante la búsqueda: {ex.Message}");
            }
            break;

        // Option 2: Show current configuration
        case 2:
            ConsoleMenu.ShowConfig(settings);
            break;

        // Option 3: Show enabled sources
        case 3:
            ShowSourcesStatus(settings);
            break;

        // Option 4: Enable/disable sources
        case 4:
            ToggleSource(settings);
            break;

        // Option 5: Run only LinkedIn
        case 5:
            ConsoleMenu.ShowMessage("Iniciando búsqueda en SOLO LinkedIn...");
            try
            {
                var cts5 = new CancellationTokenSource();
                Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts5.Cancel(); };

                var linkedinOnly = new List<string> { "LinkedIn" };
                await orchestrator.RunAsync(linkedinOnly, cts5.Token);

                ConsoleMenu.ShowMessage("Búsqueda en LinkedIn completada exitosamente.");
            }
            catch (OperationCanceledException)
            {
                ConsoleMenu.ShowMessage("Búsqueda cancelada por el usuario.");
            }
            catch (Exception ex)
            {
                ConsoleMenu.ShowMessage($"Error durante la búsqueda: {ex.Message}");
            }
            break;

        // Option 6: Run only CompuTrabajo
        case 6:
            ConsoleMenu.ShowMessage("Iniciando búsqueda en SOLO CompuTrabajo...");
            try
            {
                var cts6 = new CancellationTokenSource();
                Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts6.Cancel(); };

                var ctOnly = new List<string> { "CompuTrabajo" };
                await orchestrator.RunAsync(ctOnly, cts6.Token);

                ConsoleMenu.ShowMessage("Búsqueda en CompuTrabajo completada exitosamente.");
            }
            catch (OperationCanceledException)
            {
                ConsoleMenu.ShowMessage("Búsqueda cancelada por el usuario.");
            }
            catch (Exception ex)
            {
                ConsoleMenu.ShowMessage($"Error durante la búsqueda: {ex.Message}");
            }
            break;

        // Option 7: Configure candidate profile
        case 7:
            ConsoleMenu.ShowCandidateConfig(settings);
            ConsoleMenu.SaveConfiguration(settings);
            break;

        // Option 8: Configure LLM
        case 8:
            ConsoleMenu.ShowLlmConfig(settings);
            ConsoleMenu.SaveConfiguration(settings);
            break;

        // Option 9: Exit
        case 9:
            ConsoleMenu.ShowMessage("Hasta luego!");
            provider.Dispose();
            return;
    }
}

// ---------------------------------------------------------------------------
// Local helper functions
// ---------------------------------------------------------------------------

static void EnsureAppSettingsExists()
{
    var path = Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json");
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
      ""Keywords"": []
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

static void ShowSourcesStatus(AppSettings settings)
{
    Console.WriteLine();
    Console.WriteLine("══════════════════════════════════════════");
    Console.WriteLine("  Fuentes de Empleo");
    Console.WriteLine("══════════════════════════════════════════");

    if (settings.JobSources.Count == 0)
    {
        Console.WriteLine("  (No hay fuentes configuradas)");
    }
    else
    {
        foreach (var source in settings.JobSources)
        {
            var status = source.Value.Enabled ? "[✅]" : "[❌]";
            var label = source.Value.Enabled ? "Habilitado" : "Deshabilitado";
            Console.WriteLine($"  {status} {source.Key} - {label}");
        }
    }

    Console.WriteLine("══════════════════════════════════════════");
    Console.WriteLine();
}

static void ToggleSource(AppSettings settings)
{
    if (settings.JobSources.Count == 0)
    {
        ConsoleMenu.ShowMessage("No hay fuentes configuradas.");
        return;
    }

    var sources = settings.JobSources.ToList();

    Console.WriteLine("  Fuentes disponibles:");
    for (int i = 0; i < sources.Count; i++)
    {
        var currentStatus = sources[i].Value.Enabled ? "Habilitado" : "Deshabilitado";
        Console.WriteLine($"  {i + 1}. {sources[i].Key} (actual: {currentStatus})");
    }

    Console.Write($"  Selecciona una fuente (1-{sources.Count}): ");
    if (!int.TryParse(Console.ReadLine()?.Trim(), out int index)
        || index < 1 || index > sources.Count)
    {
        ConsoleMenu.ShowMessage("Selección inválida.");
        return;
    }

    Console.Write("  Nuevo estado (true/false): ");
    var input = Console.ReadLine()?.Trim().ToLowerInvariant();
    if (input != "true" && input != "false")
    {
        ConsoleMenu.ShowMessage("Valor inválido. Ingrese 'true' o 'false'.");
        return;
    }

    var enabled = input == "true";
    var sourceName = sources[index - 1].Key;
    settings.JobSources[sourceName].Enabled = enabled;

    var newState = enabled ? "Habilitado" : "Deshabilitado";
    ConsoleMenu.ShowMessage($"{sourceName} ahora está {newState}.");
}
