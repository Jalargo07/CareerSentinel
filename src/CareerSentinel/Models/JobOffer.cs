using System.Text.Json.Serialization;

namespace CareerSentinel.Models;

public record JobOffer
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("title")]
    public string Title { get; init; } = string.Empty;

    [JsonPropertyName("company")]
    public string Company { get; init; } = string.Empty;

    [JsonPropertyName("url")]
    public string Url { get; init; } = string.Empty;

    [JsonPropertyName("location")]
    public string Location { get; init; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; init; } = string.Empty;

    [JsonPropertyName("postedDate")]
    public string PostedDate { get; init; } = string.Empty;

    [JsonPropertyName("sourceKeyword")]
    public string SourceKeyword { get; init; } = string.Empty;
}

