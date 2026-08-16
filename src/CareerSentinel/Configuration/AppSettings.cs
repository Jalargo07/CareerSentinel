namespace CareerSentinel.Configuration;

public class AppSettings
{
    public OllamaSettings Ollama { get; set; } = new();
    public LinkedInSettings LinkedIn { get; set; } = new();
    public NotionSettings Notion { get; set; } = new();
    public TelegramSettings Telegram { get; set; } = new();
    public ScoringSettings Scoring { get; set; } = new();
    public RateLimitSettings RateLimiting { get; set; } = new();
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