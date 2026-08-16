using System.Text.Json.Serialization;

namespace CareerSentinel.Models;

public record EvaluationResult
{
    [JsonPropertyName("score")]
    public int Score { get; init; }

    [JsonPropertyName("justification")]
    public string Justification { get; init; } = string.Empty;

    [JsonPropertyName("adapted_cv")]
    public string AdaptedCv { get; init; } = string.Empty;
}

