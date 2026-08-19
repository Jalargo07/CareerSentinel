using CareerSentinel.Configuration;

namespace CareerSentinel.Services;

public static class ConsoleMenu
{
    public static int ShowMainMenu()
    {
        Console.WriteLine();
        Console.WriteLine("════════════════════════════════════════════════════════════");
        Console.WriteLine("  CareerSentinel - Buscador de Empleo con IA");
        Console.WriteLine("════════════════════════════════════════════════════════════");
        Console.WriteLine();
        Console.WriteLine("  ┌─ PRIMERA VEZ ─────────────────────────────────────────┐");
        Console.WriteLine("  │  [1] Configurar Telegram                             │");
        Console.WriteLine("  │      → Solo necesitas tu Chat ID                     │");
        Console.WriteLine("  │      → Abre: https://t.me/CareerSentinel_bot         │");
        Console.WriteLine("  │        y envía /start                                │");
        Console.WriteLine("  │                                                       │");
        Console.WriteLine("  │  [2] Configurar mi perfil                            │");
        Console.WriteLine("  │      → Tu nombre, nivel, skills, experiencia         │");
        Console.WriteLine("  │      → Modalidad y regiones preferidas               │");
        Console.WriteLine("  │                                                       │");
        Console.WriteLine("  │  [3] Configurar IA (inteligencia artificial)         │");
        Console.WriteLine("  │      → Selecciona proveedor (Gemini gratis, etc.)    │");
        Console.WriteLine("  │      → Ingresa tu API Key                            │");
        Console.WriteLine("  └───────────────────────────────────────────────────────┘");
        Console.WriteLine();
        Console.WriteLine("  ┌─ BUSCAR EMPLEO ───────────────────────────────────────┐");
        Console.WriteLine("  │  [4] Buscar en TODOS los portales                    │");
        Console.WriteLine("  │  [5] Buscar solo en LinkedIn                         │");
        Console.WriteLine("  │  [6] Buscar solo en CompuTrabajo                     │");
        Console.WriteLine("  └───────────────────────────────────────────────────────┘");
        Console.WriteLine();
        Console.WriteLine("  ┌─ UTILIDADES ──────────────────────────────────────────┐");
        Console.WriteLine("  │  [7] Ver configuración completa (solo lectura)       │");
        Console.WriteLine("  │  [8] Editar mi perfil de candidato                   │");
        Console.WriteLine("  │  [9] Editar IA / proveedor                           │");
        Console.WriteLine("  └───────────────────────────────────────────────────────┘");
        Console.WriteLine();
        Console.WriteLine("  ┌─ FUENTES DE EMPLEO ──────────────────────────────────┐");
        Console.WriteLine("  │  [0] Gestionar fuentes de empleo                     │");
        Console.WriteLine("  └───────────────────────────────────────────────────────┘");
        Console.WriteLine("════════════════════════════════════════════════════════════");
        Console.Write("  Opción: ");

        while (true)
        {
            var input = Console.ReadLine();

            if (int.TryParse(input, out var option) && option >= 0 && option <= 9)
            {
                Console.WriteLine();
                return option;
            }

            Console.WriteLine("  ❌ Opción inválida. Ingresa un número del 0 al 9.");
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

    /// <summary>
    /// Muestra el menú contextual post-búsqueda y retorna la opción seleccionada.
    /// </summary>
    /// <returns>
    /// 1 = Ver ofertas guardadas, 2 = Buscar de nuevo, 3 = Editar perfil,
    /// 4 = Ver configuración, 0 = Volver al menú principal
    /// </returns>
    public static int ShowResultsMenu(int total, int matched, int saved)
    {
        Console.WriteLine();
        Console.WriteLine("════════════════════════════════════════════════════════════");
        Console.WriteLine("  Resultados de la Búsqueda");
        Console.WriteLine("════════════════════════════════════════════════════════════");
        Console.WriteLine($"  📊 Total procesadas: {total}");
        Console.WriteLine($"  ✅ Coincidencias:    {matched}");
        Console.WriteLine($"  💾 Guardadas:        {saved}");
        Console.WriteLine("────────────────────────────────────────────────────────────");
        Console.WriteLine("  ¿Qué deseas hacer ahora?");
        Console.WriteLine();
        Console.WriteLine("  [1] Ver últimas ofertas guardadas");
        Console.WriteLine("  [2] Buscar de nuevo");
        Console.WriteLine("  [3] Editar mi perfil");
        Console.WriteLine("  [4] Ver configuración completa");
        Console.WriteLine("  [0] Volver al menú principal");
        Console.WriteLine("════════════════════════════════════════════════════════════");
        Console.Write("  Opción: ");

        while (true)
        {
            var input = Console.ReadLine();

            if (int.TryParse(input, out var option) && option >= 0 && option <= 4)
            {
                Console.WriteLine();
                return option;
            }

            Console.WriteLine("  ❌ Opción inválida. Ingresa un número del 0 al 4.");
            Console.Write("  Opción: ");
        }
    }

    public static void ShowConfig(AppSettings settings)
    {
        Console.WriteLine();
        Console.WriteLine("════════════════════════════════════════════════════════════");
        Console.WriteLine("  Configuración Actual");
        Console.WriteLine("════════════════════════════════════════════════════════════");

        // ── Modo de procesamiento ─────────────────────────────────────────
        Console.WriteLine($"  Modo:                   {settings.ProcessingMode}");

        // ── Ollama (local) ────────────────────────────────────────────────
        Console.WriteLine("────────────────────────────────────────────────────────────");
        Console.WriteLine("  Ollama (local):");
        Console.WriteLine($"    BaseUrl:              {settings.Ollama.BaseUrl}");
        Console.WriteLine($"    Model:                {settings.Ollama.ModelName}");

        // ── OpenCodeGo (API) ──────────────────────────────────────────────
        Console.WriteLine("────────────────────────────────────────────────────────────");
        Console.WriteLine("  OpenCodeGo (API):");
        Console.WriteLine($"    BaseUrl:              {settings.OpenCodeGo.BaseUrl}");
        Console.WriteLine($"    Model:                {settings.OpenCodeGo.ModelName}");
        Console.WriteLine($"    ApiKey:               {MaskSecret(settings.OpenCodeGo.ApiKey)}");
        Console.WriteLine($"    MaxTokens:            {settings.OpenCodeGo.MaxTokensBatch}");
        Console.WriteLine($"    Timeout:              {settings.OpenCodeGo.TimeoutSeconds}s");

        // ── Scoring ───────────────────────────────────────────────────────
        Console.WriteLine("────────────────────────────────────────────────────────────");
        Console.WriteLine("  LLM Config:");
        Console.WriteLine($"    Threshold:            {settings.Scoring.Threshold}");
        Console.WriteLine($"    MaxRetries:           {settings.Scoring.MaxRetries}");

        // ── Fuentes de empleo ─────────────────────────────────────────────
        Console.WriteLine("────────────────────────────────────────────────────────────");
        Console.WriteLine("  Fuentes de Empleo:");
        foreach (var source in settings.JobSources)
        {
            var status = source.Value.Enabled ? "[✅]" : "[❌]";
            Console.WriteLine($"    {status} {source.Key}:");
            Console.WriteLine($"       Location:          {source.Value.Location}");
            Console.WriteLine($"       Keywords:          {(source.Value.Keywords.Count > 0 ? string.Join(", ", source.Value.Keywords) : "(ningunas)")}");
        }

        // ── Perfil del candidato ──────────────────────────────────────────
        Console.WriteLine("────────────────────────────────────────────────────────────");
        Console.WriteLine("  Candidato:");
        Console.WriteLine($"    Nombre:               {settings.Candidate.Name}");
        Console.WriteLine($"    Nivel:                {settings.Candidate.Level}");
        Console.WriteLine($"    Años Experiencia:     {settings.Candidate.YearsExperience}");
        Console.WriteLine($"    Modalidad Preferida:  {settings.Candidate.PreferredModality}");
        Console.WriteLine($"    Regiones:             {(settings.Candidate.PreferredRegions.Count > 0 ? string.Join(", ", settings.Candidate.PreferredRegions) : "(ninguna)")}");
        Console.WriteLine($"    Skills:               {(settings.Candidate.CoreSkills.Count > 0 ? string.Join(", ", settings.Candidate.CoreSkills) : "(ninguna)")}");

        // ── Telegram ──────────────────────────────────────────────────────
        Console.WriteLine("────────────────────────────────────────────────────────────");
        Console.WriteLine("  Telegram:");
        Console.WriteLine($"    BotToken:             {MaskSecret(settings.Telegram.BotToken)}");
        Console.WriteLine($"    ChatId:               {settings.Telegram.ChatId}");

        // ── Notion ────────────────────────────────────────────────────────
        Console.WriteLine("────────────────────────────────────────────────────────────");
        Console.WriteLine("  Notion:");
        Console.WriteLine($"    ApiKey:               {MaskSecret(settings.Notion.ApiKey)}");
        Console.WriteLine($"    DatabaseId:           {MaskSecret(settings.Notion.DatabaseId)}");

        // ── Rate Limiting ─────────────────────────────────────────────────
        Console.WriteLine("────────────────────────────────────────────────────────────");
        Console.WriteLine("  Rate Limiting:");
        Console.WriteLine($"    Delay Between Requests: {settings.RateLimiting.DelayBetweenRequestsMs}ms");
        Console.WriteLine($"    Delay Between Searches: {settings.RateLimiting.DelayBetweenSearchesMs}ms");

        Console.WriteLine("════════════════════════════════════════════════════════════");
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
            Console.WriteLine("════════════════════════════════════════════════════════════");
            Console.WriteLine("  Mi Perfil de Candidato");
            Console.WriteLine("════════════════════════════════════════════════════════════");
            Console.WriteLine("  Esta información se usa para evaluar si una oferta");
            Console.WriteLine("  es compatible contigo. Mientras más precisa, mejor.");
            Console.WriteLine("────────────────────────────────────────────────────────────");
            Console.WriteLine("  Selecciona un grupo o escribe un número directamente:");
            Console.WriteLine();
            Console.WriteLine("  ┌─ [A] DATOS PERSONALES ──────────────────────────────┐");
            Console.WriteLine("  │    [1] Nombre          → " + settings.Candidate.Name);
            Console.WriteLine("  │    [2] Nivel           → " + settings.Candidate.Level);
            Console.WriteLine("  │    [3] Años experiencia→ " + settings.Candidate.YearsExperience);
            Console.WriteLine("  └──────────────────────────────────────────────────────┘");
            Console.WriteLine("  ┌─ [B] PREFERENCIAS DE EMPLEO ────────────────────────┐");
            Console.WriteLine("  │    [4] Modalidad       → " + settings.Candidate.PreferredModality);
            Console.WriteLine("  │    [5] Regiones        → " + (settings.Candidate.PreferredRegions.Count > 0 ? string.Join(", ", settings.Candidate.PreferredRegions) : "(no configuradas)"));
            Console.WriteLine("  │    [6] Skills          → " + (settings.Candidate.CoreSkills.Count > 0 ? string.Join(", ", settings.Candidate.CoreSkills) : "(no configuradas)"));
            Console.WriteLine("  └──────────────────────────────────────────────────────┘");
            Console.WriteLine("  ┌─ [C] BÚSQUEDA Y CV ────────────────────────────────┐");
            Console.WriteLine("  │    [7] Keywords        → " + (settings.LinkedIn.Keywords.Count > 0 ? string.Join(", ", settings.LinkedIn.Keywords) : "(no configuradas)"));
            Console.WriteLine("  │    [8] Descripción CV  → " + (string.IsNullOrEmpty(settings.Candidate.CvDescription) ? "(vacía)" : $"{settings.Candidate.CvDescription.Length} caracteres"));
            Console.WriteLine("  │    [9] Chat ID Telegram→ " + (string.IsNullOrEmpty(settings.Telegram.ChatId) ? "(no configurado)" : settings.Telegram.ChatId));
            Console.WriteLine("  └──────────────────────────────────────────────────────┘");
            Console.WriteLine("────────────────────────────────────────────────────────────");
            Console.WriteLine("  [0] Volver al menú principal");
            Console.WriteLine("════════════════════════════════════════════════════════════");
            Console.Write("  Opción: ");

            var input = Console.ReadLine()?.Trim()?.ToUpperInvariant();
            Console.WriteLine();

            switch (input)
            {
                // ── Navegación por grupo ──────────────────────────────
                case "A":
                    ShowCandidateGroupA(settings);
                    break;
                case "B":
                    ShowCandidateGroupB(settings);
                    break;
                case "C":
                    ShowCandidateGroupC(settings);
                    break;

                // ── Navegación directa por número ─────────────────────
                case "1":
                    EditCandidateName(settings);
                    break;
                case "2":
                    EditCandidateLevel(settings);
                    break;
                case "3":
                    EditCandidateYears(settings);
                    break;
                case "4":
                    EditCandidateModality(settings);
                    break;
                case "5":
                    EditCandidateRegions(settings);
                    break;
                case "6":
                    EditCandidateSkills(settings);
                    break;
                case "7":
                    EditCandidateKeywords(settings);
                    break;
                case "8":
                    EditCandidateCvDescription(settings);
                    break;
                case "9":
                    EditCandidateTelegramChatId(settings);
                    break;

                case "0":
                    return;

                default:
                    Console.WriteLine("  ❌ Opción inválida. Usa A/B/C o un número del 1 al 9.");
                    break;
            }
        }
    }

    // ── Submenús de grupo ────────────────────────────────────────────

    private static void ShowCandidateGroupA(AppSettings settings)
    {
        while (true)
        {
            Console.WriteLine();
            Console.WriteLine("════════════════════════════════════════════════════════════");
            Console.WriteLine("  [A] DATOS PERSONALES");
            Console.WriteLine("════════════════════════════════════════════════════════════");
            Console.WriteLine($"  [1] Tu nombre          → {settings.Candidate.Name}");
            Console.WriteLine($"  [2] Tu nivel           → {settings.Candidate.Level}");
            Console.WriteLine($"  [3] Años experiencia   → {settings.Candidate.YearsExperience}");
            Console.WriteLine("────────────────────────────────────────────────────────────");
            Console.WriteLine("  [0] Volver al menú de perfil");
            Console.WriteLine("════════════════════════════════════════════════════════════");
            Console.Write("  Opción: ");

            var input = Console.ReadLine()?.Trim();
            Console.WriteLine();

            switch (input)
            {
                case "1":
                    EditCandidateName(settings);
                    break;
                case "2":
                    EditCandidateLevel(settings);
                    break;
                case "3":
                    EditCandidateYears(settings);
                    break;
                case "0":
                    return;
                default:
                    Console.WriteLine("  ❌ Opción inválida.");
                    break;
            }
        }
    }

    private static void ShowCandidateGroupB(AppSettings settings)
    {
        while (true)
        {
            Console.WriteLine();
            Console.WriteLine("════════════════════════════════════════════════════════════");
            Console.WriteLine("  [B] PREFERENCIAS DE EMPLEO");
            Console.WriteLine("════════════════════════════════════════════════════════════");
            Console.WriteLine($"  [1] Modalidad ideal     → {settings.Candidate.PreferredModality}");
            Console.WriteLine($"  [2] Regiones preferidas → {(settings.Candidate.PreferredRegions.Count > 0 ? string.Join(", ", settings.Candidate.PreferredRegions) : "(no configuradas)")}");
            Console.WriteLine($"  [3] Skills              → {(settings.Candidate.CoreSkills.Count > 0 ? string.Join(", ", settings.Candidate.CoreSkills) : "(no configuradas)")}");
            Console.WriteLine("────────────────────────────────────────────────────────────");
            Console.WriteLine("  [0] Volver al menú de perfil");
            Console.WriteLine("════════════════════════════════════════════════════════════");
            Console.Write("  Opción: ");

            var input = Console.ReadLine()?.Trim();
            Console.WriteLine();

            switch (input)
            {
                case "1":
                    EditCandidateModality(settings);
                    break;
                case "2":
                    EditCandidateRegions(settings);
                    break;
                case "3":
                    EditCandidateSkills(settings);
                    break;
                case "0":
                    return;
                default:
                    Console.WriteLine("  ❌ Opción inválida.");
                    break;
            }
        }
    }

    private static void ShowCandidateGroupC(AppSettings settings)
    {
        while (true)
        {
            Console.WriteLine();
            Console.WriteLine("════════════════════════════════════════════════════════════");
            Console.WriteLine("  [C] BÚSQUEDA Y CV");
            Console.WriteLine("════════════════════════════════════════════════════════════");
            Console.WriteLine($"  [1] Keywords búsqueda   → {(settings.LinkedIn.Keywords.Count > 0 ? string.Join(", ", settings.LinkedIn.Keywords) : "(no configuradas)")}");
            Console.WriteLine($"  [2] Descripción CV      → {(string.IsNullOrEmpty(settings.Candidate.CvDescription) ? "(vacía)" : $"{settings.Candidate.CvDescription.Length} caracteres")}");
            Console.WriteLine($"  [3] Chat ID Telegram    → {(string.IsNullOrEmpty(settings.Telegram.ChatId) ? "(no configurado)" : settings.Telegram.ChatId)}");
            Console.WriteLine("────────────────────────────────────────────────────────────");
            Console.WriteLine("  [0] Volver al menú de perfil");
            Console.WriteLine("════════════════════════════════════════════════════════════");
            Console.Write("  Opción: ");

            var input = Console.ReadLine()?.Trim();
            Console.WriteLine();

            switch (input)
            {
                case "1":
                    EditCandidateKeywords(settings);
                    break;
                case "2":
                    EditCandidateCvDescription(settings);
                    break;
                case "3":
                    EditCandidateTelegramChatId(settings);
                    break;
                case "0":
                    return;
                default:
                    Console.WriteLine("  ❌ Opción inválida.");
                    break;
            }
        }
    }

    // ── Métodos de edición individuales ──────────────────────────────

    private static void EditCandidateName(AppSettings settings)
    {
        Console.Write($"  Tu nombre actual: {settings.Candidate.Name}");
        Console.WriteLine();
        Console.Write("  Nuevo nombre: ");
        var name = Console.ReadLine()?.Trim();
        if (!string.IsNullOrEmpty(name))
        {
            settings.Candidate.Name = name;
            Console.WriteLine($"  ✅ Nombre: {name}");
        }
    }

    private static void EditCandidateLevel(AppSettings settings)
    {
        Console.WriteLine("  ¿Cuál es tu nivel de experiencia?");
        Console.WriteLine("    [1] Junior     → 0-2 años, buscando primer empleo");
        Console.WriteLine("    [2] Mid        → 3-5 años, con experiencia sólida");
        Console.WriteLine("    [3] Senior     → 5+ años, liderando proyectos");
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
        Console.WriteLine($"  ✅ Nivel: {level}");
    }

    private static void EditCandidateYears(AppSettings settings)
    {
        Console.Write($"  Años de experiencia: {settings.Candidate.YearsExperience}");
        Console.WriteLine();
        Console.Write("  Nuevos años: ");
        var yearsInput = Console.ReadLine()?.Trim();
        if (int.TryParse(yearsInput, out var years) && years >= 0 && years <= 50)
        {
            settings.Candidate.YearsExperience = years;
            Console.WriteLine($"  ✅ Experiencia: {years} años");
        }
        else if (!string.IsNullOrEmpty(yearsInput))
            Console.WriteLine("  ❌ Ingresa un número entre 0 y 50.");
    }

    private static void EditCandidateSkills(AppSettings settings)
    {
        Console.WriteLine("  Tus habilidades técnicas principales.");
        Console.WriteLine("  Ejemplo: Node.js, TypeScript, Python, PostgreSQL, React");
        Console.WriteLine();
        Console.WriteLine($"  Actuales: {(settings.Candidate.CoreSkills.Count > 0 ? string.Join(", ", settings.Candidate.CoreSkills) : "(ninguna)")}");
        Console.Write("  Nuevas skills (separadas por coma): ");
        var skillsInput = Console.ReadLine()?.Trim();
        if (!string.IsNullOrEmpty(skillsInput))
        {
            var skills = skillsInput.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim()).ToList();
            settings.Candidate.CoreSkills = skills;
            Console.WriteLine($"  ✅ Skills: {string.Join(", ", skills)}");
        }
    }

