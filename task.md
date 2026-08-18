# Plan de Desarrollo - CareerSentinel
## Estado Actual
**Ya existe y está completo (NO tocar):**
- `Configuration/AppSettings.cs` — Records de configuración
- `Models/JobOffer.cs` — DTO de oferta
- `Models/EvaluationResult.cs` — DTO de evaluación LLM
- `Services/ILinkedInScraper.cs` + `LinkedInScraper.cs` — Scraping LinkedIn
- `Services/LocalLlmService.cs` — Evaluación Ollama
- `Services/NotionService.cs` — Persistencia Notion
- `Services/TelegramAlertService.cs` — Alertas Telegram
- `Services/IJobCacheService.cs` + `JobCacheService.cs` — Cache local
**Necesita reestructuración/completado:**
- `CareerSentinel.csproj` — Cambiar de Worker SDK a Console SDK
- `Program.cs` — Reescribir como console app con DI + menú
- `Worker.cs` — Eliminar (es skeleton vacío)
- `Workers/` — Eliminar directorio
- `appsettings.json` — Completar con todas las secciones de config
---
## Fase 1: Reestructuración del Proyecto
### Tarea 1: Convertir .csproj de Worker a Console App
- Archivos: `src/CareerSentinel/CareerSentinel.csproj`
- Acción: Cambiar `Sdk="Microsoft.NET.Sdk.Worker"` → `Sdk="Microsoft.NET.Sdk"`. Agregar `<OutputType>Exe</OutputType>`. Mantener todos los PackageReference existentes. Eliminar referencia a `Microsoft.Extensions.Hosting` si se reemplaza por DI manual, O mantenerlo si se usa `Microsoft.Extensions.DependencyInjection` + `IHost` ligero.
- Criterio: `dotnet build` compila sin errores
- Dependencias: Ninguna
### Tarea 2: Eliminar archivos obsoletos
- Archivos: `src/CareerSentinel/Worker.cs`, `src/CareerSentinel/Workers/` (directorio)
- Acción: Eliminar `Worker.cs` (es el skeleton por defecto del template). Eliminar directorio `Workers/` si está vacío.
- Criterio: `dotnet build` compila sin errores (sin referencias rotas)
- Dependencias: Tarea 1
### Tarea 3: Completar appsettings.json con todas las secciones
- Archivos: `src/CareerSentinel/appsettings.json`
- Acción: Agregar las secciones: `Ollama` (BaseUrl, ModelName), `LinkedIn` (BaseUrl, Location, Keywords), `Notion` (ApiKey, DatabaseId), `Telegram` (BotToken, ChatId), `Scoring` (Threshold, MaxRetries, CvText), `RateLimiting` (DelayBetweenRequestsMs, DelayBetweenSearchesMs). Usar valores placeholder para secrets.
- Criterio: JSON válido, todas las secciones presentes, sin secrets reales
- Dependencias: Ninguna
---
## Fase 2: Orquestador Principal
### Tarea 4: Crear Services/JobOrchestrator.cs (clase normal, NO BackgroundService)
- Archivos: `src/CareerSentinel/Services/JobOrchestrator.cs`
- Acción: Crear clase `JobOrchestrator` que recibe por DI: `ILinkedInScraper`, `LocalLlmService`, `NotionService`, `TelegramAlertService`, `IJobCacheService`, `IOptions<AppSettings>`, `ILogger<JobOrchestrator>`. Método público `RunAsync(CancellationToken ct)` que ejecuta el flujo completo:
  1. Itera por cada keyword de config
  2. Scrapea LinkedIn para cada keyword
  3. Filtra ofertas ya vistas (cache)
  4. Para cada oferta nueva: obtiene descripción, evalúa con LLM
  5. Si score >= umbral: guarda en Notion + envía alerta Telegram
  6. Al finalizar: envía resumen diario por Telegram
  7. Rate limiting entre cada llamada (Task.Delay)
- Criterio: Compila sin errores, integra todos los services
- Dependencias: Tarea 1, Tarea 2
### Tarea 5: Crear Services/ConsoleMenu.cs (menú interactivo)
- Archivos: `src/CareerSentinel/Services/ConsoleMenu.cs`
- Acción: Crear clase estática `ConsoleMenu` con método `ShowMainMenu()` que muestra:
  ```
  ════════════════════════════════════════
   CareerSentinel - Buscador de Empleo
  ════════════════════════════════════════
   [1] Ejecutar búsqueda completa
   [2] Ver configuración actual
   [3] Configurar keywords
   [4] Configurar umbral de score
   [5] Salir
  ════════════════════════════════════════
  ```
  Retorna la opción elegida como int. Método `ShowResultsSummary(int total, int matched, int saved)` para mostrar resultados post-ejecución.
- Criterio: Compila sin errores, menú legible en consola
- Dependencias: Ninguna
---
## Fase 3: Program.cs (Entry Point)
### Tarea 6: Reescribir Program.cs como Console App con DI y menú
- Archivos: `src/CareerSentinel/Program.cs`
- Acción: Reescribir completamente. Usar `Microsoft.Extensions.DependencyInjection` (NO Host de Worker). Estructura:
  1. `ServiceCollection` + `ServiceProvider`
  2. Bind `AppSettings` desde `appsettings.json` + User Secrets
  3. Registrar `HttpClient` con Polly (retry + circuit breaker) para "LinkedIn"
  4. Registrar `HttpClient` con Polly para "Ollama"
  5. Registrar `IOptions<AppSettings>`
  6. Registrar todos los services: `ILinkedInScraper` → `LinkedInScraper`, `LocalLlmService`, `NotionService`, `TelegramAlertService`, `IJobCacheService` → `JobCacheService`, `JobOrchestrator`
  7. Loop principal: mostrar menú → ejecutar opción → repetir hasta "Salir"
  8. Opción 1: llamar `JobOrchestrator.RunAsync()` con try/catch
  9. Opciones 2-4: leer/modificar config en runtime
  10. Opción 5: salir con mensaje de despedida
- Criterio: `dotnet build` compila sin errores, DI funciona correctamente
- Dependencias: Tarea 3, Tarea 4, Tarea 5
---
## Fase 4: Configuración y Secrets
### Tarea 7: Configurar User Secrets para valores sensibles
- Archivos: Ninguno (configurado via CLI)
- Acción: Ejecutar comandos para configurar User Secrets:
  ```
  dotnet user-secrets set "Notion:ApiKey" "secret_value"
  dotnet user-secrets set "Notion:DatabaseId" "secret_value"
  dotnet user-secrets set "Telegram:BotToken" "secret_value"
  dotnet user-secrets set "Telegram:ChatId" "secret_value"
  ```
  NOTA: Estos valores serán provistos por el usuario. Crear placeholder.
- Criterio: `dotnet user-secrets list` muestra las claves configuradas
- Dependencias: Tarea 6
### Tarea 8: Crear .gitignore para excluir secrets
- Archivos: `.gitignore` (raíz del proyecto)
- Acción: Crear/actualizar `.gitignore` para excluir: `appsettings.Development.json`, `appsettings.*.json`, `*.user`, `bin/`, `obj/`, `.vs/`, `seen_jobs.json`
- Criterio: Secrets no se commitean accidentalmente
- Dependencias: Ninguna
---
## Fase 5: Build y Verificación Final
### Tarea 9: Build completo y corrección de errores
- Acción: Ejecutar `dotnet build` completo. Corregir cualquier warning o error.
- Criterio: 0 errores, 0 warnings
- Dependencias: Todas las anteriores
### Tarea 10: Verificación de end-to-end (sin servicios externos)
- Acción: Ejecutar `dotnet run` y verificar:
  1. El menú se muestra correctamente
  2. Se puede navegar con las opciones
  3. La opción "Salir" funciona
  4. No hay errores de DI al resolver servicios
  5. Opción "Ver configuración" muestra los valores actuales
