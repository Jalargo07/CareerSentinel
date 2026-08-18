using System.Text.Json.Serialization;

namespace CareerSentinel.Models;

public record BatchEvaluationRequest
{
    public List<OfferToEvaluate> Offers { get; init; } = new();
    public CandidateInfo Candidate { get; init; } = new();
}

public record OfferToEvaluate
{
    public string Title { get; init; } = string.Empty;
    public string Company { get; init; } = string.Empty;
    public string Modality { get; init; } = string.Empty;
    public string Location { get; init; } = string.Empty;
    public string Seniority { get; init; } = string.Empty;
    public string ExperienceYears { get; init; } = string.Empty;
    public List<string> Technologies { get; init; } = new();
    public string Description { get; init; } = string.Empty;

    [JsonPropertyName("indice")]
    public int Indice { get; init; }
}

public record CandidateInfo
{
    public string Level { get; init; } = string.Empty;
    public string YearsExperience { get; init; } = string.Empty;
    public string PreferredModality { get; init; } = string.Empty;
    public List<string> PreferredRegions { get; init; } = new();
    public List<string> Skills { get; init; } = new();
}
