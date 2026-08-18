using CareerSentinel.Configuration;

namespace CareerSentinel.Services;

public static class ConsoleMenu
{
    public static int ShowMainMenu()
    {
        Console.WriteLine();
        Console.WriteLine("══════════════════════════════════════════");
        Console.WriteLine("  CareerSentinel - Buscador de Empleo");
        Console.WriteLine("══════════════════════════════════════════");
        Console.WriteLine("  [1] Ejecutar búsqueda completa (todos)");
        Console.WriteLine("  [2] Ver configuración actual");
        Console.WriteLine("  [3] Configurar keywords");
        Console.WriteLine("  [4] Configurar umbral de score");
        Console.WriteLine("  [5] Ver fuentes habilitadas");
        Console.WriteLine("  [6] Habilitar/deshabilitar fuentes");
        Console.WriteLine("  [7] Ejecutar SOLO LinkedIn");
        Console.WriteLine("  [8] Ejecutar SOLO CompuTrabajo");
        Console.WriteLine("  [9] Salir");
        Console.WriteLine("══════════════════════════════════════════");
        Console.Write("  Opción: ");

        while (true)
        {
            var input = Console.ReadLine();

            if (int.TryParse(input, out var option) && option >= 1 && option <= 9)
            {
                Console.WriteLine();
                return option;
            }

            Console.WriteLine("  Opción inválida. Ingrese un número del 1 al 9.");
            Console.Write("  Opción: ");
        }
    }

    public static void ShowResultsSummary(int total, int matched, int saved)
    {
        Console.WriteLine();
        Console.WriteLine("══════════════════════════════════════════");
        Console.WriteLine($"  Resultados: {total} total | {matched} coincidencias | {saved} guardadas");
        Console.WriteLine("══════════════════════════════════════════");
        Console.WriteLine();
    }

    public static void ShowConfig(AppSettings settings)
    {
        Console.WriteLine();
        Console.WriteLine("══════════════════════════════════════════");
        Console.WriteLine("  Configuración Actual");
        Console.WriteLine("══════════════════════════════════════════");
        Console.WriteLine($"  Ollama BaseUrl:      {settings.Ollama.BaseUrl}");
        Console.WriteLine($"  Ollama Model:        {settings.Ollama.ModelName}");
        Console.WriteLine($"  LinkedIn Location:   {settings.LinkedIn.Location}");
        Console.WriteLine($"  LinkedIn Keywords:   {string.Join(", ", settings.LinkedIn.Keywords)}");
        Console.WriteLine($"  Scoring Threshold:   {settings.Scoring.Threshold}");
        Console.WriteLine($"  Max Retries:         {settings.Scoring.MaxRetries}");
        Console.WriteLine($"  Notion ApiKey:       {MaskSecret(settings.Notion.ApiKey)}");
        Console.WriteLine($"  Notion DatabaseId:   {MaskSecret(settings.Notion.DatabaseId)}");
        Console.WriteLine($"  Telegram BotToken:   {MaskSecret(settings.Telegram.BotToken)}");
        Console.WriteLine($"  Telegram ChatId:     {MaskSecret(settings.Telegram.ChatId)}");
        Console.WriteLine($"  Request Delay (ms):  {settings.RateLimiting.DelayBetweenRequestsMs}");
        Console.WriteLine($"  Search Delay (ms):   {settings.RateLimiting.DelayBetweenSearchesMs}");
        Console.WriteLine("══════════════════════════════════════════");
        Console.WriteLine();
    }

    public static void ShowMessage(string message)
    {
        Console.WriteLine();
        Console.WriteLine($"  {message}");
        Console.WriteLine();
    }

    public static string? ShowScraperMenu()
    {
        Console.WriteLine();
        Console.WriteLine("══════════════════════════════════════════");
        Console.WriteLine("  Seleccionar fuente de empleo");
        Console.WriteLine("══════════════════════════════════════════");
        Console.WriteLine("  [1] LinkedIn");
        Console.WriteLine("  [2] CompuTrabajo");
        Console.WriteLine("  [3] Todos (cancelar)");
        Console.WriteLine("══════════════════════════════════════════");
        Console.Write("  Opción: ");

        var input = Console.ReadLine()?.Trim();
        return input switch
        {
            "1" => "LinkedIn",
            "2" => "CompuTrabajo",
            "3" => null, // null = todos
            _ => null
        };
    }

    private static string MaskSecret(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "(no configurado)";

        if (value.Length <= 8)
            return "****";

        return value[..4] + "****" + value[^4..];
    }
}