- Criterio: La app arranca, muestra menú, y responde a input del usuario
- Dependencias: Tarea 9
---
## Fase 6: Modularidad y Multi-Fuente
- [ ] Tarea 11: Crear `Services/IJobScraper.cs` � Interfaz gen�rica que reemplaza `ILinkedInScraper`. Debe tener: `string PortalName`, `Task<List<JobOffer>> SearchAsync(string keyword, string location, CancellationToken ct)`, `Task<string> GetDescriptionAsync(string jobUrl, CancellationToken ct)`. Eliminar `ILinkedInScraper.cs`.
  - Archivos: `src/CareerSentinel/Services/IJobScraper.cs`, eliminar `src/CareerSentinel/Services/ILinkedInScraper.cs`
  - Criterio: Compila sin errores
  - Dependencias: Ninguna
- [x] Tarea 12: Actualizar `LinkedInScraper.cs` para implementar `IJobScraper` en lugar de `ILinkedInScraper`. Agregar propiedad `PortalName => "LinkedIn"`. Mantener toda la l�gica existente.
  - Archivos: `src/CareerSentinel/Services/LinkedInScraper.cs`
  - Criterio: Compila sin errores
  - Dependencias: Tarea 11
- [x] Tarea 13: Crear `Services/CompuTrabajoScraper.cs` implementando `IJobScraper`. Scraping espec�fico para CompuTrabajo (buscar HTML structure de computrabajo.com). User-Agents, rate limiting, y manejo de errores similar al LinkedInScraper.
  - Archivos: `src/CareerSentinel/Services/CompuTrabajoScraper.cs`
  - Criterio: Compila sin errores
  - Dependencias: Tarea 11
- [x] Tarea 14: Actualizar `Configuration/AppSettings.cs` � Agregar clase `JobSourceSettings` con propiedades: `Name`, `Enabled`, `BaseUrl`, `Keywords` (List<string>), `Location`. Agregar propiedad `Dictionary<string, JobSourceSettings> JobSources` a `AppSettings`.
  - Archivos: `src/CareerSentinel/Configuration/AppSettings.cs`
  - Criterio: Compila sin errores
  - Dependencias: Ninguna
- [ ] Tarea 15: Actualizar `appsettings.json` � Agregar secci�n `JobSources` con LinkedIn (Enabled: true) y CompuTrabajo (Enabled: false por defecto). Cada uno con sus Keywords, Location y BaseUrl.
  - Archivos: `src/CareerSentinel/appsettings.json`
  - Criterio: JSON v�lido, todas las secciones presentes
  - Dependencias: Tarea 14
- [x] Tarea 16: Actualizar `Services/JobOrchestrator.cs` � Recibir `IEnumerable<IJobScraper>` en constructor. Iterar scrapers habilitados seg�n config `JobSources[scraper.PortalName].Enabled`. Para cada scraper habilitado: buscar keywords, filtrar cache, evaluar LLM, guardar Notion, alertar Telegram.
  - Archivos: `src/CareerSentinel/Services/JobOrchestrator.cs`
  - Criterio: Compila sin errores, usa todos los scrapers habilitados
  - Dependencias: Tarea 11, Tarea 12, Tarea 14
- [x] Tarea 17: Actualizar `Program.cs` � Registrar todos los scrapers: `services.AddTransient<IJobScraper, LinkedInScraper>()` y `services.AddTransient<IJobScraper, CompuTrabajoScraper>()`. Agregar `HttpClient` "CompuTrabajo" con Polly.
  - Archivos: `src/CareerSentinel/Program.cs`
  - Criterio: Compila sin errores, DI resuelve IEnumerable<IJobScraper> correctamente
  - Dependencias: Tarea 12, Tarea 13
- [x] Tarea 18: Actualizar menú en `Program.cs` — Agregar opción "[5] Ver fuentes habilitadas" (muestra estado de cada portal) y "[6] Habilitar/deshabilitar fuentes". Salir movido a opción [7].
  - Archivos: `src/CareerSentinel/Program.cs`, `src/CareerSentinel/Services/ConsoleMenu.cs`
  - Criterio: Compila sin errores, menú actualizado con 7 opciones
  - Dependencias: Tarea 17
- [x] Tarea 19: Optimizar prompt de LLM y modelo EvaluationResult — Simplificar el prompt en LocalLlmService.BuildPrompt() para que sea más ligero para Qwen 2.5-3B. Cambiar estructura de respuesta de {score, justification, adapted_cv} a {score, summary, matching_skills}. Actualizar EvaluationResult.cs para reflejar nuevos campos (record con Score, Summary, MatchingSkills). Reducir 
num_predict de 1024 a 256. Prompt optimo: system + CV text + oferta text → JSON response.
  - Archivos: src/CareerSentinel/Services/LocalLlmService.cs, src/CareerSentinel/Models/EvaluationResult.cs
  - Criterio: Compila sin errores, prompt < 500 tokens, respuesta JSON valida
  - Dependencias: Ninguna
- [ ] Tarea 20: Crear Services/AntiBotHttpClientHandler.cs — DelegatingHandler para proteccion anti-baneos. Implementar: (1) Rotacion dinamica de User-Agents reales en cada peticion, (2) Delay estocastico (Random Jitter) configurable antes de enviar request, (3) Soporte opcional de proxy HTTP/HTTPS desde appsettings.json. Inyectar en los HttpClients de los scrapers via IHttpClientFactory.
  - Archivos: src/CareerSentinel/Services/AntiBotHttpClientHandler.cs
  - Criterio: Compila sin errores, rotacion de User-Agent funciona, jitter configurable
  - Dependencias: Tarea 11, Tarea 14
- [x] Tarea 21: Actualizar AppSettings.cs y appsettings.json con configuracion Anti-Baneo — Agregar clase AntiBotSettings con propiedades: MinDelayMs (int), MaxDelayMs (int), EnableUserAgentRotation (bool), Proxy (con Enabled, Address). Agregar propiedad AntiBot a AppSettings. Actualizar appsettings.json con seccion AntiBot por defecto (MinDelayMs: 3000, MaxDelayMs: 7000, rotation habilitado, proxy deshabilitado).
  - Archivos: src/CareerSentinel/Configuration/AppSettings.cs, src/CareerSentinel/appsettings.json
  - Criterio: Compila sin errores, JSON valido, configuracion cargada correctamente
  - Dependencias: Ninguna