    private static void EditCandidateModality(AppSettings settings)
    {
        Console.WriteLine("  ¿Cómo prefieres trabajar?");
        Console.WriteLine("    [1] Remoto      → Trabajar desde casa");
        Console.WriteLine("    [2] Híbrido     → Mixto oficina/casa");
        Console.WriteLine("    [3] Presencial  → Solo oficina");
        Console.WriteLine("    [4] Cualquiera  → Sin preferencia");
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
        Console.WriteLine($"  ✅ Modalidad: {modality}");
    }

    private static void EditCandidateRegions(AppSettings settings)
    {
        Console.WriteLine("  ¿En qué regiones quieres trabajar?");
        Console.WriteLine("  Ejemplo: Colombia, Latin America, Europe, Remote");
        Console.WriteLine();
        Console.WriteLine($"  Actuales: {(settings.Candidate.PreferredRegions.Count > 0 ? string.Join(", ", settings.Candidate.PreferredRegions) : "(ninguna)")}");
        Console.Write("  Nuevas regiones (separadas por coma): ");
        var regionsInput = Console.ReadLine()?.Trim();
        if (!string.IsNullOrEmpty(regionsInput))
        {
            var regions = regionsInput.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(r => r.Trim()).ToList();
            settings.Candidate.PreferredRegions = regions;
            Console.WriteLine($"  ✅ Regiones: {string.Join(", ", regions)}");
        }
    }

