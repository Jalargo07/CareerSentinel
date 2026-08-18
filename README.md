# CareerSentinel

[![.NET 8](https://img.shields.io/badge/.NET-8.0-purple?logo=dotnet)](https://dotnet.microsoft.com/)
[![C# 12](https://img.shields.io/badge/C%23-12.0-blue?logo=csharp)](https://docs.microsoft.com/es-es/dotnet/csharp/)
[![Gemini](https://img.shields.io/badge/Gemini-3.5%20Flash-4285F4?logo=google)](https://ai.google.dev/)
[![License](https://img.shields.io/badge/License-MIT-green)](LICENSE)

> Automated job scraping and AI-powered evaluation system that scans LinkedIn and CompuTrabajo, scores opportunities against your profile, and sends real-time alerts via Telegram.

---

## Overview

CareerSentinel is a .NET 8 worker service that automates the job search pipeline:

1. **Scrape** job listings from multiple portals (LinkedIn, CompuTrabajo)
2. **Extract** structured data from each listing using AI
3. **Evaluate** compatibility against your candidate profile
4. **Alert** via Telegram when strong matches are found

Built as a personal project to solve a real problem: spending hours manually reviewing job postings that don't match my skills or preferences.

---

## Architecture

```
┌──────────────────────────────────────────────────────────────────┐
│                        JobOrchestrator                           │
│                                                                  │
│  ┌──────────────┐   ┌──────────────┐   ┌──────────────┐        │
│  │   LinkedIn    │   │ CompuTrabajo │   │  (Future)    │        │
│  │   Scraper     │   │   Scraper    │   │   Scrapers   │        │
│  └──────┬───────┘   └──────┬───────┘   └──────┬───────┘        │
│         │                  │                   │                 │
│         └──────────────────┼───────────────────┘                 │
│                            │                                     │
│                   ┌────────▼────────┐                            │
│                   │   IJobScraper   │  Strategy Pattern          │
│                   └────────┬────────┘                            │
│                            │                                     │
│              ┌─────────────┼─────────────┐                       │
│              │             │             │                        │
│        ┌─────▼─────┐ ┌────▼────┐ ┌──────▼──────┐               │
│        │  Paso 1   │ │ Paso 2  │ │   Notion    │               │
│        │ Extract   │ │ Evaluate│ │   + Telegram │               │
│        │ (Batch)   │ │ (Batch) │ │   Alerts    │               │
│        └───────────┘ └─────────┘ └─────────────┘               │
│                                                                  │
└──────────────────────────────────────────────────────────────────┘
```

### Pipeline

The system processes jobs in **batches of 5** to minimize API costs:

| Step | Description | API Calls |
|------|-------------|-----------|
| **Scrape** | Extract listings from portal (HTTP only) | 0 |
| **Paso 1** | Extract structured data (title, techs, seniority, modality) | 1 per batch |
| **Paso 2** | Evaluate compatibility against candidate profile | 1 per batch |
| **Alert** | Send matches to Telegram + save to Notion | 0 |

With 30 job listings, this means **6 API calls** instead of 60 — a 10x reduction in token usage.

---

## Tech Stack

| Component | Technology | Purpose |
|-----------|-----------|---------|
| **Runtime** | .NET 8 | Worker service with dependency injection |
| **Language** | C# 12 | Records, pattern matching, source generators |
| **Scraping** | AngleSharp + HttpClient | HTML parsing with anti-bot protection |
| **AI** | Gemini 3.5 Flash Lite | Job extraction and evaluation (free tier) |
| **Resilience** | Polly | Retry with exponential backoff + circuit breaker |
| **Storage** | Notion API | Job offer persistence |
| **Alerts** | Telegram Bot API | Real-time match notifications |
| **Config** | Options Pattern + User Secrets | Type-safe configuration |

---

## Features

### Multi-Source Scraping
- LinkedIn guest API (no authentication required)
- CompuTrabajo with HTML parsing and anti-bot headers
- Modular `IJobScraper` interface — add new portals in minutes
- Geographic filtering and seniority pre-filters

### AI-Powered Evaluation
- **2-step Chain-of-Thought pipeline**: extraction → evaluation
- **Batch processing**: 5 jobs per API call for cost efficiency
- **Structured output**: JSON mode with typed models
- **Discrete scoring**: {0, 10, 25, 30, 85} with reconciliation logic

### Smart Filtering
- Duplicate detection via local cache + Notion dedup
- Description validation (skips login walls, short content)
- Geographic matching (remote-friendly, Colombia, Latin America)
- Seniority alignment (Junior profile vs Senior requirements)

### Real-Time Alerts
- Telegram notifications for high-scoring matches (≥85)
- Daily summary with top matches
- Notion integration for persistent tracking

---

## Getting Started

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Google AI Studio API key](https://aistudio.google.com/apikey) (free tier)

### Installation

```bash
git clone https://github.com/Jalargo07/CareerSentinel.git
cd CareerSentinel/src/CareerSentinel
dotnet restore
```

### Configuration

Set up your API key and secrets:

```bash
dotnet user-secrets set "AppSettings:OpenCodeGo:ApiKey" "YOUR_GEMINI_API_KEY"
dotnet user-secrets set "Telegram:BotToken" "YOUR_BOT_TOKEN"
dotnet user-secrets set "Telegram:ChatId" "YOUR_CHAT_ID"
```

### Run

```bash
dotnet run
```

```
══════════════════════════════════════════
  CareerSentinel - Job Search Automation
══════════════════════════════════════════
  [1] Run full search (all sources)
  [2] View current configuration
  [3] Configure keywords
  [4] Configure score threshold
  [5] View enabled sources
  [6] Enable/disable sources
  [7] Run LinkedIn only
  [8] Run CompuTrabajo only
  [9] Exit
══════════════════════════════════════════
```

---

## Design Decisions

### Why batch processing?

Individual API calls for 30+ jobs would exhaust the free tier in minutes. Batching 5 jobs per call reduces API calls by 10x while maintaining accuracy through strict delimiters and index-based result alignment.

### Why Gemini Flash Lite?

| Factor | Decision |
|--------|----------|
| **Cost** | Free tier: 1,500 requests/day |
| **Speed** | ~200ms per batch |
| **JSON mode** | Native `response_format` support |
| **Accuracy** | Sufficient for structured extraction |

### Why .NET 8 Worker Service?

- Built-in dependency injection
- `IHttpClientFactory` for resilient HTTP
- Polly integration for retry/circuit-breaker
- Options pattern for type-safe config
- Source generators for zero-allocation regex

---

## Project Structure

```
src/CareerSentinel/
├── Program.cs                    # DI container + entry point
├── appsettings.json              # Configuration
├── Configuration/
│   └── AppSettings.cs            # Strongly-typed settings
├── Models/
│   ├── JobOffer.cs               # Job listing DTO
│   ├── JobAnalysis.cs            # Paso 1 output (extraction)
│   ├── EvaluationResult.cs       # Paso 2 output (scoring)
│   └── BatchEvaluationRequest.cs # Batch request models
├── Services/
│   ├── IJobScraper.cs            # Scraper interface (Strategy)
│   ├── LinkedInScraper.cs        # LinkedIn guest API
│   ├── CompuTrabajoScraper.cs    # CompuTrabajo HTML parser
│   ├── ILlmService.cs            # LLM interface
│   ├── OpenCodeGoService.cs      # Gemini API client
│   ├── LocalLlmService.cs        # Ollama local fallback
│   ├── HybridLlmService.cs       # Ollama + API hybrid
│   ├── JobOrchestrator.cs        # Pipeline orchestrator
│   ├── NotionService.cs          # Notion integration
│   ├── TelegramAlertService.cs   # Telegram notifications
│   ├── IJobCacheService.cs       # Cache interface
│   ├── JobCacheService.cs        # JSON file cache
│   └── ConsoleMenu.cs            # Interactive menu
└── Workers/
    └── JobScrapingWorker.cs      # Background service (future)
```

---

## Author

**Jorge Alejandro Largo Rojas**
- Junior Developer | 2 years experience
- Backend: Node.js, Express, TypeScript, Python, Java
- Databases: PostgreSQL, SQL Server
- Frontend: Vue.js 3

[![GitHub](https://img.shields.io/badge/GitHub-Jalargo07-181717?logo=github)](https://github.com/Jalargo07)
[![LinkedIn](https://img.shields.io/badge/LinkedIn-jorge--alejandro--largo--rojas-0A66C2?logo=linkedin)](https://www.linkedin.com/in/jorge-alejandro-largo-rojas-010528368/)
[![Email](https://img.shields.io/badge/Email-alejandrolargorojas@gmail.com-D44638?logo=gmail)](mailto:alejandrolargorojas@gmail.com)

---

## License

MIT License - see [LICENSE](LICENSE) for details.