---
## Resumen de Tareas (actualizado)
| # | Fase | Tarea | Archivos | Estado |
|---|------|-------|----------|--------|
| 1 | Reestructuración | Convertir .csproj a Console | `CareerSentinel.csproj` | ? |
| 2 | Reestructuración | Eliminar Worker.cs + Workers/ | `Worker.cs` | ? |
| 3 | Reestructuración | Completar appsettings.json | `appsettings.json` | ✅ |
| 4 | Orquestador | Crear JobOrchestrator.cs | `Services/JobOrchestrator.cs` | ✅ |
| 5 | Orquestador | Crear ConsoleMenu.cs | `Services/ConsoleMenu.cs` | ✅ |
| 6 | Entry Point | Reescribir Program.cs | `Program.cs` | ✅ |
| 7 | Secrets | Configurar User Secrets | (CLI) | ✅ |
| 8 | Secrets | Crear .gitignore | `.gitignore` | ✅ |
| 9 | Verificacion | Build completo | --- | ✅ |
| 10 | Verificación | End-to-end test | — | ? |
| 11 | Modularidad | Crear IJobScraper.cs (interfaz genérica) | `Services/IJobScraper.cs` | ? |
| 12 | Modularidad | Actualizar LinkedInScraper → IJobScraper | `Services/LinkedInScraper.cs` | ✅ |
| 13 | Modularidad | Crear CompuTrabajoScraper | `Services/CompuTrabajoScraper.cs` | ✅ |
| 14 | Modularidad | Agregar JobSourceSettings a AppSettings | `Configuration/AppSettings.cs` | ✅ |
| 15 | Modularidad | Agregar sección JobSources a appsettings.json | `appsettings.json` | ? |
| 16 | Modularidad | Actualizar JobOrchestrator con IEnumerable | `Services/JobOrchestrator.cs` | ✅ |
| 17 | Modularidad | Registrar scrapers en DI | `Program.cs` | ✅ |
| 18 | Modularidad | Actualizar menú con fuentes | `Program.cs`, `ConsoleMenu.cs` | ✅ |
| 19 | Optimizacion LLM | Optimizar prompt + EvaluationResult | LocalLlmService.cs, EvaluationResult.cs | ✅ |
| 20 | Anti-Baneos | Crear AntiBotHttpClientHandler | Services/AntiBotHttpClientHandler.cs | ☐ |
| 21 | Anti-Baneos | Agregar config AntiBot a AppSettings | AppSettings.cs, appsettings.json | ✅ |
| 22 | CandidateProfile | Agregar filtrado por perfil candidato | AppSettings.cs, LinkedInScraper.cs, appsettings.json | ✅ |
| 25 | Chain-of-Thought | Crear modelo JobAnalysis | `Models/JobAnalysis.cs` | ☐ |
| 26 | Chain-of-Thought | Agregar AnalyzeJobAsync (Paso 1) | `Services/LocalLlmService.cs` | ✅ |
| 27 | Chain-of-Thought | Modificar EvaluateJobAsync (Paso 2) | `Services/LocalLlmService.cs` | ✅ |
| 28 | Chain-of-Thought | Encadenar Paso 1→2 en Orchestrator | `Services/JobOrchestrator.cs` | ✅ |
| 29 | Chain-of-Thought | Verificar compatibilidad Telegram/Notion | TelegramAlertService, NotionService | ☐ |
| 30 | Chain-of-Thought | Build final y verificación pipeline | (solo ejecución) | ☐ |
| 31 | OpenCodeGo API | Agregar OpenCodeGoSettings a AppSettings | `Configuration/AppSettings.cs` | ✅ |
| 32 | OpenCodeGo API | Agregar config a appsettings.json | `appsettings.json` | ✅ |
| 33 | OpenCodeGo API | Crear BatchEvaluationRequest model | `Models/BatchEvaluationRequest.cs` | ✅ |
| 34 | OpenCodeGo API | Crear BatchEvaluationResponse model | `Models/BatchEvaluationResponse.cs` | ✅ |
| 35 | OpenCodeGo API | Crear OpenCodeGoService (cliente API batch) | `Services/OpenCodeGoService.cs` | ☐ |
| 36 | OpenCodeGo API | Registrar HttpClient + DI en Program.cs | `Program.cs` | ✅ |
| 37 | OpenCodeGo API | Actualizar JobOrchestrator para batch | `Services/JobOrchestrator.cs` | ✅ |
| 38 | OpenCodeGo API | Configurar User Secret para API Key | (CLI) | ☐ |
| 39 | OpenCodeGo API | Simplificar prompts para 42B | `Services/OpenCodeGoService.cs` | ☐ |
| 40 | OpenCodeGo API | Agregar logging detallado métricas | `Services/OpenCodeGoService.cs` | ☐ |
| 41 | OpenCodeGo API | Build final y verificación pipeline batch | (solo ejecución) | ✅ |
- [x] Tarea 10b: Corregir TelegramAlertService - Hacer lazy init de TelegramBotClient para evitar ArgumentException con token placeholder. Cambiar constructor para que no cree el cliente inmediatamente, sino que lo cree en el primer uso (lazy) o valide el token antes de crear.
  - Archivos: src/CareerSentinel/Services/TelegramAlertService.cs
  - Criterio: Compila sin errores, app arranca sin crashear con token placeholder
  - Dependencias: Ninguna
- [x] Tarea 23: Limpiar HTML en LinkedInScraper.GetDescriptionAsync — Eliminar "Show more"/"Show less" y normalizar espacios múltiples con Regex antes de retornar la descripción
  - Archivos: `src/CareerSentinel/Services/LinkedInScraper.cs` (líneas 121-125)
  - Criterio: Compila sin errores, descripción limpia sin artefactos HTML
  - Dependencias: Ninguna
- [x] Tarea 24: ~~Limitar a 5 ofertas por keyword~~ → LÍMITE ELIMINADO en Tarea 42
- [x] Tarea 42: Quitar límite de 5 ofertas por keyword en JobOrchestrator — Eliminar `const int maxOffersPerKeyword = 5`, eliminar bloque if que rompe el loop, actualizar log para quitar referencia a `maxOffersPerKeyword`
  - Archivos: `src/CareerSentinel/Services/JobOrchestrator.cs`
  - Criterio: Compila sin errores, todas las ofertas se procesan sin límite artificial
  - Dependencias: Ninguna
- [x] Tarea 22: Agregar CandidateProfile para filtrado basado en perfil del candidato
  - Archivos: `Configuration/AppSettings.cs`, `Services/LinkedInScraper.cs`, `appsettings.json`
  - Accion: (1) Agregar clase `CandidateProfile` con propiedades Name, Level, YearsExperience, CoreSkills, PreferredModality, PreferredRegions. Agregar propiedad `Candidate` a `AppSettings`. (2) LinkedInScraper: inyectar `_candidateProfile`, usar `f_E` filter segun YearsExperience en SearchAsync, agregar metodo `ShouldEvaluateJob` como pre-filtro antes del LLM que descarte Senior/Lead/Principal. (3) Agregar seccion `Candidate` en appsettings.json con datos del candidato.
  - Criterio: Compila sin errores, CandidateProfile existe, LinkedIn usa f_E=2 para Junior, ShouldEvaluateJob descarta Senior, appsettings.json tiene seccion Candidate
  - Dependencias: Ninguna
---
## Fase 8: Migración de Ollama a OpenCode Go API (42B)

**Contexto:** Migramos de Ollama local (qwen2.5:3b, 3-7B) a OpenCode Go API (mimo-v2.5, 42B). El nuevo modelo puede evaluar 5 ofertas por request (batch), tiene mejor razonamiento, y permite prompts simplificados.

**Alcance:** Solo el **Paso 2** (EvaluateJobAsync) se migra a batch. El Paso 1 (AnalyzeJobAsync) permanece individual usando Ollama local.

**Flujo nuevo:**
```
JobOrchestrator → Paso 1: Ollama (individual) → Paso 2: OpenCodeGo API (batch de 5)
                                                   ↓
                                              Fallback a Ollama si falla
```

