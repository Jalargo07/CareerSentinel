namespace CareerSentinel.Services;

public interface IJobCacheService
{
    Task<HashSet<string>> GetSeenIdsAsync(CancellationToken ct = default);
    Task AddSeenIdAsync(string id, CancellationToken ct = default);
    Task<bool> ContainsAsync(string id, CancellationToken ct = default);
}

