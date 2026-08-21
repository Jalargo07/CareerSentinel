using System.Text.Json.Serialization;

namespace CareerSentinel.Models;

/// <summary>
/// Represents a browser cookie for LinkedIn authentication.
/// </summary>
public record CookieData
{
    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("value")]
    public string Value { get; init; } = string.Empty;

    [JsonPropertyName("domain")]
    public string Domain { get; init; } = string.Empty;

    [JsonPropertyName("path")]
    public string Path { get; init; } = "/";

    [JsonPropertyName("expires")]
    public double? Expires { get; init; }

    [JsonPropertyName("httpOnly")]
    public bool HttpOnly { get; init; }

    [JsonPropertyName("secure")]
    public bool Secure { get; init; }

    [JsonPropertyName("sameSite")]
    public string SameSite { get; init; } = "Lax";
}
