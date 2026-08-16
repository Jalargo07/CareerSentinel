# Plan de Desarrollo - CareerSentinel

## Estado Actual

**Ya existe y estÃ¡ completo (NO tocar):**
- `Configuration/AppSettings.cs` â€” Records de configuraciÃ³n
- `Models/JobOffer.cs` â€” DTO de oferta
- `Models/EvaluationResult.cs` â€” DTO de evaluaciÃ³n LLM
- `Services/ILinkedInScraper.cs` + `LinkedInScraper.cs` â€” Scraping LinkedIn
- `Services/LocalLlmService.cs` â€” EvaluaciÃ³n Ollama
- `Services/NotionService.cs` â€” Persistencia Notion
- `Services/TelegramAlertService.cs` â€” Alertas Telegram
- `Services/IJobCacheService.cs` + `JobCacheService.cs` â€” Cache local

**Necesita reestructuraciÃ³n/completado:**
- `CareerSentinel.csproj` â€” Cambiar de Worker SDK a Console SDK
- `Program.cs` â€” Reescribir como console app con DI + menÃº
- `Worker.cs` â€” Eliminar (es skeleton vacÃ­o)
- `Workers/` â€” Eliminar directorio
- `appsettings.json` â€” Completar con todas las secciones de config

---

## Fase 1: ReestructuraciÃ³n del Proyecto

### Tarea 1: Convertir .csproj de Worker a Console App
- Archivos: `src/CareerSentinel/CareerSentinel.csproj`
- AcciÃ³n: Cambiar `Sdk="Microsoft.NET.Sdk.Worker"` â†’ `Sdk="Microsoft.NET.Sdk"`. Agregar `<OutputType>Exe</OutputType>`. Mantener todos los PackageReference existentes. Eliminar referencia a `Microsoft.Extensions.Hosting` si se reemplaza por DI manual, O mantenerlo si se usa `Microsoft.Extensions.DependencyInjection` + `IHost` ligero.
- Criterio: `dotnet build` compila sin errores
- Dependencias: Ninguna

### Tarea 2: Eliminar archivos obsoletos
- Archivos: `src/CareerSentinel/Worker.cs`, `src/CareerSentinel/Workers/` (directorio)
- AcciÃ³n: Eliminar `Worker.cs` (es el skeleton por defecto del template). Eliminar directorio `Workers/` si estÃ¡ vacÃ­o.
- Criterio: `dotnet build` compila sin errores (sin referencias rotas)
- Dependencias: Tarea 1

### Tarea 3: Completar appsettings.json con todas las secciones
- Archivos: `src/CareerSentinel/appsettings.json`
- AcciÃ³n: Agregar las secciones: `Ollama` (BaseUrl, ModelName), `LinkedIn` (BaseUrl, Location, Keywords), `Notion` (ApiKey, DatabaseId), `Telegram` (BotToken, ChatId), `Scoring` (Threshold, MaxRetries, CvText), `RateLimiting` (DelayBetweenRequestsMs, DelayBetweenSearchesMs). Usar valores placeholder para secrets.
- Criterio: JSON vÃ¡lido, todas las secciones presentes, sin secrets reales
- Dependencias: Ninguna

---

## Fase 2: Orquestador Principal

### Tarea 4: Crear Services/JobOrchestrator.cs (clase normal, NO BackgroundService)
- Archivos: `src/CareerSentinel/Services/JobOrchestrator.cs`
- AcciÃ³n: Crear clase `JobOrchestrator` que recibe por DI: `ILinkedInScraper`, `LocalLlmService`, `NotionService`, `TelegramAlertService`, `IJobCacheService`, `IOptions<AppSettings>`, `ILogger<JobOrchestrator>`. MÃ©todo pÃºblico `RunAsync(CancellationToken ct)` que ejecuta el flujo completo:
  1. Itera por cada keyword de config
  2. Scrapea LinkedIn para cada keyword
  3. Filtra ofertas ya vistas (cache)
  4. Para cada oferta nueva: obtiene descripciÃ³n, evalÃºa con LLM
  5. Si score >= umbral: guarda en Notion + envÃ­a alerta Telegram
  6. Al finalizar: envÃ­a resumen diario por Telegram
  7. Rate limiting entre cada llamada (Task.Delay)
- Criterio: Compila sin errores, integra todos los services
- Dependencias: Tarea 1, Tarea 2

### Tarea 5: Crear Services/ConsoleMenu.cs (menÃº interactivo)
- Archivos: `src/CareerSentinel/Services/ConsoleMenu.cs`
- AcciÃ³n: Crear clase estÃ¡tica `ConsoleMenu` con mÃ©todo `ShowMainMenu()` que muestra:
  ```
  â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
   CareerSentinel - Buscador de Empleo
  â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
   [1] Ejecutar bÃºsqueda completa
   [2] Ver configuraciÃ³n actual
   [3] Configurar keywords
   [4] Configurar umbral de score
   [5] Salir
  â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
  ```
  Retorna la opciÃ³n elegida como int. MÃ©todo `ShowResultsSummary(int total, int matched, int saved)` para mostrar resultados post-ejecuciÃ³n.
