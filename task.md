# Plan de Mejoras — CareerSentinel
## Estado: COMPLETADO ?

---

## Prioridad 1 — Bugs / Dead Code

- [x] Tarea 1: Eliminar ILinkedInScraper.cs y su registro en Program.cs (dead code)
  - Archivos: src/CareerSentinel/Services/ILinkedInScraper.cs, src/CareerSentinel/Program.cs
  - Criterio: Compila sin errores, no hay referencias rotas
  - Dependencias: Ninguna

- [x] Tarea 2: Eliminar AntiBotHttpClientHandler.cs (dead code, nunca se usa)
  - Archivos: src/CareerSentinel/Services/AntiBotHttpClientHandler.cs
  - Criterio: Compila sin errores, archivo eliminado
  - Dependencias: Ninguna

- [x] Tarea 3: Fix CompuTrabajoScraper.cs — usar ubicación del config en lugar de "medellin-antioquia" hardcodeado
  - Archivos: src/CareerSentinel/Services/CompuTrabajoScraper.cs
  - Criterio: Compila sin errores, location viene de JobSourceSettings.Location
  - Dependencias: Ninguna

- [x] Tarea 4: Eliminar BatchEvaluationResponse.cs (dead code, nunca se usa)
  - Archivos: src/CareerSentinel/Models/BatchEvaluationResponse.cs
  - Criterio: Compila sin errores, archivo eliminado
  - Dependencias: Ninguna

- [x] Tarea 5: Hacer que LinkedInScraper use LinkedInSettings.BaseUrl del config en lugar de URL hardcodeada
  - Archivos: src/CareerSentinel/Services/LinkedInScraper.cs
  - Criterio: Compila sin errores, URL viene de config, no hardcodeada
  - Dependencias: Ninguna

---

## Prioridad 2 — Refactorización

- [x] Tarea 6: Extraer bloques try/catch idénticos de Program.cs (cases 4, 5, 6) en un método RunSearchAsync()
  - Archivos: src/CareerSentinel/Program.cs
  - Criterio: Compila sin errores, código DRY, misma funcionalidad
  - Dependencias: Ninguna

- [x] Tarea 7: Crear helper de parsing JSON compartido para LocalLlmService y OpenCodeGoService (clase estática JsonJobParser)
  - Archivos: src/CareerSentinel/Services/JsonJobParser.cs (nuevo), src/CareerSentinel/Services/LocalLlmService.cs, src/CareerSentinel/Services/OpenCodeGoService.cs
  - Criterio: Compila sin errores, parsing centralizado, sin duplicación
  - Dependencias: Ninguna

- [x] Tarea 8: Extraer lógica de ConsoleMenu.SaveConfiguration a un ConfigurationService dedicado
  - Archivos: src/CareerSentinel/Services/ConfigurationService.cs (nuevo), src/CareerSentinel/Services/ConsoleMenu.cs, src/CareerSentinel/Program.cs
  - Criterio: Compila sin errores, ConsoleMenu delega a ConfigurationService
  - Dependencias: Ninguna

---

## Prioridad 3 — Performance / Seguridad

- [x] Tarea 9: Cambiar File.AppendAllText síncrono por StreamWriter async con buffer en logging de evaluaciones
  - Archivos: src/CareerSentinel/Services/LocalLlmService.cs, src/CareerSentinel/Services/OpenCodeGoService.cs, src/CareerSentinel/Services/AsyncFileLogger.cs (nuevo)
  - Criterio: Compila sin errores, I/O asíncrono, flush periódico
  - Dependencias: Ninguna

- [x] Tarea 10: Agregar jitter a Polly retry policies (+20% de aleatoriedad)
  - Archivos: src/CareerSentinel/Program.cs, src/CareerSentinel/Services/LocalLlmService.cs, src/CareerSentinel/Services/OpenCodeGoService.cs
  - Criterio: Compila sin errores, retry con jitter aplicado
  - Dependencias: Ninguna

- [x] Tarea 11: Reducir logging de Information a Debug en loops de scraping (LinkedInScraper, CompuTrabajoScraper)
  - Archivos: src/CareerSentinel/Services/LinkedInScraper.cs, src/CareerSentinel/Services/CompuTrabajoScraper.cs
  - Criterio: Compila sin errores, logs de scraping en nivel Debug
  - Dependencias: Ninguna

