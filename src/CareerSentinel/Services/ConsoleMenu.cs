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
        Console.WriteLine("  [3] Ver fuentes habilitadas");
        Console.WriteLine("  [4] Habilitar/deshabilitar fuentes");
        Console.WriteLine("  [5] Ejecutar SOLO LinkedIn");
        Console.WriteLine("  [6] Ejecutar SOLO CompuTrabajo");
        Console.WriteLine("  [7] Configurar perfil del candidato");
        Console.WriteLine("  [8] Configurar LLM (modelo, API key)");
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

    public static void ShowCandidateConfig(AppSettings settings)
    {
        while (true)
        {
            Console.WriteLine();
            Console.WriteLine("══════════════════════════════════════════");
            Console.WriteLine("  Configuración del Candidato");
            Console.WriteLine("══════════════════════════════════════════");
            Console.WriteLine($"  Nombre:      {settings.Candidate.Name}");
            Console.WriteLine($"  Nivel:       {settings.Candidate.Level}");
            Console.WriteLine($"  Experiencia: {settings.Candidate.YearsExperience} años");
            Console.WriteLine($"  Skills:      {string.Join(", ", settings.Candidate.CoreSkills)}");
            Console.WriteLine($"  Modalidad:   {settings.Candidate.PreferredModality}");
            Console.WriteLine($"  Regiones:    {string.Join(", ", settings.Candidate.PreferredRegions)}");
            Console.WriteLine($"  Keywords:    {string.Join(", ", settings.LinkedIn.Keywords)}");
            Console.WriteLine("══════════════════════════════════════════");
            Console.WriteLine("  [1] Editar nombre");
            Console.WriteLine("  [2] Editar nivel (Junior/Mid/Senior)");
            Console.WriteLine("  [3] Editar años de experiencia");
            Console.WriteLine("  [4] Editar skills principales");
            Console.WriteLine("  [5] Editar modalidad preferida");
            Console.WriteLine("  [6] Editar regiones preferidas");
            Console.WriteLine("  [7] Editar keywords de búsqueda");
            Console.WriteLine("  [8] Editar descripción CV");
            Console.WriteLine("  [0] Volver al menú principal");
            Console.WriteLine("══════════════════════════════════════════");
            Console.Write("  Opción: ");

            var input = Console.ReadLine()?.Trim();
            Console.WriteLine();

            switch (input)
            {
                case "1":
                    Console.Write($"  Nombre actual ({settings.Candidate.Name}): ");
                    var name = Console.ReadLine()?.Trim();
                    if (!string.IsNullOrEmpty(name))
                        settings.Candidate.Name = name;
                    break;

                case "2":
                    Console.WriteLine("  Niveles disponibles:");
                    Console.WriteLine("    [1] Junior");
                    Console.WriteLine("    [2] Mid");
                    Console.WriteLine("    [3] Senior");
                    Console.Write("  Selecciona: ");
                    var levelInput = Console.ReadLine()?.Trim();
                    var level = levelInput switch
                    {
                        "1" => "Junior",
                        "2" => "Mid",
                        "3" => "Senior",
                        _ => settings.Candidate.Level
                    };
                    settings.Candidate.Level = level;
                    Console.WriteLine($"  → Nivel establecido: {level}");
                    break;

                case "3":
                    Console.Write($"  Años de experiencia actual ({settings.Candidate.YearsExperience}): ");
                    var yearsInput = Console.ReadLine()?.Trim();
                    if (int.TryParse(yearsInput, out var years) && years >= 0 && years <= 50)
                        settings.Candidate.YearsExperience = years;
                    else if (!string.IsNullOrEmpty(yearsInput))
                        Console.WriteLine("  ❌ Valor inválido. Debe ser un número entre 0 y 50.");
                    break;

                case "4":
                    Console.WriteLine($"  Skills actuales: {string.Join(", ", settings.Candidate.CoreSkills)}");
                    Console.WriteLine("  Ingresa las skills separadas por coma:");
                    Console.Write("  → ");
                    var skillsInput = Console.ReadLine()?.Trim();
                    if (!string.IsNullOrEmpty(skillsInput))
                    {
                        var skills = skillsInput.Split(',', StringSplitOptions.RemoveEmptyEntries)
                            .Select(s => s.Trim()).ToList();
                        settings.Candidate.CoreSkills = skills;
                        Console.WriteLine($"  → Skills actualizadas: {string.Join(", ", skills)}");
                    }
                    break;

                case "5":
                    Console.WriteLine("  Modalidades disponibles:");
                    Console.WriteLine("    [1] Remoto");
                    Console.WriteLine("    [2] Híbrido");
                    Console.WriteLine("    [3] Presencial");
                    Console.WriteLine("    [4] Cualquiera");
                    Console.Write("  Selecciona: ");
                    var modalityInput = Console.ReadLine()?.Trim();
                    var modality = modalityInput switch
                    {
                        "1" => "Remoto",
                        "2" => "Híbrido",
                        "3" => "Presencial",
                        "4" => "Cualquiera",
                        _ => settings.Candidate.PreferredModality
                    };
                    settings.Candidate.PreferredModality = modality;
                    Console.WriteLine($"  → Modalidad establecida: {modality}");
                    break;

                case "6":
                    Console.WriteLine($"  Regiones actuales: {string.Join(", ", settings.Candidate.PreferredRegions)}");
                    Console.WriteLine("  Ingresa las regiones separadas por coma:");
                    Console.Write("  → ");
                    var regionsInput = Console.ReadLine()?.Trim();
                    if (!string.IsNullOrEmpty(regionsInput))
                    {
                        var regions = regionsInput.Split(',', StringSplitOptions.RemoveEmptyEntries)
                            .Select(r => r.Trim()).ToList();
                        settings.Candidate.PreferredRegions = regions;
                        Console.WriteLine($"  → Regiones actualizadas: {string.Join(", ", regions)}");
                    }
                    break;

                case "7":
                    Console.WriteLine($"  Keywords actuales: {string.Join(", ", settings.LinkedIn.Keywords)}");
                    Console.WriteLine("  Ingresa las keywords separadas por coma:");
                    Console.Write("  → ");
                    var keywordsInput = Console.ReadLine()?.Trim();
                    if (!string.IsNullOrEmpty(keywordsInput))
                    {
                        var keywords = keywordsInput.Split(',', StringSplitOptions.RemoveEmptyEntries)
                            .Select(k => k.Trim()).ToList();
                        settings.LinkedIn.Keywords = keywords;
                        if (settings.JobSources.ContainsKey("CompuTrabajo"))
                            settings.JobSources["CompuTrabajo"].Keywords = keywords;
                        Console.WriteLine($"  → Keywords actualizadas: {string.Join(", ", keywords)}");
                    }
                    break;

                case "8":
                    Console.WriteLine("  Descripción actual del CV:");
                    Console.WriteLine($"  {(string.IsNullOrEmpty(settings.Candidate.CvDescription) ? "(vacía)" : settings.Candidate.CvDescription)}");
                    Console.WriteLine("  Ingresa la nueva descripción (presiona Enter dos veces para terminar):");
                    var cvLines = new List<string>();
                    while (true)
                    {
                        var line = Console.ReadLine();
                        if (line == null) break;
                        if (string.IsNullOrEmpty(line) && cvLines.Count > 0 && string.IsNullOrEmpty(cvLines.Last()))
                        {
                            cvLines.RemoveAt(cvLines.Count - 1);
                            break;
                        }
                        cvLines.Add(line);
                    }
                    var cvDescription = string.Join("\n", cvLines).Trim();
                    if (!string.IsNullOrEmpty(cvDescription))
                    {
                        settings.Candidate.CvDescription = cvDescription;
                        Console.WriteLine($"  → CV actualizado ({cvDescription.Length} caracteres)");
                    }
                    break;

                case "0":
                    return;

                default:
                    Console.WriteLine("  Opción inválida.");
                    break;
            }
        }
    }

    public static void ShowLlmConfig(AppSettings settings)
    {
        while (true)
        {
            Console.WriteLine();
            Console.WriteLine("══════════════════════════════════════════");
            Console.WriteLine("  Configuración del Modelo de Lenguaje");
            Console.WriteLine("══════════════════════════════════════════");
            Console.WriteLine($"  Processing Mode: {settings.ProcessingMode}");
            Console.WriteLine($"  API Base URL:     {settings.OpenCodeGo.BaseUrl}");
            Console.WriteLine($"  API Model:        {settings.OpenCodeGo.ModelName}");
            Console.WriteLine($"  API Key:          {(string.IsNullOrEmpty(settings.OpenCodeGo.ApiKey) ? "(no configurada)" : "****" + settings.OpenCodeGo.ApiKey[^4..])}");
            Console.WriteLine($"  Max Tokens Batch: {settings.OpenCodeGo.MaxTokensBatch}");
            Console.WriteLine($"  Ollama URL:       {settings.Ollama.BaseUrl}");
            Console.WriteLine($"  Ollama Model:     {settings.Ollama.ModelName}");
            Console.WriteLine("══════════════════════════════════════════");
            Console.WriteLine("  [1] Cambiar ProcessingMode (Local/API/Hybrid)");
            Console.WriteLine("  [2] Cambiar API Key de Gemini");
            Console.WriteLine("  [3] Cambiar modelo de API");
            Console.WriteLine("  [4] Cambiar Base URL de API");
            Console.WriteLine("  [5] Cambiar modelo de Ollama");
            Console.WriteLine("  [6] Cambiar URL de Ollama");
            Console.WriteLine("  [0] Volver");
            Console.WriteLine("══════════════════════════════════════════");
            Console.Write("  Opción: ");

            var input = Console.ReadLine()?.Trim();
            Console.WriteLine();

            switch (input)
            {
                case "1":
                    Console.WriteLine("  Modos disponibles:");
                    Console.WriteLine("    [1] Local   (Ollama, gratis)");
                    Console.WriteLine("    [2] API     (Gemini/OpenCode, paga tokens)");
                    Console.WriteLine("    [3] Hybrid  (Ollama Paso1 + API Paso2)");
                    Console.Write("  Selecciona: ");
                    var modeInput = Console.ReadLine()?.Trim();
                    var mode = modeInput switch
                    {
                        "1" => ProcessingMode.Local,
                        "2" => ProcessingMode.API,
                        "3" => ProcessingMode.Hybrid,
                        _ => settings.ProcessingMode
                    };
                    settings.ProcessingMode = mode;
                    Console.WriteLine($"  → Modo establecido: {mode}");
                    break;

                case "2":
                    Console.Write("  API Key de Gemini (deja vacío para usar User Secrets): ");
                    var apiKey = Console.ReadLine()?.Trim();
                    if (apiKey is not null)
                    {
                        settings.OpenCodeGo.ApiKey = apiKey;
                        Console.WriteLine(string.IsNullOrEmpty(apiKey)
                            ? "  → API Key limpiada (usará User Secrets)"
                            : "  → API Key actualizada");
                    }
                    break;

                case "3":
                    Console.WriteLine("  Modelos disponibles:");
                    Console.WriteLine("    [1] gemini-3.5-flash-lite (gratis, rápido)");
                    Console.WriteLine("    [2] gemini-2.5-flash");
                    Console.WriteLine("    [3] gemini-2.0-flash");
                    Console.Write("  Selecciona o escribe uno personalizado: ");
                    var modelInput = Console.ReadLine()?.Trim();
                    var model = modelInput switch
                    {
                        "1" => "gemini-3.5-flash-lite",
                        "2" => "gemini-2.5-flash",
                        "3" => "gemini-2.0-flash",
                        _ => modelInput
                    };
                    if (!string.IsNullOrEmpty(model))
                    {
                        settings.OpenCodeGo.ModelName = model;
                        Console.WriteLine($"  → Modelo establecido: {model}");
                    }
                    break;

                case "4":
                    Console.Write($"  Base URL actual ({settings.OpenCodeGo.BaseUrl}): ");
                    var url = Console.ReadLine()?.Trim();
                    if (!string.IsNullOrEmpty(url))
                    {
                        settings.OpenCodeGo.BaseUrl = url;
                        Console.WriteLine($"  → URL establecida: {url}");
                    }
                    break;

                case "5":
                    Console.Write($"  Modelo Ollama actual ({settings.Ollama.ModelName}): ");
                    var ollamaModel = Console.ReadLine()?.Trim();
                    if (!string.IsNullOrEmpty(ollamaModel))
                    {
                        settings.Ollama.ModelName = ollamaModel;
                        Console.WriteLine($"  → Modelo Ollama: {ollamaModel}");
                    }
                    break;

                case "6":
                    Console.Write($"  URL Ollama actual ({settings.Ollama.BaseUrl}): ");
                    var ollamaUrl = Console.ReadLine()?.Trim();
                    if (!string.IsNullOrEmpty(ollamaUrl))
                    {
                        settings.Ollama.BaseUrl = ollamaUrl;
                        Console.WriteLine($"  → URL Ollama: {ollamaUrl}");
                    }
                    break;

                case "0":
                    return;
            }
        }
    }

    public static void SaveConfiguration(AppSettings settings)
    {
        try
        {
            var json = System.Text.Json.JsonSerializer.Serialize(new { AppSettings = settings }, new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            });

            var path = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
            File.WriteAllText(path, json);
            Console.WriteLine("  ✅ Configuración guardada en appsettings.json");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  ❌ Error al guardar: {ex.Message}");
        }
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
