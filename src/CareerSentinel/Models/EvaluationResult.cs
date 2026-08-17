using System.Text.Json.Serialization;

namespace CareerSentinel.Models;

public record EvaluationResult
{
    [JsonPropertyName("score")]
    public int Score { get; init; }
    [JsonPropertyName("summary")]
    public string Summary { get; init; } = string.Empty;
    [JsonPropertyName("matching_skills")]
    public List<string> MatchingSkills { get; init; } = new();
}

