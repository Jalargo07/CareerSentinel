namespace CareerSentinel.Configuration;

public enum ProcessingMode
{
    Local,
    API,
    Hybrid
}

public class AppSettings
{
    public OllamaSettings Ollama { get; set; } = new();
    public LinkedInSettings LinkedIn { get; set; } = new();
    public NotionSettings Notion { get; set; } = new();
    public TelegramSettings Telegram { get; set; } = new();
    public ScoringSettings Scoring { get; set; } = new();
    public RateLimitSettings RateLimiting { get; set; } = new();
    public AntiBotSettings AntiBot { get; set; } = new();
    public Dictionary<string, JobSourceSettings> JobSources { get; set; } = new();
    public CandidateProfile Candidate { get; set; } = new();
    public ProcessingMode ProcessingMode { get; set; } = ProcessingMode.Local;
    public OpenCodeGoSettings OpenCodeGo { get; set; } = new();
}

public class OpenCodeGoSettings
{
    public string BaseUrl { get; set; } = "https://api.opencode.ai/v1";
    public string ModelName { get; set; } = "opencode-go/mimo-v2.5";
    public int MaxConcurrentRequests { get; set; } = 2;
    public int BatchSize { get; set; } = 5;
    public int TimeoutSeconds { get; set; } = 120;
    public int MaxTokensBatch { get; set; } = 3500;
    public string ApiKey { get; set; } = string.Empty;
}

public class AntiBotSettings
{
    public bool EnableUserAgentRotation { get; set; } = true;
    public int MinDelayMs { get; set; } = 1000;
    public int MaxDelayMs { get; set; } = 3000;
    public string? ProxyUrl { get; set; }
}

public class OllamaSettings
{
    public string BaseUrl { get; set; } = "http://localhost:11434";
    public string ModelName { get; set; } = "qwen2.5:3b";
}

public class LinkedInSettings
{
    public string BaseUrl { get; set; } = "https://www.linkedin.com/jobs-guest/jobs/api/seeMoreJobPostings/search";
    public string Location { get; set; } = "Argentina";
    public List<string> Keywords { get; set; } = [];
}

public class NotionSettings
{
    public string ApiKey { get; set; } = string.Empty;
    public string DatabaseId { get; set; } = string.Empty;
}

public class TelegramSettings
{
    public string BotToken { get; set; } = string.Empty;
    public string ChatId { get; set; } = string.Empty;
}

public class ScoringSettings
{
    public int Threshold { get; set; } = 85;
    public int MaxRetries { get; set; } = 3;
    public string CvText { get; set; } = string.Empty;
}

public class RateLimitSettings
{
    public int DelayBetweenRequestsMs { get; set; } = 3000;
    public int DelayBetweenSearchesMs { get; set; } = 5000;
}

public class JobSourceSettings
{
    public string Name { get; set; } = string.Empty;
    public bool Enabled { get; set; } = true;
    public string BaseUrl { get; set; } = string.Empty;
    public List<string> Keywords { get; set; } = new();
    public string Location { get; set; } = string.Empty;
}

public class CandidateProfile
{
    public string Name { get; set; } = string.Empty;
    public string Level { get; set; } = "Junior"; // Junior, Mid, Senior
    public int YearsExperience { get; set; } = 2;
    public List<string> CoreSkills { get; set; } = new();
    public string PreferredModality { get; set; } = "Remote"; // Remote, OnSite, Hybrid, Any
    public List<string> PreferredRegions { get; set; } = new() { "Colombia", "Latin America", "Europe" };
}