using CareerSentinel.Configuration;
using CareerSentinel.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Extensions.Http;

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
// 2. ServiceCollection + ServiceProvider
// ---------------------------------------------------------------------------
var services = new ServiceCollection();

// 3. Bind AppSettings via IOptions<AppSettings>
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
services.AddHttpClient("Ollama")
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
services.AddHttpClient("CompuTrabajo")
    .AddPolicyHandler(retryPolicy);

// ---------------------------------------------------------------------------
// 7. Register all services
// ---------------------------------------------------------------------------

// Job scrapers (IJobScraper - polymorphic resolution)
services.AddTransient<IJobScraper, LinkedInScraper>();
services.AddTransient<IJobScraper, CompuTrabajoScraper>();

// Legacy interface (to be removed in a future task)
services.AddSingleton<ILinkedInScraper, LinkedInScraper>();
services.AddSingleton<LocalLlmService>();
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
        // 10. Option 1: Run orchestrator
        case 1:
            ConsoleMenu.ShowMessage("Iniciando búsqueda completa...");
            try
            {
                var cts = new CancellationTokenSource();
                Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

                await orchestrator.RunAsync(cts.Token);

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

        // 11. Option 2: Show current configuration
        case 2:
            ConsoleMenu.ShowConfig(settings);
            break;

        // 11. Option 3: Modify keywords at runtime
        case 3:
            Console.Write("  Nueva keyword (separada por coma): ");
            var keywordsInput = Console.ReadLine()?.Trim();
            if (!string.IsNullOrEmpty(keywordsInput))
            {
                settings.LinkedIn.Keywords = keywordsInput
                    .Split(',')
                    .Select(k => k.Trim())
                    .Where(k => !string.IsNullOrEmpty(k))
                    .ToList();
                ConsoleMenu.ShowMessage($"Keywords actualizadas: {string.Join(", ", settings.LinkedIn.Keywords)}");
            }
            break;

        // 11. Option 4: Modify score threshold at runtime
        case 4:
            Console.Write("  Nuevo umbral (0-100): ");
            if (int.TryParse(Console.ReadLine()?.Trim(), out int newThreshold)
                && newThreshold >= 0 && newThreshold <= 100)
            {
                settings.Scoring.Threshold = newThreshold;
                ConsoleMenu.ShowMessage($"Umbral actualizado: {newThreshold}");
            }
            else
            {
                ConsoleMenu.ShowMessage("Valor inválido. Ingrese un número del 0 al 100.");
            }
            break;

        // Option 5: Show enabled sources
        case 5:
            ShowSourcesStatus(settings);
            break;

        // Option 6: Enable/disable sources
        case 6:
            ToggleSource(settings);
            break;

        // Option 7: Exit
        case 7:
            ConsoleMenu.ShowMessage("Hasta luego!");
            provider.Dispose();
            return;
    }
}

// ---------------------------------------------------------------------------
// Local helper functions for source management
// ---------------------------------------------------------------------------

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