- Criterio: Compila sin errores, menÃº legible en consola
- Dependencias: Ninguna

---

## Fase 3: Program.cs (Entry Point)

### Tarea 6: Reescribir Program.cs como Console App con DI y menÃº
- Archivos: `src/CareerSentinel/Program.cs`
- AcciÃ³n: Reescribir completamente. Usar `Microsoft.Extensions.DependencyInjection` (NO Host de Worker). Estructura:
  1. `ServiceCollection` + `ServiceProvider`
  2. Bind `AppSettings` desde `appsettings.json` + User Secrets
  3. Registrar `HttpClient` con Polly (retry + circuit breaker) para "LinkedIn"
  4. Registrar `HttpClient` con Polly para "Ollama"
  5. Registrar `IOptions<AppSettings>`
  6. Registrar todos los services: `ILinkedInScraper` â†’ `LinkedInScraper`, `LocalLlmService`, `NotionService`, `TelegramAlertService`, `IJobCacheService` â†’ `JobCacheService`, `JobOrchestrator`
  7. Loop principal: mostrar menÃº â†’ ejecutar opciÃ³n â†’ repetir hasta "Salir"
  8. OpciÃ³n 1: llamar `JobOrchestrator.RunAsync()` con try/catch
  9. Opciones 2-4: leer/modificar config en runtime
  10. OpciÃ³n 5: salir con mensaje de despedida
- Criterio: `dotnet build` compila sin errores, DI funciona correctamente
- Dependencias: Tarea 3, Tarea 4, Tarea 5

---

## Fase 4: ConfiguraciÃ³n y Secrets

### Tarea 7: Configurar User Secrets para valores sensibles
- Archivos: Ninguno (configurado via CLI)
- AcciÃ³n: Ejecutar comandos para configurar User Secrets:
  ```
  dotnet user-secrets set "Notion:ApiKey" "secret_value"
  dotnet user-secrets set "Notion:DatabaseId" "secret_value"
  dotnet user-secrets set "Telegram:BotToken" "secret_value"
  dotnet user-secrets set "Telegram:ChatId" "secret_value"
  ```
  NOTA: Estos valores serÃ¡n provistos por el usuario. Crear placeholder.
- Criterio: `dotnet user-secrets list` muestra las claves configuradas
- Dependencias: Tarea 6

### Tarea 8: Crear .gitignore para excluir secrets
- Archivos: `.gitignore` (raÃ­z del proyecto)
- AcciÃ³n: Crear/actualizar `.gitignore` para excluir: `appsettings.Development.json`, `appsettings.*.json`, `*.user`, `bin/`, `obj/`, `.vs/`, `seen_jobs.json`
- Criterio: Secrets no se commitean accidentalmente
- Dependencias: Ninguna

---

## Fase 5: Build y VerificaciÃ³n Final

### Tarea 9: Build completo y correcciÃ³n de errores
- AcciÃ³n: Ejecutar `dotnet build` completo. Corregir cualquier warning o error.
- Criterio: 0 errores, 0 warnings
- Dependencias: Todas las anteriores

### Tarea 10: VerificaciÃ³n de end-to-end (sin servicios externos)
- AcciÃ³n: Ejecutar `dotnet run` y verificar:
  1. El menÃº se muestra correctamente
  2. Se puede navegar con las opciones
  3. La opciÃ³n "Salir" funciona
  4. No hay errores de DI al resolver servicios
  5. OpciÃ³n "Ver configuraciÃ³n" muestra los valores actuales
- Criterio: La app arranca, muestra menÃº, y responde a input del usuario
- Dependencias: Tarea 9

---

## Resumen de Tareas

| # | Fase | Tarea | Archivos | Estado |
|---|------|-------|----------|--------|
| 1 | ReestructuraciÃ³n | Convertir .csproj a Console | `CareerSentinel.csproj` | ` |
| 2 | ReestructuraciÃ³n | Eliminar Worker.cs + Workers/ | `Worker.cs` | ` |
| 3 | ReestructuraciÃ³n | Completar appsettings.json | `appsettings.json` | ` |
| 4 | Orquestador | Crear JobOrchestrator.cs | `Services/JobOrchestrator.cs` | ` |
| 5 | Orquestador | Crear ConsoleMenu.cs | `Services/ConsoleMenu.cs` | ` |
| 6 | Entry Point | Reescribir Program.cs | `Program.cs` | ` |
| 7 | Secrets | Configurar User Secrets | (CLI) | ` |
| 8 | Secrets | Crear .gitignore | `.gitignore` | ` |
| 9 | VerificaciÃ³n | Build completo | â€” | ` |
| 10 | VerificaciÃ³n | End-to-end test | â€” | ` |