- [x] Tarea 31: Agregar `OpenCodeGoSettings` a `AppSettings.cs`
  - Archivos: `src/CareerSentinel/Configuration/AppSettings.cs`
  - Acción:
    1. Crear nueva clase `OpenCodeGoSettings`:
       ```csharp
       public class OpenCodeGoSettings
       {
           public string ApiKey { get; set; } = string.Empty;
           public string BaseUrl { get; set; } = "https://api.opencode.ai";
           public string ModelName { get; set; } = "opencode-go/mimo-v2.5";
           public int BatchSize { get; set; } = 5;
           public int MaxConcurrentRequests { get; set; } = 2;
           public int RequestTimeoutSeconds { get; set; } = 120;
           public bool FallbackToOllama { get; set; } = true;
       }
       ```
    2. Agregar propiedad `OpenCodeGo` a `AppSettings`:
       ```csharp
       public OpenCodeGoSettings OpenCodeGo { get; set; } = new();
       ```
  - Criterio: Compila sin errores, clase existe con todas las propiedades
  - Dependencias: Ninguna

- [x] Tarea 32: Agregar configuración OpenCodeGo a `appsettings.json`
  - Archivos: `src/CareerSentinel/appsettings.json`
  - Acción:
    1. Agregar sección `OpenCodeGo` bajo `AppSettings`:
       ```json
       "OpenCodeGo": {
           "ApiKey": "",
           "BaseUrl": "https://api.opencode.ai",
           "ModelName": "opencode-go/mimo-v2.5",
           "BatchSize": 5,
           "MaxConcurrentRequests": 2,
           "RequestTimeoutSeconds": 120,
           "FallbackToOllama": true
       }
       ```
    2. El `ApiKey` se configurará via User Secrets (no hardcodeado)
  - Criterio: JSON válido, sección presente, sin API key real
  - Dependencias: Tarea 31

- [x] Tarea 33: Crear `Models/BatchEvaluationRequest.cs`
  - Archivos: `src/CareerSentinel/Models/BatchEvaluationRequest.cs`
  - Acción:
    1. Crear record `BatchEvaluationRequest`:
       ```csharp
       using System.Text.Json.Serialization;

       namespace CareerSentinel.Models;

       public record BatchEvaluationRequest
       {
           [JsonPropertyName("candidate")]
           public CandidateInfo Candidate { get; init; } = new();

           [JsonPropertyName("jobs")]
           public List<JobToEvaluate> Jobs { get; init; } = new();
       }

       public record CandidateInfo
       {
           [JsonPropertyName("level")]
           public string Level { get; init; } = string.Empty;

           [JsonPropertyName("years_experience")]
           public int YearsExperience { get; init; }

           [JsonPropertyName("preferred_modality")]
           public string PreferredModality { get; init; } = string.Empty;

           [JsonPropertyName("preferred_regions")]
           public List<string> PreferredRegions { get; init; } = new();

           [JsonPropertyName("core_skills")]
           public List<string> CoreSkills { get; init; } = new();
       }

       public record JobToEvaluate
       {
           [JsonPropertyName("id")]
           public string Id { get; init; } = string.Empty;

           [JsonPropertyName("title")]
           public string Title { get; init; } = string.Empty;

           [JsonPropertyName("company")]
           public string Company { get; init; } = string.Empty;

           [JsonPropertyName("modalidad")]
           public string Modalidad { get; init; } = string.Empty;

           [JsonPropertyName("ubicacion")]
           public string Ubicacion { get; init; } = string.Empty;

           [JsonPropertyName("seniority_requerido")]
           public string SeniorityRequerido { get; init; } = string.Empty;

           [JsonPropertyName("anos_experiencia")]
           public string AnosExperiencia { get; init; } = string.Empty;

           [JsonPropertyName("tecnologias_clave")]
           public List<string> TecnologiasClave { get; init; } = new();
       }
       ```
  - Criterio: Compila sin errores, records serializables con System.Text.Json
  - Dependencias: Ninguna

- [x] Tarea 34: Crear `Models/BatchEvaluationResponse.cs`
  - Archivos: `src/CareerSentinel/Models/BatchEvaluationResponse.cs`
  - Acción:
    1. Crear record `BatchEvaluationResponse`:
       ```csharp
       using System.Text.Json.Serialization;

       namespace CareerSentinel.Models;

       public record BatchEvaluationResponse
       {
           [JsonPropertyName("evaluations")]
           public List<SingleEvaluation> Evaluations { get; init; } = new();
       }

       public record SingleEvaluation
       {
           [JsonPropertyName("id")]
           public string Id { get; init; } = string.Empty;

           [JsonPropertyName("score")]
           public int Score { get; init; }

           [JsonPropertyName("match")]
           public bool Match { get; init; }

           [JsonPropertyName("cumple")]
           public List<string> Cumple { get; init; } = new();

           [JsonPropertyName("no_cumple")]
           public List<string> NoCumple { get; init; } = new();

           [JsonPropertyName("razon")]
           public string Razon { get; init; } = string.Empty;
       }
       ```
    2. Crear método estático para convertir `SingleEvaluation` → `EvaluationResult`:
       ```csharp
       public EvaluationResult ToEvaluationResult()
       {
           return new EvaluationResult
           {
               Score = Score,
               Match = Match,
               Cumple = Cumple,
               NoCumple = NoCumple,
               Razon = Razon
           };
       }
       ```
  - Criterio: Compila sin errores, conversión a EvaluationResult funciona
  - Dependencias: Tarea 33

- [x] Tarea 35a: Crear `Services/ILlmService.cs` — Interfaz común para evaluar ofertas con LLM
  - Archivos: `src/CareerSentinel/Services/ILlmService.cs`
  - Criterio: Compila sin errores, 3 métodos (AnalyzeJobAsync, EvaluateJobAsync, EvaluateBatchAsync)
  - Dependencias: Ninguna

- [x] Tarea 36a: Crear `Services/OpenCodeGoService.cs` — Implementación ILlmService vía API externa
  - Archivos: `src/CareerSentinel/Services/OpenCodeGoService.cs`
  - Criterio: Compila sin errores, implementa ILlmService, batch de 5 funciona
  - Dependencias: Tarea 35a

- [x] Tarea 37a: Refactorizar `LocalLlmService` para implementar `ILlmService`
  - Archivos: `src/CareerSentinel/Services/LocalLlmService.cs`
  - Acción: Agregar `: ILlmService` a la declaración de clase, agregar método `EvaluateBatchAsync` que evalúa ofertas una por una
  - Criterio: Compila sin errores, implementa los 3 métodos de la interfaz
  - Dependencias: Tarea 35a

- [x] Tarea 36: Registrar HttpClient "OpenCodeGo" + OpenCodeGoService en DI (`Program.cs`) + Registrar ILlmService según ProcessingMode
  - Archivos: `src/CareerSentinel/Program.cs`
  - Acción: Registrar HttpClient "OpenCodeGo" con Polly, registrar OpenCodeGoService, leer ProcessingMode de config, registrar ILlmService según modo (LOCAL→LocalLlmService, API→OpenCodeGoService, HYBRID→LocalLlmService)
  - Criterio: Compila sin errores, DI resuelve ILlmService correctamente
  - Dependencias: Tarea 35