    private static void EditCandidateKeywords(AppSettings settings)
    {
        Console.WriteLine("  Palabras clave para buscar ofertas.");
        Console.WriteLine("  Ejemplo: Node.js, JavaScript, Backend, Full-Stack");
        Console.WriteLine();
        Console.WriteLine($"  Actuales: {(settings.LinkedIn.Keywords.Count > 0 ? string.Join(", ", settings.LinkedIn.Keywords) : "(ninguna)")}");
        Console.Write("  Nuevas keywords (separadas por coma): ");
        var keywordsInput = Console.ReadLine()?.Trim();
        if (!string.IsNullOrEmpty(keywordsInput))
        {
            var keywords = keywordsInput.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(k => k.Trim()).ToList();
            settings.LinkedIn.Keywords = keywords;
            if (settings.JobSources.ContainsKey("CompuTrabajo"))
                settings.JobSources["CompuTrabajo"].Keywords = keywords;
            Console.WriteLine($"  ✅ Keywords: {string.Join(", ", keywords)}");
        }
    }

    private static void EditCandidateCvDescription(AppSettings settings)
    {
        Console.WriteLine("  Descripción breve de tu perfil profesional.");
        Console.WriteLine("  Esto ayuda a la IA a entender tu perfil.");
        Console.WriteLine();
        Console.WriteLine($"  Actual: {(string.IsNullOrEmpty(settings.Candidate.CvDescription) ? "(vacía)" : settings.Candidate.CvDescription)}");
        Console.WriteLine("  Escribe tu descripción (Enter dos veces para terminar):");
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
            Console.WriteLine($"  ✅ CV actualizado ({cvDescription.Length} caracteres)");
        }
    }

    private static void EditCandidateTelegramChatId(AppSettings settings)
    {
        Console.WriteLine("  Tu Chat ID de Telegram para recibir alertas.");
        Console.WriteLine();
        Console.WriteLine("  Pasos:");
        Console.WriteLine("  1. Abre Telegram y busca @userinfobot");
        Console.WriteLine("  2. Envía /start");
        Console.WriteLine("  3. Copia tu ID numérico");
        Console.WriteLine("  4. Pégalo aquí abajo");
        Console.WriteLine();
        Console.Write($"  Tu Chat ID actual: {(string.IsNullOrEmpty(settings.Telegram.ChatId) ? "(no configurado)" : settings.Telegram.ChatId)}");
        Console.WriteLine();
        Console.Write("  Nuevo Chat ID: ");
        var chatId = Console.ReadLine()?.Trim();
        if (!string.IsNullOrEmpty(chatId))
        {
            settings.Telegram.ChatId = chatId;
            Console.WriteLine($"  ✅ Chat ID: {chatId}");
            Console.WriteLine("  💡 Prueba enviar /start a @CareerSentinel_bot");
        }
    }

    public static void ShowTelegramConfig(AppSettings settings)
    {
        Console.WriteLine("════════════════════════════════════════════════════════════");
        Console.WriteLine("  Configurar Telegram");
        Console.WriteLine("════════════════════════════════════════════════════════════");
        Console.WriteLine();
        Console.WriteLine("  Para recibir alertas de empleo por Telegram:");
        Console.WriteLine();
        Console.WriteLine("  1. Abre Telegram y busca: @CareerSentinel_bot");
        Console.WriteLine("     ─────────────────────────────────────────");
        Console.WriteLine("     https://t.me/CareerSentinel_bot");
        Console.WriteLine("     ─────────────────────────────────────────");
        Console.WriteLine();
        Console.WriteLine("  2. Envía /start al bot");
        Console.WriteLine();
        Console.WriteLine("  3. Abre @userinfobot y envía /start");
        Console.WriteLine("     → Copia tu ID numérico");
        Console.WriteLine();
        Console.WriteLine($"  Tu Chat ID actual: {(string.IsNullOrEmpty(settings.Telegram.ChatId) ? "(no configurado)" : settings.Telegram.ChatId)}");
        Console.WriteLine();
        Console.Write("  Pega tu Chat ID aquí: ");
        var chatId = Console.ReadLine()?.Trim();
        if (!string.IsNullOrEmpty(chatId))
        {
            settings.Telegram.ChatId = chatId;
            Console.WriteLine();
            Console.WriteLine($"  ✅ Chat ID configurado: {chatId}");
            Console.WriteLine("  💡 Ya puedes recibir alertas de empleo");
        }
        else
        {
            Console.WriteLine("  ⏭️  Saltado. Puedes configurarlo después con [1]");
        }
        Console.WriteLine();
    }

    public static void ShowLlmConfig(AppSettings settings)
    {
        while (true)
        {
            Console.WriteLine();
            Console.WriteLine("══════════════════════════════════════════");
            Console.WriteLine("  Configuración del Modelo de Lenguaje");
            Console.WriteLine("══════════════════════════════════════════");
            Console.WriteLine($"  Modo:          {settings.ProcessingMode}");
            Console.WriteLine($"  API URL:       {settings.OpenCodeGo.BaseUrl}");
            Console.WriteLine($"  API Model:     {settings.OpenCodeGo.ModelName}");
            Console.WriteLine($"  API Key:       {(string.IsNullOrEmpty(settings.OpenCodeGo.ApiKey) ? "(no configurada)" : $"****{settings.OpenCodeGo.ApiKey[Math.Max(0, settings.OpenCodeGo.ApiKey.Length - 4)..]}")}");
            Console.WriteLine($"  Max Tokens:    {settings.OpenCodeGo.MaxTokensBatch}");
            Console.WriteLine($"  Timeout:       {settings.OpenCodeGo.TimeoutSeconds}s");
            Console.WriteLine($"  Ollama URL:    {settings.Ollama.BaseUrl}");
            Console.WriteLine($"  Ollama Model:  {settings.Ollama.ModelName}");
            Console.WriteLine("══════════════════════════════════════════");
            Console.WriteLine("  [1] Cambiar modo (Local/API/Hybrid)");
            Console.WriteLine("  [2] Seleccionar proveedor de API");
            Console.WriteLine("  [3] Configurar Ollama local");
            Console.WriteLine("  [0] Volver");
            Console.WriteLine("══════════════════════════════════════════");
            Console.Write("  Opción: ");

            var input = Console.ReadLine()?.Trim();
            Console.WriteLine();

            switch (input)
            {
                case "1":
                    Console.WriteLine("  Modos:");
                    Console.WriteLine("    [1] Local   - Ollama (gratis)");
                    Console.WriteLine("    [2] API     - Proveedor externo");
                    Console.WriteLine("    [3] Hybrid  - Ollama + API");
                    Console.Write("  → ");
                    settings.ProcessingMode = (Console.ReadLine()?.Trim()) switch
                    {
                        "1" => ProcessingMode.Local,
                        "2" => ProcessingMode.API,
                        "3" => ProcessingMode.Hybrid,
                        _ => settings.ProcessingMode
                    };
                    Console.WriteLine($"  → Modo: {settings.ProcessingMode}");
                    break;

                case "2":
                    ShowProviderPicker(settings);
                    break;

                case "3":
                    ConfigureOllama(settings);
                    break;

                case "0":
                    return;
            }
        }
    }

    private static void ShowProviderPicker(AppSettings settings)
    {
        Console.WriteLine("══════════════════════════════════════════");
        Console.WriteLine("  Seleccionar Proveedor de API");
        Console.WriteLine("══════════════════════════════════════════");
        Console.WriteLine("  GRATIS / BARATOS:");
        Console.WriteLine("    [1] Gemini 3.5 Flash Lite  (gratis, 1500 req/día)");
        Console.WriteLine("    [2] Gemini 2.5 Flash       (gratis, 1500 req/día)");
        Console.WriteLine("    [3] Groq Llama 3.3 70B     (gratis, 30 req/min)");
        Console.WriteLine("    [4] Groq Gemma 2 9B        (gratis, 30 req/min)");
        Console.WriteLine("    [5] OpenRouter Free Models (gratis, limitado)");
        Console.WriteLine("  DE PAGO:");
        Console.WriteLine("    [6] OpenAI GPT-4o Mini     (~$0.15/1M tokens)");
        Console.WriteLine("    [7] OpenAI GPT-4o          (~$2.50/1M tokens)");
        Console.WriteLine("    [8] Anthropic Claude 3.5   (~$3/1M tokens)");
        Console.WriteLine("    [9] Deepseek Chat          (~$0.14/1M tokens)");
        Console.WriteLine("    [10] Deepseek Coder        (~$0.28/1M tokens)");
        Console.WriteLine("  PERSONALIZADO:");
        Console.WriteLine("    [0] Ingresar URL y modelo manualmente");
        Console.WriteLine("══════════════════════════════════════════");
        Console.Write("  Selecciona: ");

        var choice = Console.ReadLine()?.Trim();
        Console.WriteLine();

        switch (choice)
        {
            case "1":
                settings.OpenCodeGo.BaseUrl = "https://generativelanguage.googleapis.com/v1beta/openai/";
                settings.OpenCodeGo.ModelName = "gemini-3.5-flash-lite";
                Console.WriteLine("  → Gemini 3.5 Flash Lite seleccionado");
                break;
            case "2":
                settings.OpenCodeGo.BaseUrl = "https://generativelanguage.googleapis.com/v1beta/openai/";
                settings.OpenCodeGo.ModelName = "gemini-2.5-flash";
                Console.WriteLine("  → Gemini 2.5 Flash seleccionado");
                break;
            case "3":
                settings.OpenCodeGo.BaseUrl = "https://api.groq.com/openai/v1/";
                settings.OpenCodeGo.ModelName = "llama-3.3-70b-versatile";
                Console.WriteLine("  → Groq Llama 3.3 70B seleccionado");
                break;
            case "4":
                settings.OpenCodeGo.BaseUrl = "https://api.groq.com/openai/v1/";
                settings.OpenCodeGo.ModelName = "gemma2-9b-it";
                Console.WriteLine("  → Groq Gemma 2 9B seleccionado");
                break;
            case "5":
                settings.OpenCodeGo.BaseUrl = "https://openrouter.ai/api/v1/";
                settings.OpenCodeGo.ModelName = "meta-llama/llama-3.1-8b-instruct:free";
                Console.WriteLine("  → OpenRouter Free seleccionado");
                break;
            case "6":
                settings.OpenCodeGo.BaseUrl = "https://api.openai.com/v1/";
                settings.OpenCodeGo.ModelName = "gpt-4o-mini";
                Console.WriteLine("  → OpenAI GPT-4o Mini seleccionado");
                break;
            case "7":
                settings.OpenCodeGo.BaseUrl = "https://api.openai.com/v1/";
                settings.OpenCodeGo.ModelName = "gpt-4o";
                Console.WriteLine("  → OpenAI GPT-4o seleccionado");
                break;
            case "8":
                settings.OpenCodeGo.BaseUrl = "https://api.anthropic.com/v1/";
                settings.OpenCodeGo.ModelName = "claude-3-5-sonnet-20241022";
                Console.WriteLine("  → Anthropic Claude 3.5 seleccionado");
                break;
            case "9":
                settings.OpenCodeGo.BaseUrl = "https://api.deepseek.com/v1/";
                settings.OpenCodeGo.ModelName = "deepseek-chat";
                Console.WriteLine("  → Deepseek Chat seleccionado");
                break;
            case "10":
                settings.OpenCodeGo.BaseUrl = "https://api.deepseek.com/v1/";
                settings.OpenCodeGo.ModelName = "deepseek-coder";
                Console.WriteLine("  → Deepseek Coder seleccionado");
                break;
            case "0":
                ConfigureCustomApi(settings);
                return;
            default:
                Console.WriteLine("  Opción inválida.");
                return;
        }

        // Pedir API Key
        Console.Write("  API Key: ");
        var key = Console.ReadLine()?.Trim();
        if (!string.IsNullOrEmpty(key))
            settings.OpenCodeGo.ApiKey = key;

        // Max tokens
        Console.Write($"  Max Tokens ({settings.OpenCodeGo.MaxTokensBatch}): ");
        var tokensInput = Console.ReadLine()?.Trim();
        if (int.TryParse(tokensInput, out var tokens) && tokens > 0)
            settings.OpenCodeGo.MaxTokensBatch = tokens;

        settings.ProcessingMode = ProcessingMode.API;
        Console.WriteLine("  → Modo cambiado a API automáticamente");
    }

    private static void ConfigureCustomApi(AppSettings settings)
    {
        Console.WriteLine("══════════════════════════════════════════");
        Console.WriteLine("  Configurar API Personalizada");
        Console.WriteLine("══════════════════════════════════════════");
        Console.WriteLine("  Debe ser compatible con formato OpenAI:");
        Console.WriteLine("  POST {BaseUrl}/chat/completions");
        Console.WriteLine("  Headers: Authorization: Bearer {API_KEY}");
        Console.WriteLine("══════════════════════════════════════════");

        Console.Write($"  Base URL ({settings.OpenCodeGo.BaseUrl}): ");
        var url = Console.ReadLine()?.Trim();
        if (!string.IsNullOrEmpty(url))
            settings.OpenCodeGo.BaseUrl = url;

        Console.Write($"  Modelo ({settings.OpenCodeGo.ModelName}): ");
        var model = Console.ReadLine()?.Trim();
        if (!string.IsNullOrEmpty(model))
            settings.OpenCodeGo.ModelName = model;

        Console.Write("  API Key: ");
        var key = Console.ReadLine()?.Trim();
        if (!string.IsNullOrEmpty(key))
            settings.OpenCodeGo.ApiKey = key;

        Console.Write($"  Max Tokens ({settings.OpenCodeGo.MaxTokensBatch}): ");
        var tokensInput = Console.ReadLine()?.Trim();
        if (int.TryParse(tokensInput, out var tokens) && tokens > 0)
            settings.OpenCodeGo.MaxTokensBatch = tokens;

        Console.Write($"  Timeout segundos ({settings.OpenCodeGo.TimeoutSeconds}): ");
        var timeoutInput = Console.ReadLine()?.Trim();
        if (int.TryParse(timeoutInput, out var timeout) && timeout > 0)
            settings.OpenCodeGo.TimeoutSeconds = timeout;

        settings.ProcessingMode = ProcessingMode.API;
        Console.WriteLine($"  → API configurada: {settings.OpenCodeGo.ModelName}");
        Console.WriteLine("  → Modo cambiado a API automáticamente");
    }

    private static void ConfigureOllama(AppSettings settings)
    {
        Console.WriteLine("══════════════════════════════════════════");
        Console.WriteLine("  Configurar Ollama Local");
        Console.WriteLine("══════════════════════════════════════════");

        Console.Write($"  URL ({settings.Ollama.BaseUrl}): ");
        var url = Console.ReadLine()?.Trim();
        if (!string.IsNullOrEmpty(url))
            settings.Ollama.BaseUrl = url;

        Console.Write($"  Modelo ({settings.Ollama.ModelName}): ");
        Console.WriteLine("  (qwen2.5:3b, llama3.1:8b, mistral:7b, etc.)");
        var model = Console.ReadLine()?.Trim();
        if (!string.IsNullOrEmpty(model))
            settings.Ollama.ModelName = model;

        settings.ProcessingMode = ProcessingMode.Local;
        Console.WriteLine($"  → Ollama: {settings.Ollama.ModelName}");
        Console.WriteLine("  → Modo cambiado a Local automáticamente");
    }

    public static void ShowSources(AppSettings settings)
    {
        Console.WriteLine();
        Console.WriteLine("════════════════════════════════════════════════════════════");
        Console.WriteLine("  Fuentes de Empleo");
        Console.WriteLine("════════════════════════════════════════════════════════════");

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

        Console.WriteLine("════════════════════════════════════════════════════════════");
        Console.WriteLine();
    }

    public static void ConfigureSources(AppSettings settings)
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

    public static void ShowSourcesSubmenu(AppSettings settings, ConfigurationService configService)
    {
        while (true)
        {
            Console.WriteLine();
            Console.WriteLine("════════════════════════════════════════════════════════════");
            Console.WriteLine("  Fuentes de Empleo");
            Console.WriteLine("════════════════════════════════════════════════════════════");
            Console.WriteLine("  [1] Ver fuentes habilitadas");
            Console.WriteLine("  [2] Habilitar/deshabilitar fuente");
            Console.WriteLine("────────────────────────────────────────────────────────────");
            Console.WriteLine("  [0] Volver al menú principal");
            Console.WriteLine("════════════════════════════════════════════════════════════");
            Console.Write("  Opción: ");

            var input = Console.ReadLine()?.Trim();
            Console.WriteLine();

            switch (input)
            {
                case "1":
                    ShowSources(settings);
                    break;

                case "2":
                    ConfigureSources(settings);
                    configService.Save(settings);
                    Console.WriteLine("  ✅ Configuración guardada en appsettings.json");
                    break;

                case "0":
                    return;

                default:
                    Console.WriteLine("  ❌ Opción inválida.");
                    break;
            }
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

    // ── Wizard de primer uso ─────────────────────────────────────────────

    /// <summary>
    /// Detecta si el usuario necesita el wizard de configuración inicial.
    /// Retorna true si Telegram.ChatId está vacío o CoreSkills está vacío.
    /// </summary>
    public static bool IsFirstTimeSetup(AppSettings settings)
    {
        return string.IsNullOrEmpty(settings.Telegram.ChatId)
            || settings.Candidate.CoreSkills is null
            || settings.Candidate.CoreSkills.Count == 0;
    }

    /// <summary>
    /// Wizard guiado de primer uso. Guía al usuario paso a paso por:
    /// 1) Telegram, 2) Perfil, 3) Inteligencia Artificial.
    /// Muestra valores actuales y permite mantenerlos presionando Enter.
    /// Guarda la configuración solo al final del wizard.
    /// </summary>
    public static void ShowSetupWizard(AppSettings settings, ConfigurationService configService)
    {
        // ── Pantalla de bienvenida ───────────────────────────────────────
        Console.Clear();
        Console.WriteLine();
        Console.WriteLine("════════════════════════════════════════════════════════════");
        Console.WriteLine("  ¡Bienvenido a CareerSentinel!");
        Console.WriteLine("  Buscador Inteligente de Empleo con IA");
        Console.WriteLine("════════════════════════════════════════════════════════════");
        Console.WriteLine();
        Console.WriteLine("  Vamos a configurarte en 3 simples pasos.");
        Console.WriteLine("  Si ya tienes algo configurado, presiona Enter para mantenerlo.");
        Console.WriteLine();
        Console.WriteLine("  ┌─ Pasos ──────────────────────────────────────────────┐");
        Console.WriteLine("  │  [1] Configurar Telegram (recibir alertas)          │");
        Console.WriteLine("  │  [2] Tu perfil (evaluar compatibilidad)              │");
        Console.WriteLine("  │  [3] Inteligencia Artificial (analizar ofertas)     │");
        Console.WriteLine("  └──────────────────────────────────────────────────────┘");
        Console.WriteLine();
        Console.Write("  Presiona Enter para comenzar...");
        Console.ReadLine();

        // ── Paso 1: Telegram ─────────────────────────────────────────────
        SetupWizardTelegram(settings);

        // ── Paso 2: Perfil ───────────────────────────────────────────────
        SetupWizardProfile(settings);

        // ── Paso 3: IA ───────────────────────────────────────────────────
        SetupWizardAI(settings);

        // ── Guardar todo al final ────────────────────────────────────────
        configService.Save(settings);

        // ── Resumen ──────────────────────────────────────────────────────
        ShowWizardSummary(settings);

        Console.Write("  Presiona Enter para ir al menú principal...");
        Console.ReadLine();
    }

    private static void SetupWizardTelegram(AppSettings settings)
    {
        Console.Clear();
        Console.WriteLine("════════════════════════════════════════════════════════════");
        Console.WriteLine("  Paso 1 de 3: Configurar Telegram");
        Console.WriteLine("════════════════════════════════════════════════════════════");
        Console.WriteLine();
        Console.WriteLine("  Telegram te permite recibir alertas de nuevas ofertas");
        Console.WriteLine("  directamente en tu celular. Es gratis.");
        Console.WriteLine();

        if (!string.IsNullOrEmpty(settings.Telegram.ChatId))
        {
            Console.WriteLine("  ┌─ Telegram ya configurado ─────────────────────────┐");
            Console.WriteLine($"  │  Tu Chat ID actual: {settings.Telegram.ChatId,-28}│");
            Console.WriteLine("  │                                                    │");
            Console.WriteLine("  │  Presiona ENTER para mantenerlo,                   │");
            Console.WriteLine("  │  o escribe uno nuevo:                              │");
            Console.WriteLine("  └────────────────────────────────────────────────────┘");
            Console.WriteLine();
            Console.Write("  → ");
            var chatId = Console.ReadLine()?.Trim();
            if (!string.IsNullOrEmpty(chatId))
            {
                settings.Telegram.ChatId = chatId;
                Console.WriteLine($"  ✅ Chat ID actualizado: {chatId}");
            }
            else
            {
                Console.WriteLine($"  ✅ Chat ID mantenido: {settings.Telegram.ChatId}");
            }
        }
        else
        {
            Console.WriteLine("  ┌─ Cómo obtener tu Chat ID ──────────────────────────┐");
            Console.WriteLine("  │  1. Abre Telegram y busca: @CareerSentinel_bot     │");
            Console.WriteLine("  │     https://t.me/CareerSentinel_bot                 │");
            Console.WriteLine("  │  2. Envía /start al bot                            │");
            Console.WriteLine("  │  3. Abre @userinfobot y envía /start               │");
            Console.WriteLine("  │  4. Copia tu ID numérico                           │");
            Console.WriteLine("  └──────────────────────────────────────────────────────┘");
            Console.WriteLine();
            Console.Write("  Tu Chat ID: ");
            var chatId = Console.ReadLine()?.Trim();
            if (!string.IsNullOrEmpty(chatId))
            {
                settings.Telegram.ChatId = chatId;
                Console.WriteLine($"  ✅ Chat ID configurado: {chatId}");
            }
            else
            {
                Console.WriteLine("  ⏭️  Saltado. Puedes configurarlo después desde el menú.");
            }
        }
        Console.WriteLine();
    }

    private static void SetupWizardProfile(AppSettings settings)
    {
        Console.Clear();
        Console.WriteLine("════════════════════════════════════════════════════════════");
        Console.WriteLine("  Paso 2 de 3: Tu Perfil");
        Console.WriteLine("════════════════════════════════════════════════════════════");
        Console.WriteLine();
        Console.WriteLine("  Esta información nos ayuda a evaluar si una oferta");
        Console.WriteLine("  es compatible con tu perfil profesional.");
        Console.WriteLine();
        Console.WriteLine("  Presiona ENTER en cualquier paso para mantener el valor actual.");
        Console.WriteLine();

        // ── Nombre ───────────────────────────────────────────────────────
        Console.WriteLine("  --- Tu nombre ---");
        Console.WriteLine($"  Tu nombre actual: {settings.Candidate.Name}");
        Console.Write("  Presiona ENTER para mantener, o escribe uno nuevo: ");
        var name = Console.ReadLine()?.Trim();
        if (!string.IsNullOrEmpty(name))
        {
            settings.Candidate.Name = name;
            Console.WriteLine($"  ✅ Nombre actualizado: {name}");
        }
        else
        {
            Console.WriteLine($"  ✅ Nombre mantenido: {settings.Candidate.Name}");
        }
        Console.WriteLine();

        // ── Nivel ────────────────────────────────────────────────────────
        Console.WriteLine("  --- Tu nivel de experiencia ---");
        Console.WriteLine($"  Tu nivel actual: {settings.Candidate.Level}");
        Console.WriteLine("    [1] Junior   → 0-2 años");
        Console.WriteLine("    [2] Mid      → 3-5 años");
        Console.WriteLine("    [3] Senior   → 5+ años");
        Console.Write("  Presiona ENTER para mantener, o selecciona uno nuevo: ");
        var levelInput = Console.ReadLine()?.Trim();
        var level = levelInput switch
        {
            "1" => "Junior",
            "2" => "Mid",
            "3" => "Senior",
            _ => null
        };
        if (level != null)
        {
            settings.Candidate.Level = level;
            Console.WriteLine($"  ✅ Nivel actualizado: {level}");
        }
        else
        {
            Console.WriteLine($"  ✅ Nivel mantenido: {settings.Candidate.Level}");
        }
        Console.WriteLine();

        // ── Skills ───────────────────────────────────────────────────────
        Console.WriteLine("  --- Tus habilidades técnicas ---");
        var currentSkills = settings.Candidate.CoreSkills.Count > 0
            ? string.Join(", ", settings.Candidate.CoreSkills)
            : "(ninguna configurada)";
        Console.WriteLine($"  Tus skills actuales: {currentSkills}");
        Console.WriteLine("  Ejemplo: Node.js, TypeScript, Python, React, PostgreSQL");
        Console.Write("  Presiona ENTER para mantener, o escribe las nuevas (separadas por coma): ");
        var skillsInput = Console.ReadLine()?.Trim();
        if (!string.IsNullOrEmpty(skillsInput))
        {
            var skills = skillsInput.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim()).ToList();
            settings.Candidate.CoreSkills = skills;
            Console.WriteLine($"  ✅ Skills actualizadas: {string.Join(", ", skills)}");
        }
        else
        {
            if (settings.Candidate.CoreSkills.Count > 0)
                Console.WriteLine($"  ✅ Skills mantenidas: {currentSkills}");
            else
                Console.WriteLine("  ⏭️  Sin skills configuradas. Puedes agregarlas después desde el menú [2].");
        }
        Console.WriteLine();

        // ── Modalidad ────────────────────────────────────────────────────
        Console.WriteLine("  --- Modalidad preferida ---");
        Console.WriteLine($"  Tu modalidad actual: {settings.Candidate.PreferredModality}");
        Console.WriteLine("    [1] Remoto     → Desde casa");
        Console.WriteLine("    [2] Híbrido    → Mixto oficina/casa");
        Console.WriteLine("    [3] Presencial → Solo oficina");
        Console.WriteLine("    [4] Cualquiera → Sin preferencia");
        Console.Write("  Presiona ENTER para mantener, o selecciona uno nuevo: ");
        var modalityInput = Console.ReadLine()?.Trim();
        var modality = modalityInput switch
        {
            "1" => "Remoto",
            "2" => "Híbrido",
            "3" => "Presencial",
            "4" => "Cualquiera",
            _ => null
        };
        if (modality != null)
        {
            settings.Candidate.PreferredModality = modality;
            Console.WriteLine($"  ✅ Modalidad actualizada: {modality}");
        }
        else
        {
            Console.WriteLine($"  ✅ Modalidad mantenido: {settings.Candidate.PreferredModality}");
        }
        Console.WriteLine();
    }

    private static void SetupWizardAI(AppSettings settings)
    {
        Console.Clear();
        Console.WriteLine("════════════════════════════════════════════════════════════");
        Console.WriteLine("  Paso 3 de 3: Inteligencia Artificial");
        Console.WriteLine("════════════════════════════════════════════════════════════");
        Console.WriteLine();
        Console.WriteLine("  La IA analiza cada oferta y evalúa si es compatible");
        Console.WriteLine("  con tu perfil.");
        Console.WriteLine();
        Console.WriteLine("  Presiona ENTER en cualquier paso para mantener el valor actual.");
        Console.WriteLine();

        // ── Modo ─────────────────────────────────────────────────────────
        Console.WriteLine("  --- Modo de IA ---");
        Console.WriteLine($"  Modo actual: {settings.ProcessingMode}");
        Console.WriteLine("    [1] Local    → Ollama (gratis, en tu PC)");
        Console.WriteLine("    [2] API      → Proveedor externo");
        Console.WriteLine("    [3] Hybrid   → Ollama + API");
        Console.Write("  Presiona ENTER para mantener, o selecciona uno nuevo: ");
        var modeInput = Console.ReadLine()?.Trim();
        var mode = modeInput switch
        {
            "1" => ProcessingMode.Local,
            "2" => ProcessingMode.API,
            "3" => ProcessingMode.Hybrid,
            _ => (ProcessingMode?)null
        };
        if (mode.HasValue)
        {
            settings.ProcessingMode = mode.Value;
            Console.WriteLine($"  ✅ Modo actualizado: {mode.Value}");
        }
        else
        {
            Console.WriteLine($"  ✅ Modo mantenido: {settings.ProcessingMode}");
        }
        Console.WriteLine();

        // ── Proveedor (solo si modo es API o Hybrid) ────────────────────
        if (settings.ProcessingMode == ProcessingMode.API || settings.ProcessingMode == ProcessingMode.Hybrid)
        {
            var currentProvider = DetectCurrentProvider(settings);

            Console.WriteLine("  --- Proveedor de API ---");
            if (!string.IsNullOrEmpty(currentProvider))
                Console.WriteLine($"  Proveedor actual: {currentProvider} ({settings.OpenCodeGo.ModelName})");
            else if (!string.IsNullOrEmpty(settings.OpenCodeGo.ApiKey))
                Console.WriteLine($"  Proveedor actual: {settings.OpenCodeGo.BaseUrl} / {settings.OpenCodeGo.ModelName}");
            else
                Console.WriteLine("  Proveedor actual: (ninguno configurado)");
            Console.WriteLine();
            Console.WriteLine("  ── GRATIS ────────────────────────────────────────────");
            Console.WriteLine("    [1] Gemini 3.5 Flash Lite (gratis, 1500 req/día)");
            Console.WriteLine("    [2] Gemini 2.5 Flash (gratis, 1500 req/día)");
            Console.WriteLine("    [3] Groq Llama 3.3 70B (gratis, 30 req/min)");
            Console.WriteLine("  ── DE PAGO ──────────────────────────────────────────");
            Console.WriteLine("    [4] OpenAI GPT-4o Mini (~$0.15/1M tokens)");
            Console.WriteLine("    [5] Deepseek Chat (~$0.14/1M tokens)");
            Console.WriteLine("  ── PERSONALIZADO ────────────────────────────────────");
            Console.WriteLine("    [0] Configurar manualmente (menú completo)");
            Console.WriteLine("════════════════════════════════════════════════════════════");
            Console.Write("  Presiona ENTER para mantener proveedor actual, o selecciona uno nuevo: ");

            var choice = Console.ReadLine()?.Trim();
            Console.WriteLine();

            var changed = false;
            switch (choice)
            {
                case "1":
                    settings.OpenCodeGo.BaseUrl = "https://generativelanguage.googleapis.com/v1beta/openai/";
                    settings.OpenCodeGo.ModelName = "gemini-3.5-flash-lite";
                    settings.ProcessingMode = ProcessingMode.API;
                    Console.WriteLine("  → Gemini 3.5 Flash Lite seleccionado");
                    changed = true;
                    break;
                case "2":
                    settings.OpenCodeGo.BaseUrl = "https://generativelanguage.googleapis.com/v1beta/openai/";
                    settings.OpenCodeGo.ModelName = "gemini-2.5-flash";
                    settings.ProcessingMode = ProcessingMode.API;
                    Console.WriteLine("  → Gemini 2.5 Flash seleccionado");
                    changed = true;
                    break;
                case "3":
                    settings.OpenCodeGo.BaseUrl = "https://api.groq.com/openai/v1/";
                    settings.OpenCodeGo.ModelName = "llama-3.3-70b-versatile";
                    settings.ProcessingMode = ProcessingMode.API;
                    Console.WriteLine("  → Groq Llama 3.3 70B seleccionado");
                    changed = true;
                    break;
                case "4":
                    settings.OpenCodeGo.BaseUrl = "https://api.openai.com/v1/";
                    settings.OpenCodeGo.ModelName = "gpt-4o-mini";
                    settings.ProcessingMode = ProcessingMode.API;
                    Console.WriteLine("  → OpenAI GPT-4o Mini seleccionado");
                    changed = true;
                    break;
                case "5":
                    settings.OpenCodeGo.BaseUrl = "https://api.deepseek.com/v1/";
                    settings.OpenCodeGo.ModelName = "deepseek-chat";
                    settings.ProcessingMode = ProcessingMode.API;
                    Console.WriteLine("  → Deepseek Chat seleccionado");
                    changed = true;
                    break;
                case "0":
                    ShowLlmConfig(settings);
                    return;
                default:
                    if (!string.IsNullOrEmpty(choice))
                    {
                        Console.WriteLine("  ⏭️  Opción no reconocida. Manteniendo proveedor actual.");
                    }
                    else
                    {
                        Console.WriteLine($"  ✅ Proveedor mantenido: {settings.OpenCodeGo.ModelName}");
                    }
                    break;
            }

            // API Key — solo pedir si se cambió el proveedor
            if (changed)
            {
                if (!string.IsNullOrEmpty(settings.OpenCodeGo.ApiKey))
                {
                    Console.WriteLine($"  Tu API Key actual: ****{settings.OpenCodeGo.ApiKey[Math.Max(0, settings.OpenCodeGo.ApiKey.Length - 4)..]}");
                    Console.Write("  Presiona ENTER para mantener, o escribe una nueva: ");
                }
                else
                {
                    Console.Write("  API Key (deja vacío para saltar): ");
                }
                var key = Console.ReadLine()?.Trim();
                if (!string.IsNullOrEmpty(key))
                {
                    settings.OpenCodeGo.ApiKey = key;
                    Console.WriteLine("  ✅ API Key configurada");
                }
                else
                {
                    if (!string.IsNullOrEmpty(settings.OpenCodeGo.ApiKey))
                        Console.WriteLine("  ✅ API Key mantenida");
                    else
                        Console.WriteLine("  ⏭️  Saltado. Configúrala después desde el menú [3].");
                }
            }
        }
        else
        {
            // Modo Local — configurar Ollama
            Console.WriteLine("  --- Configuración de Ollama (local) ---");
            Console.WriteLine($"  URL actual: {settings.Ollama.BaseUrl}");
            Console.WriteLine($"  Modelo actual: {settings.Ollama.ModelName}");
            Console.WriteLine();
            Console.Write("  Presiona ENTER para mantener Ollama, o escribe 'cambiar' para configurar: ");
            var ollamaInput = Console.ReadLine()?.Trim();
            if (string.Equals(ollamaInput, "cambiar", StringComparison.OrdinalIgnoreCase))
            {
                Console.Write($"  URL de Ollama ({settings.Ollama.BaseUrl}): ");
                var url = Console.ReadLine()?.Trim();
                if (!string.IsNullOrEmpty(url))
                    settings.Ollama.BaseUrl = url;

                Console.Write($"  Modelo ({settings.Ollama.ModelName}): ");
                Console.WriteLine("  (qwen2.5:3b, llama3.1:8b, mistral:7b, etc.)");
                var model = Console.ReadLine()?.Trim();
                if (!string.IsNullOrEmpty(model))
                    settings.Ollama.ModelName = model;

                Console.WriteLine($"  ✅ Ollama configurado: {settings.Ollama.ModelName}");
            }
            else
            {
                Console.WriteLine($"  ✅ Ollama mantenido: {settings.Ollama.ModelName} en {settings.Ollama.BaseUrl}");
            }
        }

        Console.WriteLine();
    }

    /// <summary>
    /// Detecta el proveedor actual basándose en la URL del API.
    /// </summary>
    private static string DetectCurrentProvider(AppSettings settings)
    {
        var url = settings.OpenCodeGo.BaseUrl?.ToLowerInvariant() ?? string.Empty;
        var model = settings.OpenCodeGo.ModelName?.ToLowerInvariant() ?? string.Empty;

        if (url.Contains("googleapis.com") && model.Contains("gemini"))
            return "Gemini";
        if (url.Contains("groq.com"))
            return "Groq";
        if (url.Contains("openai.com"))
            return "OpenAI";
        if (url.Contains("anthropic.com"))
            return "Anthropic";
        if (url.Contains("deepseek.com"))
            return "Deepseek";
        if (url.Contains("openrouter.ai"))
            return "OpenRouter";
        if (url.Contains("opencode.ai"))
            return "OpenCodeGo";

        return string.Empty;
    }

    private static void ShowWizardSummary(AppSettings settings)
    {
        Console.Clear();
        Console.WriteLine("════════════════════════════════════════════════════════════");
        Console.WriteLine("  ¡Configuración completada!");
        Console.WriteLine("════════════════════════════════════════════════════════════");
        Console.WriteLine();
        Console.WriteLine("  Resumen de tu configuración:");
        Console.WriteLine();
        Console.WriteLine($"  Telegram:      {(string.IsNullOrEmpty(settings.Telegram.ChatId) ? "No configurado" : $"Chat ID: {settings.Telegram.ChatId}")}");
        Console.WriteLine($"  Nombre:        {settings.Candidate.Name}");
        Console.WriteLine($"  Nivel:         {settings.Candidate.Level}");
        Console.WriteLine($"  Skills:        {(settings.Candidate.CoreSkills.Count > 0 ? string.Join(", ", settings.Candidate.CoreSkills) : "No configuradas")}");
        Console.WriteLine($"  Modalidad:     {settings.Candidate.PreferredModality}");
        Console.WriteLine($"  IA:            {settings.OpenCodeGo.ModelName} (modo {settings.ProcessingMode})");
        Console.WriteLine($"  API Key:       {(string.IsNullOrEmpty(settings.OpenCodeGo.ApiKey) ? "No configurada" : "Configurada")}");
        Console.WriteLine();
        Console.WriteLine("  Puedes cambiar cualquier opción desde el menú principal.");
        Console.WriteLine("════════════════════════════════════════════════════════════");
        Console.WriteLine();
    }
}
