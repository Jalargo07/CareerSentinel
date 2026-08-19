using System.Text.Json;
using CareerSentinel.Configuration;
using Microsoft.Extensions.Logging;

namespace CareerSentinel.Services;

/// <summary>
/// Servicio dedicado para cargar, guardar y persistir configuración
/// en appsettings.json. Encapsula toda la lógica de serialización/persistencia
/// que antes estaba en ConsoleMenu y Program.
/// </summary>
public class ConfigurationService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    private readonly string _settingsPath;
    private readonly ILogger<ConfigurationService> _logger;

    public ConfigurationService(ILogger<ConfigurationService> logger)
    {
        _logger = logger;
        _settingsPath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
    }

    /// <summary>
    /// Ruta completa al archivo appsettings.json.
    /// </summary>
    public string SettingsPath => _settingsPath;

    /// <summary>
    /// Serializa y guarda la configuración actual en appsettings.json.
    /// </summary>
    public void Save(AppSettings settings)
    {
        try
        {
            var wrapper = new { AppSettings = settings };
            var json = JsonSerializer.Serialize(wrapper, JsonOptions);
            File.WriteAllText(_settingsPath, json);
            _logger.LogInformation("Configuración guardada en {Path}", _settingsPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al guardar configuración en {Path}", _settingsPath);
            throw;
        }
    }

    /// <summary>
    /// Carga y deserializa la configuración desde appsettings.json.
    /// Retorna null si el archivo no existe o no se puede leer.
    /// </summary>
    public AppSettings? Load()
    {
        if (!File.Exists(_settingsPath))
        {
            _logger.LogWarning("Archivo de configuración no encontrado: {Path}", _settingsPath);
            return null;
        }

        try
        {
            var json = File.ReadAllText(_settingsPath);
            var document = JsonDocument.Parse(json);

            if (document.RootElement.TryGetProperty("AppSettings", out var appSettingsElement))
            {
                var rawJson = appSettingsElement.GetRawText();
                return JsonSerializer.Deserialize<AppSettings>(rawJson, JsonOptions);
            }

            _logger.LogWarning("El archivo de configuración no contiene la sección 'AppSettings'");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al cargar configuración desde {Path}", _settingsPath);
            return null;
        }
    }

    /// <summary>
    /// Asegura que appsettings.json exista con valores por defecto.
    /// Usado para la experiencia de primera ejecución.
    /// </summary>
    public void EnsureDefaultExists()
    {
        if (File.Exists(_settingsPath)) return;

        var defaultConfig = @"{
  ""AppSettings"": {
    ""ProcessingMode"": ""API"",
    ""Ollama"": {
      ""BaseUrl"": ""http://localhost:11434"",
      ""ModelName"": ""qwen2.5:3b""
    },
    ""OpenCodeGo"": {
      ""BaseUrl"": ""https://generativelanguage.googleapis.com/v1beta/openai/"",
      ""ModelName"": ""gemini-3.5-flash-lite"",
      ""MaxConcurrentRequests"": 2,
      ""BatchSize"": 5,
      ""TimeoutSeconds"": 120,
      ""MaxTokensBatch"": 3500,
      ""ApiKey"": """"
    },
    ""LinkedIn"": {
      ""BaseUrl"": ""https://www.linkedin.com/jobs-guest/jobs/api/seeMoreJobPostings/search"",
      ""Location"": ""Argentina"",
      ""Keywords"": []
    },
    ""AntiBot"": {
      ""EnableUserAgentRotation"": true,
      ""MinDelayMs"": 1000,
      ""MaxDelayMs"": 3000
    },
    ""Candidate"": {
      ""Name"": ""Tu nombre"",
      ""Level"": ""Junior"",
      ""YearsExperience"": 2,
      ""CoreSkills"": [""Node.js"", ""JavaScript"", ""TypeScript"", ""Python""],
      ""PreferredModality"": ""Remoto"",
      ""PreferredRegions"": [""Colombia"", ""Latin America"", ""Europe""],
      ""CvDescription"": """"
    },
    ""JobSources"": {
      ""LinkedIn"": {
        ""Enabled"": true,
        ""Keywords"": [""Node.js"", ""JavaScript"", ""TypeScript"", ""Backend"", ""Full-Stack"", ""Python""],
        ""Location"": ""Medellin, Colombia""
      },
      ""CompuTrabajo"": {
        ""Enabled"": true,
        ""Keywords"": [""Node.js"", ""JavaScript"", ""TypeScript"", ""Backend"", ""Full-Stack"", ""Python""],
        ""Location"": ""Medellin, Colombia""
      }
    },
    ""Scoring"": {
      ""Threshold"": 75,
      ""MaxRetries"": 3
    },
    ""RateLimiting"": {
      ""DelayBetweenRequestsMs"": 2000,
      ""DelayBetweenSearchesMs"": 5000
    },
    ""Notion"": {
      ""ApiKey"": """",
      ""DatabaseId"": """"
    },
    ""Telegram"": {
      ""BotToken"": ""PLACEHOLDER_BOT_TOKEN"",
      ""ChatId"": """"
    }
  }
}";
        File.WriteAllText(_settingsPath, defaultConfig);
        _logger.LogInformation("appsettings.json creado con valores por defecto en {Path}", _settingsPath);
    }

    /// <summary>
    /// Actualiza el Chat ID de Telegram y guarda la configuración.
    /// </summary>
    public void UpdateTelegramChatId(AppSettings settings, string chatId)
    {
        settings.Telegram.ChatId = chatId;
        Save(settings);
    }

    /// <summary>
    /// Actualiza la API Key del proveedor LLM y guarda la configuración.
    /// </summary>
    public void UpdateApiKey(AppSettings settings, string apiKey)
    {
        settings.OpenCodeGo.ApiKey = apiKey;
        Save(settings);
    }

    /// <summary>
    /// Actualiza el threshold de scoring y guarda la configuración.
    /// </summary>
    public void UpdateThreshold(AppSettings settings, int threshold)
    {
        settings.Scoring.Threshold = threshold;
        Save(settings);
    }

    /// <summary>
    /// Actualiza el perfil del candidato y guarda la configuración.
    /// </summary>
    public void UpdateCandidateProfile(AppSettings settings, CandidateProfile profile)
    {
        settings.Candidate = profile;
        Save(settings);
    }

    /// <summary>
    /// Actualiza la configuración del LLM y guarda.
    /// </summary>
    public void UpdateLlmConfig(AppSettings settings, OpenCodeGoSettings llmSettings, ProcessingMode mode)
    {
        settings.OpenCodeGo = llmSettings;
        settings.ProcessingMode = mode;
        Save(settings);
    }

    /// <summary>
    /// Actualiza las fuentes de empleo y guarda la configuración.
    /// </summary>
    public void UpdateJobSources(AppSettings settings, Dictionary<string, JobSourceSettings> sources)
    {
        settings.JobSources = sources;
        Save(settings);
    }
}