- [x] Tarea 37: Actualizar `JobOrchestrator.cs` para usar `ILlmService` en vez de `LocalLlmService`
  - Archivos: `src/CareerSentinel/Services/JobOrchestrator.cs`
  - Acción: Cambiar campo `LocalLlmService _localLlmService` → `ILlmService _llmService`. Cambiar constructor parameter. Cambiar llamadas AnalyzeJobAsync y EvaluateJobAsync.
  - Criterio: Compila sin errores, usa ILlmService
  - Dependencias: Tarea 35a

- [ ] Tarea 35: Crear `Services/OpenCodeGoService.cs` — Cliente API batch
  - Archivos: `src/CareerSentinel/Services/OpenCodeGoService.cs`
  - Acción:
    1. Crear clase `OpenCodeGoService` con inyección de: `IHttpClientFactory`, `IOptions<AppSettings>`, `ILogger<OpenCodeGoService>`, `LocalLlmService` (para fallback)
    2. Crear HttpClient名为 "OpenCodeGo" con timeout configurable
    3. Implementar `SendBatchAsync(List<(JobAnalysis Analysis, string JobId, string JobTitle)> jobs, CandidateProfile candidate, CancellationToken ct)`:
       - Construir `BatchEvaluationRequest` desde las ofertas y el candidato
       - Serializar a JSON con snake_case
       - Enviar POST a `{BaseUrl}/v1/chat/completions` con header `Authorization: Bearer {ApiKey}`
       - Formato OpenAI-compatible: `{ model, messages: [{role:"user", content: prompt}], response_format: {type:"json_object"} }`
       - Parsear `BatchEvaluationResponse` del response
       - Logging detallado de request/response
    4. Implementar `BuildBatchPrompt(List<JobToEvaluate> jobs, CandidateInfo candidate)` → string:
       - Prompt simplificado para 42B: lista las ofertas numeradas, pide JSON array con evaluaciones
       - Reglas R1-R5 incluidas pero más compactas
    5. Implementar fallback: si falla,逐个 llamar a `LocalLlmService.EvaluateJobAsync()` para cada oferta
  - Criterio: Compila sin errores, batch de 5 funciona, fallback funciona
  - Dependencias: Tarea 31, Tarea 33, Tarea 34

- [ ] Tarea 36: Agregar Polly retry + circuit breaker al HttpClient "OpenCodeGo" en `Program.cs`
  - Archivos: `src/CareerSentinel/Program.cs`
  - Acción:
    1. Registrar HttpClient "OpenCodeGo" con policies Polly:
       ```csharp
       services.AddHttpClient("OpenCodeGo", client =>
       {
           client.Timeout = TimeSpan.FromSeconds(120);
       })
           .AddPolicyHandler(retryPolicy)
           .AddPolicyHandler(circuitBreakerPolicy);
       ```
    2. Registrar `OpenCodeGoService` como Singleton
  - Criterio: Compila sin errores, DI resuelve OpenCodeGoService
  - Dependencias: Tarea 35

- [ ] Tarea 37: Actualizar `JobOrchestrator.cs` para usar batch en Paso 2
  - Archivos: `src/CareerSentinel/Services/JobOrchestrator.cs`
  - Acción:
    1. Agregar `OpenCodeGoService` al constructor
    2. Modificar el flujo del Paso 2 para acumular ofertas y enviar en batch:
       - Después del Paso 1 (AnalyzeJobAsync), agregar `JobAnalysis` a una `List<(JobOffer Offer, JobAnalysis Analysis)>`
       - Cuando la lista tenga 5 ofertas O no haya más ofertas, enviar batch vía `OpenCodeGoService.SendBatchAsync()`
       - Procesar respuestas: para cada `SingleEvaluation`, converter a `EvaluationResult` y continuar con lógica existente (Notion, Telegram)
    3. Mantener fallback: si `OpenCodeGoSettings.FallbackToOllama == true`, usar Ollama local en su lugar
    4. Rate limiting entre batches: `await Task.Delay(_settings.RateLimiting.DelayBetweenRequestsMs, ct)`
    5. Logging: log cuando se envía batch, log de cada evaluación individual
  - Criterio: Compila sin errores, batch de 5 se procesa correctamente, fallback funciona
  - Dependencias: Tarea 35, Tarea 36

- [ ] Tarea 38: Configurar User Secret para API Key
  - Archivos: Ninguno (CLI)
  - Acción:
    1. Ejecutar: `dotnet user-secrets set "AppSettings:OpenCodeGo:ApiKey" "tu_api_key_aqui"`
    2. Verificar que `appsettings.json` NO tiene la key (solo placeholder vacío)
  - Criterio: API key configurada en User Secrets, no en appsettings.json
  - Dependencias: Tarea 32

- [ ] Tarea 39: Simplificar prompts para modelo 42B
  - Archivos: `src/CareerSentinel/Services/OpenCodeGoService.cs`
  - Acción:
    1. El prompt batch debe ser significativamente más corto que el prompt individual de Ollama
    2. Incluir solo las reglas esenciales R1-R5 (sin ejemplos detallados)
    3. Formato de respuesta esperado: JSON array con 5 objetos
    4. Ejemplo mínimo:
       ```
       Evalúa estas 5 ofertas contra el candidato. Responde JSON array:
       [{"id":"...","score":0|10|25|30|85,"match":bool,"cumple":[...],"no_cumple":[...],"razon":"R1-R5"}]
       ```
    5. Reducir tokens: objetivo < 1000 tokens de prompt (vs ~500 actuales por oferta)
  - Criterio: Prompt es ~50% más corto que el prompt individual de Ollama, respuestas JSON válidas
  - Dependencias: Tarea 35

- [ ] Tarea 40: Agregar logging detallado de métricas de batch
  - Archivos: `src/CareerSentinel/Services/OpenCodeGoService.cs`
  - Acción:
    1. Log antes del request: "Enviando batch de {count} ofertas a OpenCodeGo API"
    2. Log del tiempo de respuesta: "Respuesta recibida en {elapsed}ms"
    3. Log de cada evaluación: "Oferta {id}: score={score}, match={match}"
    4. Log de fallback activado: "Fallback a Ollama activado para oferta {id}"
    5. Log de errores: "Error en batch request: {error}, fallback={fallbackEnabled}"
    6. Log de métricas acumuladas: "Batch completado: {success}/{total} exitosas, {time}ms total"
  - Criterio: Todos los logs son informativos y útiles para debugging
  - Dependencias: Tarea 35

- [x] Tarea 41: Build final y verificación del pipeline batch
  - Archivos: Ninguno (solo ejecución)
  - Acción:
    1. Ejecutar `dotnet build` — 0 errores, 0 warnings
    2. Verificar que el flujo en logs es: Paso 1 (Ollama individual) → Batch de 5 → Paso 2 (OpenCodeGo batch) → Notion/Telegram
    3. Verificar fallback: si OpenCodeGo falla, usar Ollama local
  - Criterio: `dotnet build` 0 errores/0 warnings, pipeline batch funciona end-to-end
  - Dependencias: Todas las anteriores
---
## Fase 7: Pipeline Chain-of-Thought (Evaluación en 2 pasos)

**Problema:** El LLM local (Ollama + qwen2.5:3b) no logra evaluar ofertas correctamente en una sola llamada. Se distrae con contexto largo (CV + oferta) y da scores por defecto (75/100) sin razonar.

**Solución:** Dividir la evaluación en 2 llamadas LLM independientes:
- **Paso 1 - Extracción:** El LLM extrae datos estructurados de la oferta (sin juzgar). Prompt corto, enfocado solo en la descripción.
- **Paso 2 - Evaluación:** El LLM compara el CV del candidato con los datos extraídos del Paso 1 y asigna score. Prompt corto, contexto reducido.

