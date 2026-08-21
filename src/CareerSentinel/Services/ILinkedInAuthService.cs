using CareerSentinel.Models;

namespace CareerSentinel.Services;

/// <summary>
/// Service for managing LinkedIn authentication via Playwright browser cookies.
/// </summary>
public interface ILinkedInAuthService
{
    /// <summary>
    /// Ensures the user is authenticated. If not, launches a browser for manual login.
    /// </summary>
    Task EnsureAuthenticatedAsync(CancellationToken ct = default);

    /// <summary>
    /// Returns the current set of valid LinkedIn cookies.
    /// </summary>
    Task<IReadOnlyList<CookieData>> GetCookiesAsync(CancellationToken ct = default);

    /// <summary>
    /// Checks whether the stored cookies are still valid for LinkedIn.
    /// </summary>
    Task<bool> IsAuthenticatedAsync(CancellationToken ct = default);
}
