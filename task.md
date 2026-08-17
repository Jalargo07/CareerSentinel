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
| 1 | Reestructuraci�n | Convertir .csproj a Console | `CareerSentinel.csproj` | ? |
| 2 | Reestructuraci�n | Eliminar Worker.cs + Workers/ | `Worker.cs` | ? |
| 3 | Reestructuraci�n | Completar appsettings.json | `appsettings.json` | ✅ |
| 4 | Orquestador | Crear JobOrchestrator.cs | `Services/JobOrchestrator.cs` | ✅ |
| 5 | Orquestador | Crear ConsoleMenu.cs | `Services/ConsoleMenu.cs` | ✅ |
| 6 | Entry Point | Reescribir Program.cs | `Program.cs` | ✅ |
| 7 | Secrets | Configurar User Secrets | (CLI) | ✅ |
| 8 | Secrets | Crear .gitignore | `.gitignore` | ✅ |
| 9 | Verificacion | Build completo | --- | ✅ |
| 10 | Verificaci�n | End-to-end test | � | ? |
| 11 | Modularidad | Crear IJobScraper.cs (interfaz gen�rica) | `Services/IJobScraper.cs` | ? |
| 12 | Modularidad | Actualizar LinkedInScraper ? IJobScraper | `Services/LinkedInScraper.cs` | ✅ |
| 13 | Modularidad | Crear CompuTrabajoScraper | `Services/CompuTrabajoScraper.cs` | ✅ |
| 14 | Modularidad | Agregar JobSourceSettings a AppSettings | `Configuration/AppSettings.cs` | ✅ |
| 15 | Modularidad | Agregar secci�n JobSources a appsettings.json | `appsettings.json` | ? |
| 16 | Modularidad | Actualizar JobOrchestrator con IEnumerable | `Services/JobOrchestrator.cs` | ✅ |
| 17 | Modularidad | Registrar scrapers en DI | `Program.cs` | ✅ |
| 18 | Modularidad | Actualizar menú con fuentes | `Program.cs`, `ConsoleMenu.cs` | ✅ |
| 19 | Optimizacion LLM | Optimizar prompt + EvaluationResult | LocalLlmService.cs, EvaluationResult.cs | ✅ |
| 20 | Anti-Baneos | Crear AntiBotHttpClientHandler | Services/AntiBotHttpClientHandler.cs | ☐ |
| 21 | Anti-Baneos | Agregar config AntiBot a AppSettings | AppSettings.cs, appsettings.json | ✅ |
- [x] Tarea 10b: Corregir TelegramAlertService - Hacer lazy init de TelegramBotClient para evitar ArgumentException con token placeholder. Cambiar constructor para que no cree el cliente inmediatamente, sino que lo cree en el primer uso (lazy) o valide el token antes de crear.
  - Archivos: src/CareerSentinel/Services/TelegramAlertService.cs
  - Criterio: Compila sin errores, app arranca sin crashear con token placeholder
  - Dependencias: Ninguna