**Compatibilidad:** `EvaluationResult` NO cambia. Telegram y Notion consumen el mismo DTO. Solo cambia la forma en que se genera.

- [x] Tarea 25: Crear modelo `JobAnalysis` (resultado del Paso 1 — extracción estructurada)
  - Archivos: `src/CareerSentinel/Models/JobAnalysis.cs`
  - Acción: Record con campos de extracción (es_texto_valido, titulo, empresa, modalidad, ubicacion, seniority_requerido, anos_experiencia, tecnologias_clave, resumen, responsabilidades, requisitos_deseados, descripcion_original). Campo `DescripcionOriginal` agregado para pasar la descripción completa al Paso 2 como red de seguridad.
  - Criterio: Compila sin errores, es record serializable con System.Text.Json
  - Dependencias: Ninguna

- [x] Tarea 26: Agregar método `AnalyzeJobAsync` + `BuildAnalysisPrompt` a `LocalLlmService` (Paso 1 — extracción)
  - Archivos: `src/CareerSentinel/Services/LocalLlmService.cs`
  - Acción:
    1. Método público `AnalyzeJobAsync(string jobTitle, string jobDescription, CancellationToken ct)` → `Task<JobAnalysis?>`
    2. `BuildAnalysisPrompt` pide datos COMPLETOS y DETALLADOS (no resumen de 1 línea). Incluye reglas de extracción para modalidad, ubicación, seniority, años, tecnologías, resumen 2-3 oraciones, responsabilidades, requisitos. Incluye ejemplo few-shot.
    3. `ParseJobAnalysis` con parseo doble (JSON directo + regex fallback)
    4. Después de parsear, guarda `DescripcionOriginal` en el resultado para Paso 2
    5. Logging detallado de cada paso
  - Criterio: Compila sin errores, prompt pide datos completos (no resumen 1 línea), DescripcionOriginal guardado
  - Dependencias: Tarea 25

- [x] Tarea 27: Modificar `EvaluateJobAsync` para recibir JSON del Paso 1 en lugar de descripción cruda
  - Archivos: `src/CareerSentinel/Services/LocalLlmService.cs`
  - Acción:
    1. Cambiar la firma de `EvaluateJobAsync` de:
       ```csharp
       public async Task<EvaluationResult?> EvaluateJobAsync(string jobTitle, string jobDescription, string myCv, CancellationToken ct)
       ```
       a:
       ```csharp
       public async Task<EvaluationResult?> EvaluateJobAsync(string jobTitle, JobAnalysis analysis, string myCv, CancellationToken ct)
       ```
    2. Reescribir `BuildPrompt` para que ahora reciba `JobAnalysis` (ya parseado) en lugar de `string jobDescription`. Convertir el `JobAnalysis` a un JSON string legible para el prompt. Ejemplo de prompt reescrito:
       ```
       Eres un reclutador técnico. Compara el PERFIL DEL CANDIDATO con los datos extraídos de la OFERTA y asigna un score de compatibilidad.

       PERFIL DEL CANDIDATO:
       {myCv}

       DATOS EXTRAÍADOS DE LA OFERTA:
       Título: {analysis.TituloOferta}
       Empresa: {analysis.Empresa}
       Ubicación: {analysis.Ubicacion}
       Modalidad: {analysis.Modalidad}
       Stack: {string.Join(", ", analysis.StackTecnologico)}
       Seniority: {analysis.SeniorityRequerido}
       Experiencia: {analysis.AnosExperiencia} años
       Requisitos blandos: {string.Join(", ", analysis.RequisitosBlandos)}
       Salario: {analysis.SalarioMencionado}

       REGLAS DE CALIBRACIÓN:
       - Si oferta inválida (es_oferta_valida=false) → score: 0, match: false
       - Si 100% Remota → sede no es problema
       - Si Presencial/Híbrida fuera de Colombia → score: 10-20, match: false
       - Si stack no coincide → score: 25-35, match: false
       - Si Seniority muy alto → score: 20-30, match: false
       - Si stack coincide y nivel adecuado → score: 65-90, match: true

       Responde ÚNICAMENTE en formato JSON:
       { "score": <0-100>, "match": <true|false>, "cumple": [...], "no_cumple": [...], "razon": "..." }
       ```
    3. Reducir `num_ctx` de 4096 a 2048 en el requestBody del Paso 2 (el contexto ahora es mucho más corto).
    4. Actualizar el método `LogEvaluationToFile` para que acepte un parámetro adicional `string? step1Json` (el JSON del Paso 1) y lo muestre en el log bajo un separador `PASO 1 - EXTRACCIÓN:`.
  - Criterio: Compila sin errores, el prompt del Paso 2 es significativamente más corto que el original, `num_ctx` reducido
  - Dependencias: Tarea 25, Tarea 26
  - **Nota:** Se completó una variante mejorada: en vez de `string myCv`, se usa `CandidateProfile candidate` con prompt 4-paso obligatorio (normalizar → listar techs → verificar cada una → aplicar escalera). Few-shot incluyen R3 (Senior→25) y R4 (tech mismatch→30). num_ctx=4096, num_predict=700.

- [x] Tarea 28: Actualizar `JobOrchestrator` para encadenar Paso 1 → Paso 2
  - Archivos: `src/CareerSentinel/Services/JobOrchestrator.cs`
  - Acción: Reemplazar la llamada actual (líneas 164-172):
    ```csharp
    // Rate limit before LLM call
    await Task.Delay(_settings.RateLimiting.DelayBetweenRequestsMs, ct);

    // Evaluate with local LLM
    var evaluation = await _localLlmService.EvaluateJobAsync(
        offer.Title,
        description,
        _settings.Scoring.CvText,
        ct);
    ```
    Por el encadenamiento de 2 pasos:
    ```csharp
    // Rate limit before LLM call (Step 1)
    await Task.Delay(_settings.RateLimiting.DelayBetweenRequestsMs, ct);

    // Step 1: Extract structured data from job description
    var analysis = await _localLlmService.AnalyzeJobAsync(
        offer.Title,
        description,
        ct);

    if (analysis is null)
    {
        _logger.LogWarning(
            "[{PortalName}] El LLM no pudo analizar la oferta (Paso 1): {Title}",
            scraper.PortalName, offer.Title);
        await _jobCacheService.AddSeenIdAsync(offer.Id, ct);
        continue;
    }

    _logger.LogInformation(
        "[{PortalName}] Paso 1 completado: {Title} - Válida: {IsValid}, Stack: {Stack}, Seniority: {Seniority}",
        scraper.PortalName, offer.Title, analysis.EsOfertaValida,
        string.Join(", ", analysis.StackTecnologico), analysis.SeniorityRequerido);

    if (!analysis.EsOfertaValida)
    {
        _logger.LogInformation(
            "[{PortalName}] Oferta marcada como inválida en Paso 1, saltando evaluación: {Title}",
            scraper.PortalName, offer.Title);
        await _jobCacheService.AddSeenIdAsync(offer.Id, ct);
        continue;
    }

    // Rate limit between LLM calls
    await Task.Delay(_settings.RateLimiting.DelayBetweenRequestsMs, ct);

    // Step 2: Evaluate compatibility against extracted data
    var evaluation = await _localLlmService.EvaluateJobAsync(
        offer.Title,
        analysis,
        _settings.Scoring.CvText,
        ct);
    ```
    El resto del método (deduplicación, score threshold, Notion, Telegram) permanece sin cambios.
  - Criterio: Compila sin errores, flujo visible en logs: Paso 1 → validación → Paso 2 → score
  - Dependencias: Tarea 26, Tarea 27
  - **Nota:** Se completó variante mejorada: EvaluateJobAsync ahora recibe `CandidateProfile candidate` en vez de `string myCv`. JobOrchestrator pasa `_settings.Candidate`.

