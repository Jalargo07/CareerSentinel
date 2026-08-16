using CareerSentinel.Configuration;
using CareerSentinel.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Extensions.Http;

// Build configuration
var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false)
    .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Production"}.json", optional: true)
    .AddUserSecrets<Program>(optional: true)
    .Build();

// Build services
var services = new ServiceCollection();

services.Configure<AppSettings>(configuration.GetSection("AppSettings"));
services.AddLogging(builder => builder.AddConsole());

// HttpClient for LinkedIn with Polly
services.AddHttpClient("LinkedIn")
    .AddPolicyHandler(HttpPolicyExtensions
        .HandleTransientHttpError()
        .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))));

// HttpClient for Ollama
services.AddHttpClient("Ollama");

// HttpClient for Notion
services.AddHttpClient("Notion")
    .AddPolicyHandler(HttpPolicyExtensions
        .HandleTransientHttpError()
        .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))));

// Services
services.AddSingleton<ILinkedInScraper, LinkedInScraper>();
services.AddSingleton<LocalLlmService>();
services.AddSingleton<NotionService>();
services.AddSingleton<TelegramAlertService>();
services.AddSingleton<IJobCacheService, JobCacheService>();
services.AddSingleton<JobOrchestrator>();

var provider = services.BuildServiceProvider();

// Main menu loop
var orchestrator = provider.GetRequiredService<JobOrchestrator>();

// Load settings for display
var settings = new AppSettings();
configuration.GetSection("AppSettings").Bind(settings);

Console.WriteLine();
Console.WriteLine("========================================");
Console.WriteLine("  CareerSentinel - Buscador de Empleo");
Console.WriteLine("========================================");
Console.WriteLine();

while (true)
{
    Console.WriteLine("[1] Ejecutar busqueda completa");
    Console.WriteLine("[2] Ver configuracion actual");
    Console.WriteLine("[3] Configurar keywords");
    Console.WriteLine("[4] Configurar umbral de score");
    Console.WriteLine("[5] Salir");
    Console.WriteLine();
    Console.Write("Opcion: ");

    var option = Console.ReadLine()?.Trim();

    switch (option)
    {
        case "1":
            Console.WriteLine("\nIniciando busqueda...\n");
            try
            {
                var cts = new CancellationTokenSource();
                Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

                await orchestrator.RunAsync(cts.Token);
                Console.WriteLine("\nBusqueda completada.\n");
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("\nBusqueda cancelada por el usuario.\n");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\nError: {ex.Message}\n");
            }
            break;

        case "2":
            Console.WriteLine($"\n--- Configuracion Actual ---");
            Console.WriteLine($"  Ollama URL: {settings.Ollama.BaseUrl}");
            Console.WriteLine($"  Modelo: {settings.Ollama.ModelName}");
            Console.WriteLine($"  Keywords: {string.Join(", ", settings.LinkedIn.Keywords)}");
            Console.WriteLine($"  Ubicacion: {settings.LinkedIn.Location}");
            Console.WriteLine($"  Umbral Score: {settings.Scoring.Threshold}");
            Console.WriteLine($"  Notion DB: {(string.IsNullOrEmpty(settings.Notion.DatabaseId) ? "(no configurado)" : "OK")}");
            Console.WriteLine($"  Telegram: {(string.IsNullOrEmpty(settings.Telegram.BotToken) ? "(no configurado)" : "OK")}");
            Console.WriteLine();
            break;

        case "3":
            Console.Write("\nNueva keyword (separada por coma): ");
            var input = Console.ReadLine()?.Trim();
            if (!string.IsNullOrEmpty(input))
            {
                settings.LinkedIn.Keywords = input.Split(',').Select(k => k.Trim()).ToList();
                Console.WriteLine($"Keywords actualizadas: {string.Join(", ", settings.LinkedIn.Keywords)}\n");
            }
            break;

        case "4":
            Console.Write("\nNuevo umbral (0-100): ");
            if (int.TryParse(Console.ReadLine()?.Trim(), out int newThreshold) && newThreshold >= 0 && newThreshold <= 100)
            {
                settings.Scoring.Threshold = newThreshold;
                Console.WriteLine($"Umbral actualizado: {newThreshold}\n");
            }
            else
            {
                Console.WriteLine("Valor invalido.\n");
            }
            break;

        case "5":
            Console.WriteLine("\nHasta luego!\n");
            return;

        default:
            Console.WriteLine("\nOpcion invalida.\n");
            break;
    }
}