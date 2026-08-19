# CareerSentinel

[![.NET 8](https://img.shields.io/badge/.NET-8.0-purple?logo=dotnet)](https://dotnet.microsoft.com/)
[![C# 12](https://img.shields.io/badge/C%23-12.0-blue?logo=csharp)](https://docs.microsoft.com/es-es/dotnet/csharp/)
[![Gemini](https://img.shields.io/badge/Gemini-3.5%20Flash-4285F4?logo=google)](https://ai.google.dev/)
[![License](https://img.shields.io/badge/License-MIT-green)](LICENSE)

> Automated job scraping and AI-powered evaluation system that scans LinkedIn and CompuTrabajo, scores opportunities against your profile using batch AI inference, and sends real-time alerts via Telegram.

---

## Overview

CareerSentinel is a .NET 8 console application designed to automate the job search pipeline:

1. **Scrape** job listings from multiple portals (LinkedIn, CompuTrabajo).
2. **Extract** structured technical requirements from raw job descriptions using AI.
3. **Evaluate** candidate compatibility against strict business rules (R1-R5).
4. **Alert** via Telegram when strong matches are found and persist records to Notion.

---

## Architecture

```text
+------------------------------------------------------------------+
|                         JobOrchestrator                          |
|                                                                  |
|  +--------------+   +--------------+   +--------------+          |
|  |   LinkedIn   |   | CompuTrabajo |   |   (Future)   |          |
|  |   Scraper    |   |   Scraper    |   |   Scraper    |          |
|  +--------------+   +--------------+   +--------------+          |
|         |                  |                   |                 |
|         +------------------+-------------------+                 |
|                            |                                     |
|                   +--------v--------+                            |
|                   |  IJobScraper    |   Strategy Pattern         |
|                   +--------^--------+                            |
|                            |                                     |
|              +-------------+-------------+                       |
|              |                           |                       |
|        +-----v-----+               +-----v-----+                 |
|        |  Paso 1   |               |  Paso 2   |                 |
|        | Extract   |               | Evaluate  |                 |
|        | (Batch 5) |               | (Batch 5) |                 |
|        +-----+-----+               +-----+-----+                 |
|              |                           |                       |
|              +-------------+-------------+                       |
|                            |                                     |
|                   +--------v--------+                            |
|                   | Notion + Alert  |                            |
|                   |  (Telegram)     |                            |
|                   +-----------------+                            |
+------------------------------------------------------------------+
```
### Pipeline

The system processes job offers in batches of 5 using Google Gemini's structured output capability, reducing API overhead significantly:

| Step | Description | API Calls (per 30 jobs) |
|------|-------------|--------------------------|
| **Scrape** | Extract listings from portal (HTTP only) | 0 |
| **Paso 1** | Extract structured data (title, techs, seniority, modality) | 6 requests |
| **Paso 2** | Evaluate compatibility against candidate profile | 6 requests |
| **Alert** | Send matches to Telegram + save to Notion | 0 |

With 30 job listings, this means **12 API calls** total instead of 60 - a 5x to 10x reduction in token consumption and execution time.

---

## Tech Stack

| Component | Technology | Purpose |
|-----------|-----------|---------|
| **Runtime** | .NET 8 | Core console runtime with Microsoft.Extensions DI |
| **Language** | C# 12 | Records, primary constructors, pattern matching |
| **Scraping** | AngleSharp + HttpClient | HTML parsing with anti-bot user-agent rotation |
| **AI Engine** | Gemini 3.5 Flash API | Batch extraction and scoring (Free Tier) |
| **Resilience** | Polly | Retry with exponential backoff, jitter, and circuit breaker |
| **Storage** | Notion API | Structured persistence of evaluated offers |
| **Alerts** | Telegram Bot API | Instant notification for high-score matches |
| **Config** | Options Pattern + User Secrets | Type-safe configuration management |

---

## Evaluation Rules & Scoring Engine

The Paso 2 evaluator applies strict, deterministic business rules (R1-R5) to calculate compatibility:

| Score | Match | Rule | Description |
|-------|-------|------|-------------|
| 0 | false | R1 | Empty technology stack or invalid/login-wall description text. |
| 10 | false | R2 | On-site or Hybrid roles outside candidate's target regions. |
| 25 | false | R3 | Seniority mismatch (e.g., Senior required vs. Junior profile). |
| 30 | false | R4 | Tech stack overlap < 2 matching core technologies. |
| 85 | true | R5 | Target match (Valid location, compatible seniority, >= 2 core techs). |

---

## Features

### Smart Setup Wizard
Automatically detects missing settings on first run and guides configuration step-by-step.

### Multi-Source Scraping
Modular IJobScraper architecture supporting LinkedIn guest API and CompuTrabajo HTML parsing.

### Batch AI Processing
Aggregates 5 offers per LLM prompt, maintaining alignment with structured JSON schemas.

### Polly Resilience Pipeline
Automatic retries with jitter (+20% variance) and 30-second circuit breaker on API failures.

### Real-time Notifications
Direct Telegram alerts for scores >= 85 and persistent tracking in Notion.

### Intelligent Menu
- View vs Edit separation for clear configuration management
- Organized profile menu with 3 logical groups [A/B/C]
- Contextual post-search menu with actionable options

---

## Getting Started

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Google AI Studio API Key](https://aistudio.google.com/apikey) (Free Tier)

### Installation & Setup
```bash
git clone https://github.com/Jalargo07/CareerSentinel.git
cd CareerSentinel/src/CareerSentinel
dotnet restore
```

Configure local User Secrets:

```bash
dotnet user-secrets set "AppSettings:OpenCodeGo:ApiKey" "YOUR_GEMINI_API_KEY"
dotnet user-secrets set "AppSettings:Telegram:BotToken" "YOUR_TELEGRAM_BOT_TOKEN"
dotnet user-secrets set "AppSettings:Telegram:ChatId" "YOUR_TELEGRAM_CHAT_ID"
```

### Run

```bash
dotnet run
```

---

## Project Structure
```text
src/CareerSentinel/
├── Program.cs                    # DI Container & Entry Point
├── appsettings.json              # Base application settings
├── Configuration/
│   └── AppSettings.cs            # Strongly-typed settings model
├── Models/
│   ├── JobOffer.cs               # Raw scraper DTO
│   ├── JobAnalysis.cs            # Paso 1 extraction result
│   ├── EvaluationResult.cs       # Paso 2 scoring result
│   ├── BatchEvaluationRequest.cs # Batch request models
│   └── SearchResult.cs           # Execution metrics DTO
└── Services/
    ├── IJobScraper.cs            # Scraper Strategy Interface
    ├── LinkedInScraper.cs        # LinkedIn guest API client
    ├── CompuTrabajoScraper.cs    # CompuTrabajo AngleSharp parser
    ├── ILlmService.cs            # LLM Service Interface
    ├── OpenCodeGoService.cs      # Gemini API client
    ├── LocalLlmService.cs        # Ollama local fallback
    ├── HybridLlmService.cs       # Ollama + API hybrid
    ├── JsonJobParser.cs          # Shared JSON parsing helper
    ├── ConfigurationService.cs   # Config persistence service
    ├── AsyncFileLogger.cs        # Async buffered file logging
    ├── JobOrchestrator.cs        # Main execution pipeline coordinator
    ├── NotionService.cs          # Notion API Integration
    ├── TelegramAlertService.cs   # Telegram notifications dispatcher
    ├── IJobCacheService.cs       # Cache interface
    ├── JobCacheService.cs        # Local JSON deduplication cache
    └── ConsoleMenu.cs            # Interactive console menu & setup wizard
```
---

## Author

**Jorge Alejandro Largo Rojas**

Full-Stack Developer | Backend Specialist

Stack: Node.js, Express, C# (.NET 8), TypeScript, PostgreSQL, Vue.js

[![GitHub](https://img.shields.io/badge/GitHub-Jalargo07-181717?logo=github)](https://github.com/Jalargo07)
[![LinkedIn](https://img.shields.io/badge/LinkedIn-jorge--alejandro--largo--rojas-0A66C2?logo=linkedin)](https://www.linkedin.com/in/jorge-alejandro-largo-rojas-010528368/)
[![Email](https://img.shields.io/badge/Email-alejandrolargorojas@gmail.com-D44638?logo=gmail)](mailto:alejandrolargorojas@gmail.com)

---

## License

MIT License - see [LICENSE](LICENSE) for details.