- [ ] Tarea 29: Verificar compatibilidad con Telegram y Notion (sin cambios necesarios)
  - Archivos: `src/CareerSentinel/Services/TelegramAlertService.cs`, `src/CareerSentinel/Services/NotionService.cs`
  - Acción:
    1. Verificar que `TelegramAlertService.SendAlertAsync(JobOffer, EvaluationResult, ct)` NO requiere cambios — el DTO `EvaluationResult` no cambió.
    2. Verificar que `NotionService.SaveJobAsync(JobOffer, EvaluationResult, ct)` NO requiere cambios — consume el mismo DTO.
    3. Verificar que `SendDailySummaryAsync(List<(JobOffer, EvaluationResult)>)` funciona igual.
    4. Si algún servicio necesita ajuste por datos adicionales del `JobAnalysis` (por ejemplo, guardar el stack extraído en Notion), agregar un campo opcional `JobAnalysis?` al parámetro. **Pero solo si el usuario lo pide explícitamente** — por defecto NO se tocan estos archivos.
    5. Ejecutar `dotnet build` para confirmar que no hay breaks.
  - Criterio: Compila sin errores, Telegram y Notion consumen el mismo `EvaluationResult` sin cambios
  - Dependencias: Tarea 27, Tarea 28

- [ ] Tarea 30: Build final y verificación del pipeline completo
  - Archivos: Ninguno (solo ejecución)
  - Acción:
    1. Ejecutar `dotnet build` — 0 errores, 0 warnings.
    2. Verificar en logs que el flujo es: `AnalyzeJobAsync` → log Paso 1 → `EvaluateJobAsync` → log Paso 2 → resultado final.
    3. Verificar que el archivo `logs/evaluaciones.log` muestra ambos pasos (Paso 1: JSON de extracción + Paso 2: JSON de evaluación).
  - Criterio: `dotnet build` 0 errores/0 warnings, pipeline 2 pasos funciona end-to-end en logs
  - Dependencias: Todas las anteriores
---
## Fix 5-6: ProcessingMode path + API key hardcodeado (2026-08-17)

- [x] Fix 5: Corregir ProcessingMode path en Program.cs — Cambiar `configuration.GetValue<string>("ProcessingMode")` → `configuration.GetValue<string>("AppSettings:ProcessingMode")` para que lea correctamente de la sección AppSettings
  - Archivos: `src/CareerSentinel/Program.cs`
  - Criterio: Compila sin errores, ProcessingMode se lee de AppSettings:ProcessingMode
- [x] Fix 6: Eliminar API key hardcodeado de appsettings.json — Reemplazar `"ApiKey": "sk-6Rbud..."` con `"ApiKey": ""`. El API key real ya existe en User Secrets (`AppSettings:OpenCodeGo:ApiKey`)
  - Archivos: `src/CareerSentinel/appsettings.json`
  - Criterio: API key real eliminada de appsettings.json, placeholder vacío presente

## Fix 1-4: HybridLlmService + OpenCodeGo limpieza (2026-08-17)

- [x] Fix 1: Crear `Services/HybridLlmService.cs` — Implementa `ILlmService`, delega Paso1 a `LocalLlmService` (Ollama) y Paso2+Batch a `OpenCodeGoService` (API)
  - Archivos: `src/CareerSentinel/Services/HybridLlmService.cs` (nuevo)
  - Criterio: Compila sin errores, delegación correcta
- [x] Fix 2: Actualizar DI en `Program.cs` — Caso HYBRID ahora resuelve `HybridLlmService`. Agregar registro `services.AddSingleton<HybridLlmService>()`
  - Archivos: `src/CareerSentinel/Program.cs`
  - Criterio: Compila sin errores, DI resuelve ILlmService=HybridLlmService cuando ProcessingMode=HYBRID
- [x] Fix 3: Eliminar código muerto en `OpenCodeGoService.cs` — Eliminar campo `OllamaJsonOptions` no utilizado
  - Archivos: `src/CareerSentinel/Services/OpenCodeGoService.cs`
  - Criterio: Compila sin errores, campo eliminado
- [x] Fix 4: Usar `ScoringSettings.MaxRetries` en `OpenCodeGoService.cs` — Reemplazar valor hardcodeado `3` por `_scoringSettings.MaxRetries`. Agregar campo `_scoringSettings`. Logging actualizado para mostrar MaxRetries.
  - Archivos: `src/CareerSentinel/Services/OpenCodeGoService.cs`
  - Criterio: Compila sin errores, retry configurable desde config

## Fix 7: Migración OpenCodeGo → Google Gemini (2026-08-17)

- [x] Fix 7: Migrar de OpenCodeGo API a Google Gemini (gratis) — Cambiar BaseUrl a `https://generativelanguage.googleapis.com/v1beta/openai/`, ModelName a `gemini-2.5-flash`, agregar `response_format: {type: "json_object"}` para forzar respuesta JSON válida, actualizar system prompt para indicar "sin markdown".
  - Archivos: `src/CareerSentinel/appsettings.json`, `src/CareerSentinel/Services/OpenCodeGoService.cs`
  - Criterio: Compila sin errores, BaseUrl apunta a Gemini, ModelName es gemini-2.5-flash, response_format forzado a json_object
  - Nota: El usuario debe configurar la API key de Gemini via User Secrets: `dotnet user-secrets set "AppSettings:OpenCodeGo:ApiKey" "SU_API_KEY" --project "D:\CareerSentinel\src\CareerSentinel"`

---

## Fase 9: Batch Completo Paso 1 + Paso 2 (Optimización de Tokens)

**Problema actual:** Cada oferta genera 2 llamadas API individuales (Paso 1 análisis + Paso 2 evaluación). Con lotes de 5, el Paso 2 ya es batch pero el Paso 1 no. El usuario quiere batch en AMBOS pasos.

**Objetivo:** Paso 1 batch (AnalyzeBatchAsync) + fix wrappers `{"analyses":[...]}` y `{"evaluations":[...]}` para `json_object` + parsing por `indice` + subir max_tokens a ~3500 en batch.

**Flujo nuevo:**
```
Scraping individual → Acumular 5 ofertas → AnalyzeBatchAsync (1 llamada API) → EvaluateBatchAsync (1 llamada API) → Notion/Telegram
```

---

### Tarea 50: Agregar campo `Indice` a `JobAnalysis`
- Archivos: `src/CareerSentinel/Models/JobAnalysis.cs`
- Acción: Agregar al record `JobAnalysis`:
  ```csharp
  [JsonPropertyName("indice")]
  public int Indice { get; init; }
  ```
  Este campo permite identificar cada análisis en respuestas batch. En modo individual queda en 0.
- Criterio: Compila sin errores
- Dependencias: Ninguna

