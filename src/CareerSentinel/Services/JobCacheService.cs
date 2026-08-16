using System.Text.Json;

namespace CareerSentinel.Services;

public class JobCacheService : IJobCacheService
{
    private readonly string _cacheFilePath;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private HashSet<string> _seenIds = [];
    private bool _loaded;

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public JobCacheService(string? cacheFilePath = null)
    {
        _cacheFilePath = cacheFilePath ?? Path.Combine(AppContext.BaseDirectory, "seen_jobs.json");
    }

    public async Task<HashSet<string>> GetSeenIdsAsync(CancellationToken ct = default)
    {
        await EnsureLoadedAsync(ct);
        return [.. _seenIds];
    }

    public async Task AddSeenIdAsync(string id, CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct);
        try
        {
            await EnsureLoadedAsync(ct);

            if (_seenIds.Add(id))
            {
                await SaveAsync(ct);
            }
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<bool> ContainsAsync(string id, CancellationToken ct = default)
    {
        await EnsureLoadedAsync(ct);
        return _seenIds.Contains(id);
    }

    private async Task EnsureLoadedAsync(CancellationToken ct)
    {
        if (_loaded) return;

        if (File.Exists(_cacheFilePath))
        {
            var json = await File.ReadAllTextAsync(_cacheFilePath, ct);
            _seenIds = JsonSerializer.Deserialize<HashSet<string>>(json, JsonOptions) ?? [];
        }
        else
        {
            _seenIds = [];
        }

        _loaded = true;
    }

    private async Task SaveAsync(CancellationToken ct)
    {
        var json = JsonSerializer.Serialize(_seenIds, JsonOptions);
        await File.WriteAllTextAsync(_cacheFilePath, json, ct);
    }
}

