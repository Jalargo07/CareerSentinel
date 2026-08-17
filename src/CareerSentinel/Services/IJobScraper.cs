using CareerSentinel.Models;

namespace CareerSentinel.Services;

public interface IJobScraper
{
    string PortalName { get; }
    Task<List<JobOffer>> SearchAsync(string keyword, string location, CancellationToken ct = default);
    Task<string> GetDescriptionAsync(string jobUrl, CancellationToken ct = default);
}