### Tarea 51: Agregar `MaxTokensBatch` a `OpenCodeGoSettings`
- Archivos: `src/CareerSentinel/Configuration/AppSettings.cs`, `src/CareerSentinel/appsettings.json`
- Acción:
  1. En `AppSettings.cs`, agregar a `OpenCodeGoSettings`:
     ```csharp
     public int MaxTokensBatch { get; set; } = 3500;
     ```
  2. En `appsettings.json`, agregar dentro de la sección `OpenCodeGo`:
     ```json
     "MaxTokensBatch": 3500
     ```
- Criterio: Compila sin errores, JSON válido
- Dependencias: Ninguna

### Tarea 52: Agregar campo `Indice` a `OfferToEvaluate`
- Archivos: `src/CareerSentinel/Models/BatchEvaluationRequest.cs`
- Acción: Agregar al record `OfferToEvaluate`:
  ```csharp
  [JsonPropertyName("indice")]
  public int Indice { get; init; }
  ```
  Permite identificar cada oferta en respuestas batch de evaluación.
- Criterio: Compila sin errores
- Dependencias: Ninguna

### Tarea 53: Agregar `AnalyzeBatchAsync` a `ILlmService`
- Archivos: `src/CareerSentinel/Services/ILlmService.cs`
- Acción: Agregar nuevo método a la interfaz:
  ```csharp
  Task<List<JobAnalysis?>> AnalyzeBatchAsync(
      List<(string Title, string Description)> offers,
      CancellationToken ct = default);
  ```
  Recibe lista de (título, descripción) y devolverá lista de `JobAnalysis?` en el mismo orden.
- Criterio: Compila sin errores
- Dependencias: Tarea 50 (para que `JobAnalysis` tenga `Indice`)

### Tarea 54: Implementar `AnalyzeBatchAsync` en `OpenCodeGoService`
- Archivos: `src/CareerSentinel/Services/OpenCodeGoService.cs`
- Estado: ✅ Completado
- Implementado: `AnalyzeBatchAsync`, `BuildAnalysisBatchPrompt`, `ParseAnalysisBatchResponse`, `CallOpenAiApiAsync` con `isBatch`

### Tarea 55: Implementar `AnalyzeBatchAsync` en `LocalLlmService` (fallback)
- Archivos: `src/CareerSentinel/Services/LocalLlmService.cs`
- Estado: ✅ Completado
- Implementado: Fallback one-by-one iterando `AnalyzeJobAsync` por cada oferta

### Tarea 56: Implementar `AnalyzeBatchAsync` en `HybridLlmService`
- Archivos: `src/CareerSentinel/Services/HybridLlmService.cs`
- Estado: ✅ Completado
- Implementado: Delega a `_localService.AnalyzeBatchAsync`

### Tarea 57: Fix wrapper `{"evaluations":[...]}` + parsing por `indice` en `OpenCodeGoService`
- Archivos: `src/CareerSentinel/Services/OpenCodeGoService.cs`
- Estado: ✅ Completado
- Implementado: `BuildBatchEvaluationPrompt` usa wrapper `{"evaluations":[...]}`, `ParseBatchResponse` parsea wrapper con fallback regex, `EvaluateBatchAsync` pasa `isBatch: true`

### Tarea 58: Actualizar `JobOrchestrator` para batch Paso 1
- Archivos: `src/CareerSentinel/Services/JobOrchestrator.cs`
- Estado: ✅ Completado
- Implementado: Reemplazado el loop individual de Paso 1+2 por pipeline batch completo:
  1. **Fase de recolección**: Scraping individual con dedup + guardrails (descripción ≥150 chars), acumula en `pendingBatch`
  2. **Procesamiento en lotes de 5**: `AnalyzeBatchAsync` (1 llamada API) → filtrar válidos → `EvaluateBatchAsync` (1 llamada API) → Notion/Telegram
  3. Logging claro: `[Paso1 batch X/Y]` y `[Paso2 batch X/Y]`
  4. Eliminado `processedCount` y llamadas individuales a `AnalyzeJobAsync`
  5. Todos los guardrails preservados: seen jobs, Notion dedup, descripción ≥150 chars
- Dependencias: Tarea 53, Tarea 54, Tarea 55

### Tarea 59: Marcar métodos single como `[Obsolete]`
- Archivos: `src/CareerSentinel/Services/ILlmService.cs`, `src/CareerSentinel/Services/OpenCodeGoService.cs`, `src/CareerSentinel/Services/LocalLlmService.cs`, `src/CareerSentinel/Services/HybridLlmService.cs`
- Acción: Agregar atributo `[Obsolete]` a los métodos individuales:
  ```csharp
  [Obsolete("Usar AnalyzeBatchAsync/EvaluateBatchAsync para mejor eficiencia de tokens")]
  Task<JobAnalysis?> AnalyzeJobAsync(string jobTitle, string jobDescription, CancellationToken ct = default);
  
  [Obsolete("Usar EvaluateBatchAsync para mejor eficiencia de tokens")]
  Task<EvaluationResult?> EvaluateJobAsync(string jobTitle, JobAnalysis analysis, CandidateProfile candidate, CancellationToken ct = default);
  ```
  En `ILlmService.cs` y en las 3 implementaciones. Los métodos se mantienen para compatibilidad pero marcan warnings.
- Criterio: Compila sin errores, warnings de `[Obsolete]` visibles
- Dependencias: Tarea 53

### Tarea 60: Build final verificación batch
- Archivos: Ninguno (solo ejecución)
- Acción:
  1. Ejecutar `dotnet build` — 0 errores, 0 warnings de error (los warnings de `[Obsolete]` son esperados)
  2. Verificar en logs que el flujo es:
     - Scraping individual → Acumular 5 → AnalyzeBatchAsync (1 llamada) → EvaluateBatchAsync (1 llamada) → Notion/Telegram
  3. Verificar que los wrappers `{"analyses":[...]}` y `{"evaluations":[...]}` están bien parseados
- Criterio: `dotnet build` 0 errores, pipeline batch completo funciona
- Dependencias: Todas las anteriores

---
## Resumen de Tareas Fase 9 (Batch Completo)
| # | Tarea | Archivos | Estado |
|---|-------|----------|--------|
| 50 | Agregar `Indice` a `JobAnalysis` | `Models/JobAnalysis.cs` | ☐ |
| 51 | Agregar `MaxTokensBatch` a config | `AppSettings.cs`, `appsettings.json` | ☐ |
| 52 | Agregar `Indice` a `OfferToEvaluate` | `Models/BatchEvaluationRequest.cs` | ☐ |
| 53 | Agregar `AnalyzeBatchAsync` a `ILlmService` | `Services/ILlmService.cs` | ☐ |
| 54 | Implementar `AnalyzeBatchAsync` en `OpenCodeGoService` | `Services/OpenCodeGoService.cs` | ☐ |
| 55 | Implementar `AnalyzeBatchAsync` en `LocalLlmService` | `Services/LocalLlmService.cs` | ☐ |
| 56 | Implementar `AnalyzeBatchAsync` en `HybridLlmService` | `Services/HybridLlmService.cs` | ☐ |
| 57 | Fix wrapper `{"evaluations":[...]}` + parsing por indice | `Services/OpenCodeGoService.cs` | ☐ |
| 58 | Actualizar `JobOrchestrator` batch Paso 1 | `Services/JobOrchestrator.cs` | ✅ |
| 59 | Marcar métodos single `[Obsolete]` | `ILlmService.cs` + 3 impl. | ☐ |
| 60 | Build final verificación batch | (solo ejecución) | ☐ |
