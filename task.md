# Plan de Mejoras � CareerSentinel
## Estado: COMPLETADO ?

---

## Prioridad 1 � Bugs / Dead Code

- [x] Tarea 1: Eliminar ILinkedInScraper.cs y su registro en Program.cs (dead code)
  - Archivos: src/CareerSentinel/Services/ILinkedInScraper.cs, src/CareerSentinel/Program.cs
  - Criterio: Compila sin errores, no hay referencias rotas
  - Dependencias: Ninguna

- [x] Tarea 2: Eliminar AntiBotHttpClientHandler.cs (dead code, nunca se usa)
  - Archivos: src/CareerSentinel/Services/AntiBotHttpClientHandler.cs
  - Criterio: Compila sin errores, archivo eliminado
  - Dependencias: Ninguna

- [x] Tarea 3: Fix CompuTrabajoScraper.cs � usar ubicaci�n del config en lugar de "medellin-antioquia" hardcodeado
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

## Prioridad 2 � Refactorizaci�n

- [x] Tarea 6: Extraer bloques try/catch id�nticos de Program.cs (cases 4, 5, 6) en un m�todo RunSearchAsync()
  - Archivos: src/CareerSentinel/Program.cs
  - Criterio: Compila sin errores, c�digo DRY, misma funcionalidad
  - Dependencias: Ninguna

- [x] Tarea 7: Crear helper de parsing JSON compartido para LocalLlmService y OpenCodeGoService (clase est�tica JsonJobParser)
  - Archivos: src/CareerSentinel/Services/JsonJobParser.cs (nuevo), src/CareerSentinel/Services/LocalLlmService.cs, src/CareerSentinel/Services/OpenCodeGoService.cs
  - Criterio: Compila sin errores, parsing centralizado, sin duplicaci�n
  - Dependencias: Ninguna

- [x] Tarea 8: Extraer l�gica de ConsoleMenu.SaveConfiguration a un ConfigurationService dedicado
  - Archivos: src/CareerSentinel/Services/ConfigurationService.cs (nuevo), src/CareerSentinel/Services/ConsoleMenu.cs, src/CareerSentinel/Program.cs
  - Criterio: Compila sin errores, ConsoleMenu delega a ConfigurationService
  - Dependencias: Ninguna

---

## Prioridad 3 � Performance / Seguridad

- [x] Tarea 9: Cambiar File.AppendAllText s�ncrono por StreamWriter async con buffer en logging de evaluaciones
  - Archivos: src/CareerSentinel/Services/LocalLlmService.cs, src/CareerSentinel/Services/OpenCodeGoService.cs, src/CareerSentinel/Services/AsyncFileLogger.cs (nuevo)
  - Criterio: Compila sin errores, I/O as�ncrono, flush peri�dico
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

## Prioridad 4 � Mejoras UX del Men�

- [x] Tarea 13: Crear wizard de primer uso � si Telegram.ChatId est� vac�o, detectar y guiar al usuario paso a paso
  - Archivos: Program.cs, ConsoleMenu.cs
  - Criterio: Si es primer uso, el men� muestra un flujo guiado en lugar del men� principal
  - Dependencias: Ninguna

- [x] Tarea 14: Unificar y corregir ShowConfig (opci�n 7 del men�)
  - Mostrar ProcessingMode y OpenCodeGo (actualmente no se muestran)
  - Mostrar JobSources["LinkedIn"].Location en lugar de LinkedIn.Location (que no existe)
  - Mostrar Scoring.Threshold (solo lectura)
  - Archivos: ConsoleMenu.cs
  - Criterio: ShowConfig muestra TODA la config relevante
  - Dependencias: Ninguna

- [x] Tarea 15: Separar "Ver configuraci�n" de "Editar configuraci�n"
  - La opci�n 7 del men� principal solo muestra (view only)
  - La opci�n 8 edita perfil, 9 edita IA, 0 abre submen� de fuentes
  - Archivos: ConsoleMenu.cs, Program.cs
  - Criterio: Men� principal con opciones claras de Ver vs Editar
  - Dependencias: Ninguna

- [x] Tarea 16: Split del men� "Mi Perfil" (9 opciones ? grupos l�gicos)
  - Grupo A: Datos Personales [1-3] ? nombre, nivel, a�os exp
  - Grupo B: Preferencias [4-6] ? modalidad, regiones, skills
  - Grupo C: B�squeda & CV [7-9] ? keywords, descripci�n CV, Chat ID
  - Navegaci�n por letras [A/B/C] adem�s de n�meros
  - Archivos: ConsoleMenu.cs
  - Criterio: Men� de perfil organizado en secciones claras
  - Dependencias: Ninguna

- [x] Tarea 17: Pantalla de resultados con men� contextual post-b�squeda
  - Despu�s de buscar: [1] Ver ofertas, [2] Buscar de nuevo, [3] Editar perfil, [4] Ver config, [0] Volver
  - Archivos: ConsoleMenu.cs, Program.cs, JobOrchestrator.cs, Models/SearchResult.cs (nuevo)
  - Criterio: Despu�s de buscar, el usuario tiene opciones contextuales
  - Dependencias: Ninguna

---

## Prioridad 5 - Bug Fixes (LLM Evaluation)

- [x] Tarea 18: Fix OpenCodeGoService prompt - R1-R5 rules incorrectos
  - Archivos: src/CareerSentinel/Services/OpenCodeGoService.cs
  - Criterio: BuildBatchEvaluationPrompt usa reglas R1-R5 correctas (solo score 85 = match)
  - Dependencias: Ninguna

- [x] Tarea 19: Fix OpenCodeGoService ParseBatchResponse - Match override con Score >= 50
  - Archivos: src/CareerSentinel/Services/OpenCodeGoService.cs
  - Criterio: ParseBatchResponse respeta decision del LLM (Score == 85 para match)
  - Dependencias: Tarea 18

- [x] Tarea 20: Fix JobOrchestrator - ignoraba evaluation.Match del LLM
  - Archivos: src/CareerSentinel/Services/JobOrchestrator.cs
  - Criterio: Usa evaluation.Match en vez de evaluation.Score >= threshold
  - Dependencias: Ninguna

- [x] Tarea 21: Agregar filtro "Cualquiera" modality en C#
  - Archivos: src/CareerSentinel/Services/JobOrchestrator.cs
  - Criterio: Si PreferredModality="Cualquiera", Remoto acepta cualquier region; Presencial/Hibrido solo si ubicacion esta en PreferredRegions
  - Dependencias: Tarea 20

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
| 13 | UX Men� | Wizard de primer uso | ? |
| 14 | UX Men� | Unificar y corregir ShowConfig | ? |
| 15 | UX Men� | Separar Ver vs Editar configuraci�n | ? |
| 16 | UX Men� | Split men� Mi Perfil en grupos | ? |
| 17 | UX Men� | Men� contextual post-b�squeda | ? |
| 18 | Bug Fix | Fix OpenCodeGoService prompt R1-R5 | ? |
| 19 | Bug Fix | Fix ParseBatchResponse Match override | ? |
| 20 | Bug Fix | Fix JobOrchestrator usar evaluation.Match | ? |
| 21 | Bug Fix | Agregar filtro "Cualquiera" modality C# | ? |

**TOTAL: 21/21 tareas completadas. Proyecto: 10/10.**
