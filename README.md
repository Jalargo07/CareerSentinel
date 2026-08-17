# 🛡️ CareerSentinel

[![.NET](https://img.shields.io/badge/.NET-8.0-purple?logo=dotnet)](https://dotnet.microsoft.com/)
[![C#](https://img.shields.io/badge/C%23-12.0-blue?logo=csharp)](https://docs.microsoft.com/es-es/dotnet/csharp/)
[![License](https://img.shields.io/badge/License-MIT-green)](LICENSE)

> 🤖 **Asistente inteligente de búsqueda de empleo** que automatiza el scraping de portales laborales, evalúa ofertas con IA local y te envía alertas personalizadas por Telegram.

## 📋 Tabla de Contenidos

- [Descripción](#-descripción)
- [Características](#-características)
- [Arquitectura](#-arquitectura)
- [Stack Tecnológico](#-stack-tecnológico)
- [Requisitos Previos](#-requisitos-previos)
- [Instalación](#-instalación)
- [Configuración](#-configuración)
- [Uso](#-uso)
- [Estructura del Proyecto](#-estructura-del-proyecto)
- [Diseño de Agentes](#-diseño-de-agentes)
- [Decisiones de Arquitectura](#-decisiones-de-arquitectura)
- [Autor](#-autor)

## 📖 Descripción

**CareerSentinel** es una aplicación de escritorio desarrollada en .NET 8 que automatiza el proceso de búsqueda de empleo. El sistema:

- **Scrapea** múltiples portales de empleo (LinkedIn, CompuTrabajo)
- **Evalúa** cada oferta usando un modelo de lenguaje local (Ollama + Qwen 2.5)
- **Filtra** automáticamente ofertas que no coinciden con tu perfil
- **Guarda** las mejores ofertas en Notion
- **Envía** alertas instantáneas por Telegram cuando hay coincidencias de alto puntaje

### 🎯 Objetivo

Reducir de horas a minutos el proceso manual de búsqueda y evaluación de ofertas laborales, mientras mantienes el control total de tus datos (sin servicios externos de pago).

## ✨ Características

### 🔍 Scraping Inteligente
- Múltiples portales soportados (LinkedIn, CompuTrabajo)
- Arquitectura modular para fácil adición de nuevos portales
- Rotación automática de User-Agents
- Delays estocásticos para evitar bloqueos
- Soporte opcional de proxies

### 🧠 Evaluación con IA
- Modelo local Qwen 2.5-3B via Ollama
- Evaluación 1:1 (una oferta por llamada para máxima precisión)
- Prompt optimizado para modelos de 3B parámetros
- Respuesta estructurada: score, resumen, skills coincidentes
- Validación de respuestas con fallback regex

### 📊 Gestión de Ofertas
- Cache local para evitar duplicados
- Deduplicación con Notion
- Scoring configurable (0-100)
- Alertas por Telegram para scores altos
- Resumen diario de actividad

### 🛡️ Anti-Baneos
- Delay estocástico (jitter) entre requests
- Rotación dinámica de User-Agents
- Configuración de delays personalizable
- Soporte futuro de proxies

### 🖥️ Interfaz de Usuario
- Menú interactivo en consola
- 7 opciones disponibles
- Configuración en tiempo de ejecución
- Feedback visual con emojis y formato

## 🏗️ Arquitectura

```
┌─────────────────────────────────────────────────────────────┐
│                    CareerSentinel                           │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  ┌─────────────┐    ┌─────────────┐    ┌─────────────┐    │
│  │   LinkedIn   │    │ CompuTrabajo│    │  (Futuros)  │    │
│  │   Scraper    │    │   Scraper   │    │   Scrapers  │    │
│  └──────┬──────┘    └──────┬──────┘    └──────┬──────┘    │
│         │                  │                  │             │
│         └──────────────────┼──────────────────┘             │
│                            │                                │
│                    ┌───────▼───────┐                        │
│                    │  IJobScraper  │  ← Interfaz genérica   │
│                    └───────┬───────┘                        │
│                            │                                │
│                    ┌───────▼───────┐                        │
│                    │ JobOrchestrator│  ← Motor principal    │
│                    └───────┬───────┘                        │
│                            │                                │
│         ┌──────────────────┼──────────────────┐             │
│         │                  │                  │             │
│  ┌──────▼──────┐    ┌──────▼──────┐    ┌──────▼──────┐    │
│  │   Ollama    │    │   Notion    │    │   Telegram  │    │
│  │  (Evaluar)  │    │  (Guardar)  │    │  (Alertar)  │    │
│  └─────────────┘    └─────────────┘    └─────────────┘    │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

### Patrones de Diseño Aplicados

| Patrón | Uso |
|--------|-----|
| **Strategy** | `IJobScraper` permite intercambiar algoritmos de scraping |
| **Dependency Injection** | Contenedor DI nativo de .NET 8 |
| **DelegatingHandler** | `AntiBotHttpClientHandler` para protección anti-baneos |
| **Options Pattern** | `IOptions<AppSettings>` para configuración tipada |
| **Background Service** | Arquitectura extensible para ejecución programada |

## 🛠️ Stack Tecnológico

| Componente | Tecnología | Propósito |
|------------|------------|-----------|
| **Runtime** | .NET 8 | Plataforma de ejecución |
| **Lenguaje** | C# 12 | Programación principal |
| **Scraping** | AngleSharp | Parsing de HTML |
| **HTTP** | HttpClient + Polly | Requests resilientes |
| **LLM** | Ollama + Qwen 2.5-3B | Evaluación de ofertas |
| **Almacenamiento** | Notion API | Persistencia de ofertas |
| **Alertas** | Telegram Bot API | Notificaciones push |
| **Configuración** | appsettings.json + User Secrets | Gestión de configuración |
| **Resiliencia** | Polly | Retry + Circuit Breaker |

## 📋 Requisitos Previos

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) o superior
- [Ollama](https://ollama.ai/) instalado y ejecutándose
- Modelo Qwen 2.5 descargado: `ollama pull qwen2.5:3b`
- Cuenta de Notion (opcional)
- Bot de Telegram (opcional)

## 🚀 Instalación

### 1. Clonar el repositorio

```bash
git clone https://github.com/tu-usuario/CareerSentinel.git
cd CareerSentinel
```

### 2. Instalar dependencias

```bash
cd src/CareerSentinel
dotnet restore
```

### 3. Configurar secrets

```bash
dotnet user-secrets init

# Notion (opcional)
dotnet user-secrets set "Notion:ApiKey" "tu-api-key-de-notion"
dotnet user-secrets set "Notion:DatabaseId" "tu-database-id"

# Telegram (opcional)
dotnet user-secrets set "Telegram:BotToken" "tu-token-de-bot"
dotnet user-secrets set "Telegram:ChatId" "tu-chat-id"
```

### 4. Ejecutar

```bash
dotnet run
```

## ⚙️ Configuración

### Archivo `appsettings.json`

```json
{
  "AppSettings": {
    "Ollama": {
      "BaseUrl": "http://localhost:11434",
      "ModelName": "qwen2.5:3b"
    },
    "JobSources": {
      "LinkedIn": {
        "Enabled": true,
        "Keywords": ["developer", ".NET", "C#"],
        "Location": "Colombia"
      },
      "CompuTrabajo": {
        "Enabled": false,
        "Keywords": ["developer", ".NET", "C#"],
        "Location": "Colombia"
      }
    },
    "Scoring": {
      "Threshold": 75,
      "MaxRetries": 3
    },
    "AntiBot": {
      "MinDelayMs": 3000,
      "MaxDelayMs": 7000,
      "EnableUserAgentRotation": true
    }
  }
}
```

### Variables de Entorno (User Secrets)

| Variable | Descripción | Requerido |
|----------|-------------|-----------|
| `Notion:ApiKey` | API Key de Notion | No |
| `Notion:DatabaseId` | ID de base de datos | No |
| `Telegram:BotToken` | Token del bot de Telegram | No |
| `Telegram:ChatId` | ID del chat destino | No |

## 💻 Uso

### Menú Principal

```
══════════════════════════════════════════
  CareerSentinel - Buscador de Empleo
══════════════════════════════════════════
  [1] Ejecutar búsqueda completa
  [2] Ver configuración actual
  [3] Configurar keywords
  [4] Configurar umbral de score
  [5] Ver fuentes habilitadas
  [6] Habilitar/deshabilitar fuentes
  [7] Salir
══════════════════════════════════════════
```

### Ejecutar Búsqueda

1. Selecciona opción `[1]`
2. El sistema iterará por cada fuente habilitada
3. Scraping de ofertas con rate limiting
4. Evaluación con IA local
5. Guardado en Notion + alertas por Telegram

## 📁 Estructura del Proyecto

```
CareerSentinel/
├── AGENTS.md                    # Reglas del sistema de agentes
├── README.md                    # Este archivo
├── task.md                      # Plan de desarrollo
├── .gitignore
├── CareerSentinel.sln
└── src/CareerSentinel/
    ├── CareerSentinel.csproj
    ├── Program.cs               # Entry point + DI
    ├── appsettings.json         # Configuración
    ├── Configuration/
    │   └── AppSettings.cs       # Modelos de configuración
    ├── Models/
    │   ├── JobOffer.cs          # DTO de oferta
    │   └── EvaluationResult.cs  # DTO de evaluación LLM
    ├── Services/
    │   ├── IJobScraper.cs       # Interfaz genérica de scraping
    │   ├── LinkedInScraper.cs   # Scraping de LinkedIn
    │   ├── CompuTrabajoScraper.cs # Scraping de CompuTrabajo
    │   ├── JobOrchestrator.cs   # Motor principal
    │   ├── ConsoleMenu.cs       # Interfaz de usuario
    │   ├── LocalLlmService.cs   # Evaluación con Ollama
    │   ├── NotionService.cs     # Persistencia en Notion
    │   ├── TelegramAlertService.cs # Alertas por Telegram
    │   ├── IJobCacheService.cs  # Interfaz de cache
    │   ├── JobCacheService.cs   # Implementación de cache
    │   └── AntiBotHttpClientHandler.cs # Protección anti-baneos
    └── Workers/
        └── JobScrapingWorker.cs # Background service (futuro)
```

## 🤖 Diseño de Agentes

El proyecto utiliza un sistema de **5 agentes** que trabajan en cadena para garantizar calidad de código:

```
USUARIO
   │
   ▼
@build ──── Orquestador primario
   │
   ├──→ @plan ──── Crea/actualiza task.md
   │
   ├──→ @code ──── Implementa código C#
   │
   ├──→ @review ── Audita y aprueba/rechaza
   │
   └──→ @explore ─ Busca info (solo lectura)
```

### Flujo de Desarrollo

1. **@build** recibe requerimientos del usuario
2. **@plan** crea tareas atómicas en `task.md`
3. **@code** implementa una tarea a la vez
4. **@review** audita el código
5. **@build** reporta resultados al usuario

## 📐 Decisiones de Arquitectura

### ¿Por qué Strategy + DI nativa?

En lugar de un Factory con switch/case, se utilizó la inyección de dependencias nativa de .NET 8 para registrar múltiples implementaciones de `IJobScraper`. Esto permite:

- **Open/Closed Principle**: Agregar nuevos scrapers sin modificar código existente
- **Testability**: Fácil de mockear para testing
- **Configuración dinámica**: Habilitar/deshabilitar fuentes en runtime

### ¿Por qué Qwen 2.5-3B?

- **Velocidad**: < 2 segundos por evaluación
- **VRAM**: ~2.5 GB (ejecutable en GPUs modestas)
- **Precisión**: Suficiente para scoring de ofertas
- **Privacidad**: Todo ejecuta localmente

### ¿Por qué Anti-Baneos?

Los portales de empleo detectan scraping mediante:
- Patrones de requests regulares
- User-Agents estáticos
- IP fija

La solución implementa:
- Delay estocástico (jitter)
- Rotación de User-Agents
- Soporte futuro de proxies

## 📊 Métricas de Rendimiento

| Métrica | Valor |
|---------|-------|
| **Build time** | ~1.2 segundos |
| **Tiempo de evaluación LLM** | < 2 segundos/oferta |
| **Memoria VRAM** | ~2.5 GB |
| **Tamaño DLL** | ~45 KB |

## 🔮 Futuras Mejoras

- [ ] Ejecución programada (cron jobs)
- [ ] Dashboard web para visualización
- [ ] Scraping de Indeed, Glassdoor
- [ ] Análisis de tendencias del mercado
- [ ] Exportación a CSV/Excel
- [ ] Modo headless para servidores

## 👨‍💻 Autor

**Tu Nombre**
- GitHub: [@tu-usuario](https://github.com/tu-usuario)
- LinkedIn: [Tu LinkedIn](https://linkedin.com/in/tu-perfil)
- Email: tu-email@ejemplo.com

## 📄 Licencia

Este proyecto está bajo la Licencia MIT - consulta el archivo [LICENSE](LICENSE) para detalles.

---

<p align="center">
  <b>⭐ Si este proyecto te fue útil, considera darle una estrella en GitHub ⭐</b>
</p>
