using System.Text.Json.Serialization;

namespace CareerSentinel.Models;

public record EvaluationResult
{
    [JsonPropertyName("score")]
    public int Score { get; init; }
    
    [JsonPropertyName("match")]
    public bool Match { get; init; }
    
    [JsonPropertyName("cumple")]
    public List<string> Cumple { get; init; } = new();
    
    [JsonPropertyName("no_cumple")]
    public List<string> NoCumple { get; init; } = new();
    
    [JsonPropertyName("razon")]
    public string Razon { get; init; } = string.Empty;
}
