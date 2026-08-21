using System.Text.Json;
using System.Text.Json.Serialization;

namespace CareerSentinel.Services;

public record CookieData
{
    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("value")]
    public string Value { get; init; } = string.Empty;

    [JsonPropertyName("domain")]
    public string Domain { get; init; } = string.Empty;

    [JsonPropertyName("path")]
    public string Path { get; init; } = string.Empty;

    [JsonPropertyName("expires")]
    public DateTime? Expires { get; init; }
}

public class CookiesManager
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public async Task SaveCookiesAsync(string path, IReadOnlyList<CookieData> cookies)
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var json = JsonSerializer.Serialize(cookies, JsonOptions);
        await File.WriteAllTextAsync(path, json);
    }

    public async Task<List<CookieData>> LoadCookiesAsync(string path)
    {
        if (!File.Exists(path))
        {
            return [];
        }

        var json = await File.ReadAllTextAsync(path);

        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        return JsonSerializer.Deserialize<List<CookieData>>(json, JsonOptions) ?? [];
    }

    public bool Exists(string path)
    {
        return File.Exists(path);
    }

    public void Delete(string path)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }
}
