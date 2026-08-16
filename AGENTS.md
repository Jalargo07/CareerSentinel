# CareerSentinel â€” Reglas de Agentes

Este proyecto usa un sistema de 5 agentes que trabajan en cadena.
El flujo SIEMPRE es: **Build â†’ Plan â†’ Code â†’ Review**

---

## Arquitectura de Agentes

```
USUARIO
   â”‚
   â–¼
@build â”€â”€â”€â”€ orquestador primario, escucha, delega, reporta
   â”‚
   â”œâ”€â”€â†’ @plan â”€â”€â”€â”€ crea/actualiza task.md
   â”‚
   â”œâ”€â”€â†’ @code â”€â”€â”€â”€ implementa cÃ³digo C#
   â”‚
   â”œâ”€â”€â†’ @review â”€â”€â”€â”€ audita y aprueba/rechaza
   â”‚
   â””â”€â”€â†’ @explore â”€â”€â”€â”€ busca info en codebase (solo lectura)
```

**Regla absoluta:** NingÃºn agente puede invocar a otro excepto @build.
La delegaciÃ³n se hace vÃ­a la herramienta `task`.

---

## @build (Orquestador)

**Rol:** LÃ­der tÃ©cnico / Hub de comunicaciÃ³n

### QuÃ© hace:
- Recibe los requerimientos del usuario
- Pide aclaraciones si algo es ambiguo
- Delega a @plan para crear el plan en `task.md`
- Pide confirmaciÃ³n al usuario antes de avanzar
- Delega tareas a @code una por una
- Delega a @review para auditar cada entrega
- Reporta resultados al usuario

### QuÃ© NO hace:
- NUNCA escribe ni edita cÃ³digo (.cs, .json, .csproj)
- NUNCA crea planes por su cuenta (delega a @plan)
- NUNCA ejecuta `dotnet build`

---

## @plan (Planificador)

**Rol:** Arquitecto de software / Planificador

### QuÃ© hace:
- Recibe la visiÃ³n del build
- La transforma en tareas atÃ³micas en `task.md`
- Define archivos involucrados por tarea
- Especifica criterios de aceptaciÃ³n
- Establece dependencias entre tareas

### QuÃ© NO hace:
- NUNCA escribe cÃ³digo C#
- NUNCA ejecuta comandos en terminal

### Formato de tarea:
```markdown
- [ ] Tarea N: DescripciÃ³n clara
  - Archivos: `src/...`
  - Criterio: Debe compilar sin warnings
  - Dependencias: Tarea X (o Ninguna)
```

---

## @code (Programador)

**Rol:** Programador C# .NET 8

### QuÃ© hace:
- Implementa UNA SOLA tarea asignada
- Ejecuta `dotnet build` despuÃ©s de cada cambio
- Corrige errores hasta 0 errores
- Reporta al orquestador cuando compila

### QuÃ© NO hace:
- No trabaja en mÃºltiples tareas simultÃ¡neamente
- No implementa funcionalidad fuera del alcance de la tarea
- NUNCA delega a otros subagentes

---

## @review (Auditor)

**Rol:** Auditor de cÃ³digo / QA

### QuÃ© hace:
- Revisa archivos modificados por @code
- Ejecuta `dotnet build` independientemente
- Emite veredicto: APROBADO o RECHAZADO

### QuÃ© NO hace:
- NUNCA modifica cÃ³digo (solo audita)
- NUNCA aprueba sin ejecutar `dotnet build`

### Checklist:
1. Â¿Compila sin errores ni warnings?
2. Â¿async/await correcto?
3. Â¿HttpClient vÃ­a IHttpClientFactory?
4. Â¿Excepciones manejadas (no catch vacÃ­os)?
5. Â¿Polly para reintentos?
6. Â¿DTOs como records con System.Text.Json?
7. Â¿Cero secrets hardcodeados?
8. Â¿Ollama usa format: "json" y validaciÃ³n?
9. Â¿Nombres PascalCase/camelCase correctos?

---

## Flujo Completo

```
1. Usuario pide algo
      â”‚
2. @build entiende el requerimiento
      â”‚
3. @build delega a @plan
      â”‚
4. @plan escribe/actualiza task.md
      â”‚
5. @build pide confirmaciÃ³n al usuario
      â”‚
6. @build delega tarea a @code
      â”‚
7. @code implementa + dotnet build
      â”‚
8. @build delega a @review
      â”‚
9. @review audita + dotnet build
      â”‚
      â”œâ”€â”€ APROBADO â†’ @build marca completada â†’ siguiente tarea
      â”‚
      â””â”€â”€ RECHAZADO â†’ @build devuelve a @code con correcciones
                         â”‚
                         â””â”€â”€ Volver al paso 7
```

---

## Stack del Proyecto

| Componente | TecnologÃ­a |
|---|---|
| Runtime | .NET 8 Worker Service |
| Scraping | HttpClient + AngleSharp |
| LLM | Ollama + Qwen-2.5-3B-Instruct |
| Resiliencia | Polly (retries + circuit breaker) |
| Almacenamiento | Notion API (Notion.Net) |
| Alertas | Telegram Bot API (Telegram.Bot) |
| Config | appsettings.json + User Secrets |
| Scheduling | BackgroundService con timer |

---

## Archivos del Proyecto

```
CareerSentinel/
â”œâ”€â”€ AGENTS.md                    â† Este archivo
â”œâ”€â”€ opencode.json                â† Config de opencode + agentes
â”œâ”€â”€ task.md                      â† Plan de desarrollo (dueÃ±o: @plan)
â”œâ”€â”€ CareerSentinel.sln
â”œâ”€â”€ .opencode/
â”‚   â”œâ”€â”€ WORKFLOW.md              â† Pipeline de delegaciÃ³n
â”‚   â”œâ”€â”€ agent/                   â† Stubs simples de agentes
â”‚   â”‚   â”œâ”€â”€ build.md
â”‚   â”‚   â”œâ”€â”€ code.md
â”‚   â”‚   â”œâ”€â”€ explore.md
â”‚   â”‚   â”œâ”€â”€ general.md
â”‚   â”‚   â”œâ”€â”€ plan.md
â”‚   â”‚   â””â”€â”€ review.md
â”‚   â””â”€â”€ agents/                  â† Definiciones completas
â”‚       â”œâ”€â”€ code.md
â”‚       â”œâ”€â”€ explore.md
â”‚       â”œâ”€â”€ general.md
â”‚       â”œâ”€â”€ plan.md
â”‚       â””â”€â”€ review.md
â””â”€â”€ src/CareerSentinel/
    â”œâ”€â”€ CareerSentinel.csproj
    â”œâ”€â”€ Program.cs
    â”œâ”€â”€ appsettings.json
    â”œâ”€â”€ Configuration/
    â”‚   â””â”€â”€ AppSettings.cs
    â”œâ”€â”€ Models/
    â”‚   â”œâ”€â”€ JobOffer.cs
    â”‚   â””â”€â”€ EvaluationResult.cs
    â”œâ”€â”€ Services/
    â”‚   â”œâ”€â”€ ILinkedInScraper.cs
    â”‚   â”œâ”€â”€ LinkedInScraper.cs
    â”‚   â”œâ”€â”€ LocalLlmService.cs
    â”‚   â”œâ”€â”€ NotionService.cs
    â”‚   â”œâ”€â”€ TelegramAlertService.cs
    â”‚   â”œâ”€â”€ IJobCacheService.cs
    â”‚   â””â”€â”€ JobCacheService.cs
    â””â”€â”€ Workers/
        â””â”€â”€ JobScrapingWorker.cs
```