- [x] Tarea 12: Hacer que CvDescription del config se use en los prompts LLM o eliminar el campo si no aplica
  - Archivos: src/CareerSentinel/Configuration/AppSettings.cs, src/CareerSentinel/Services/LocalLlmService.cs, src/CareerSentinel/Services/OpenCodeGoService.cs
  - Criterio: Compila sin errores, CvDescription se usa en prompts
  - Dependencias: Ninguna

---

## Prioridad 4 — Mejoras UX del Menú

- [x] Tarea 13: Crear wizard de primer uso — si Telegram.ChatId está vacío, detectar y guiar al usuario paso a paso
  - Archivos: Program.cs, ConsoleMenu.cs
  - Criterio: Si es primer uso, el menú muestra un flujo guiado en lugar del menú principal
  - Dependencias: Ninguna

- [x] Tarea 14: Unificar y corregir ShowConfig (opción 7 del menú)
  - Mostrar ProcessingMode y OpenCodeGo (actualmente no se muestran)
  - Mostrar JobSources["LinkedIn"].Location en lugar de LinkedIn.Location (que no existe)
  - Mostrar Scoring.Threshold (solo lectura)
  - Archivos: ConsoleMenu.cs
  - Criterio: ShowConfig muestra TODA la config relevante
  - Dependencias: Ninguna

- [x] Tarea 15: Separar "Ver configuración" de "Editar configuración"
  - La opción 7 del menú principal solo muestra (view only)
  - La opción 8 edita perfil, 9 edita IA, 0 abre submenú de fuentes
  - Archivos: ConsoleMenu.cs, Program.cs
  - Criterio: Menú principal con opciones claras de Ver vs Editar
  - Dependencias: Ninguna

- [x] Tarea 16: Split del menú "Mi Perfil" (9 opciones ? grupos lógicos)
  - Grupo A: Datos Personales [1-3] ? nombre, nivel, años exp
  - Grupo B: Preferencias [4-6] ? modalidad, regiones, skills
  - Grupo C: Búsqueda & CV [7-9] ? keywords, descripción CV, Chat ID
  - Navegación por letras [A/B/C] además de números
  - Archivos: ConsoleMenu.cs
  - Criterio: Menú de perfil organizado en secciones claras
  - Dependencias: Ninguna

- [x] Tarea 17: Pantalla de resultados con menú contextual post-búsqueda
  - Después de buscar: [1] Ver ofertas, [2] Buscar de nuevo, [3] Editar perfil, [4] Ver config, [0] Volver
  - Archivos: ConsoleMenu.cs, Program.cs, JobOrchestrator.cs, Models/SearchResult.cs (nuevo)
  - Criterio: Después de buscar, el usuario tiene opciones contextuales
  - Dependencias: Ninguna

---

## Resumen
| # | Prioridad | Tarea | Estado |
|---|-----------|-------|--------|
| 1 | Bugs | Eliminar ILinkedInScraper dead code | ? |
| 2 | Bugs | Eliminar AntiBotHttpClientHandler dead code | ? |
| 3 | Bugs | Fix CompuTrabajoScraper hardcodeado | ? |
| 4 | Bugs | Eliminar BatchEvaluationResponse dead code | ? |
| 5 | Bugs | LinkedInScraper usar config BaseUrl | ? |
| 6 | Refactor | Extraer RunSearchAsync de Program.cs | ? |
| 7 | Refactor | Crear JsonJobParser compartido | ? |
| 8 | Refactor | Extraer ConfigurationService | ? |
| 9 | Perf/Sec | StreamWriter async para logging | ? |
| 10 | Perf/Sec | Agregar jitter a Polly retries | ? |
| 11 | Perf/Sec | Reducir logging a Debug en scraping | ? |
| 12 | Perf/Sec | Usar CvDescription en prompts o eliminarlo | ? |
| 13 | UX Menú | Wizard de primer uso | ? |
| 14 | UX Menú | Unificar y corregir ShowConfig | ? |
| 15 | UX Menú | Separar Ver vs Editar configuración | ? |
| 16 | UX Menú | Split menú Mi Perfil en grupos | ? |
| 17 | UX Menú | Menú contextual post-búsqueda | ? |

**TOTAL: 17/17 tareas completadas. Proyecto: 10/10.**
