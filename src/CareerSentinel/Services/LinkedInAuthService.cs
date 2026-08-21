using Microsoft.Playwright;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using CareerSentinel.Configuration;

namespace CareerSentinel.Services;

/// <summary>
/// Service for managing LinkedIn authentication via Playwright browser cookies.
/// Handles cookie persistence and manual login flow.
/// </summary>
public class LinkedInAuthService : ILinkedInAuthService
{
    private const int LoginTimeoutMinutes = 2;

    private readonly ILogger<LinkedInAuthService> _logger;
    private readonly LinkedInSettings _settings;
    private readonly CookiesManager _cookiesManager;
    private IPlaywright? _playwright;
    private IBrowser? _browser;

    public LinkedInAuthService(
        ILogger<LinkedInAuthService> logger,
        IOptions<AppSettings> settings,
        CookiesManager cookiesManager)
    {
        _logger = logger;
        _settings = settings.Value.LinkedIn;
        _cookiesManager = cookiesManager;
    }

    /// <inheritdoc />
    public async Task EnsureAuthenticatedAsync(CancellationToken ct = default)
    {
        // 1. Intentar cargar cookies guardadas
        if (_cookiesManager.Exists(_settings.CookiesPath))
        {
            _logger.LogInformation("Cookies encontradas, verificando autenticacion...");
            if (await IsAuthenticatedAsync(ct))
            {
                _logger.LogInformation("Sesion de LinkedIn valida, usando cookies guardadas");
                return;
            }
            _logger.LogWarning("Cookies invalidas o expiradas, re-autenticando...");
        }

        // 2. Abrir navegador para autenticacion manual
        try
        {
            await OpenBrowserForLoginAsync(ct);
        }
        catch (PlaywrightException ex)
        {
            // Playwright or Chromium not installed — operate in no-cookie mode
            _logger.LogWarning(ex,
                "Chromium no disponible, usando modo sin cookies. " +
                "Las funcionalidades que requieren autenticacion no estaran disponibles.");
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<CookieData>> GetCookiesAsync(CancellationToken ct = default)
    {
        if (!_cookiesManager.Exists(_settings.CookiesPath))
        {
            return [];
        }

        return await _cookiesManager.LoadCookiesAsync(_settings.CookiesPath);
    }

    /// <inheritdoc />
    public async Task<bool> IsAuthenticatedAsync(CancellationToken ct = default)
    {
        if (!_cookiesManager.Exists(_settings.CookiesPath))
            return false;

        try
        {
            // Usar Playwright para verificar cookies
            if (!await EnsurePlaywrightAsync())
                return false;

            var context = await _browser!.NewContextAsync();
            var cookies = await _cookiesManager.LoadCookiesAsync(_settings.CookiesPath);

            // Convert stored cookies to Playwright cookies
            var playwrightCookies = cookies.Select(c => new Microsoft.Playwright.Cookie
            {
                Name = c.Name,
                Value = c.Value,
                Domain = c.Domain,
                Path = c.Path,
                Expires = c.Expires.HasValue
                    ? (float)(c.Expires.Value - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds
                    : 0f
            });

            await context.AddCookiesAsync(playwrightCookies);

            var page = await context.NewPageAsync();
            await page.GotoAsync("https://www.linkedin.com/feed");

            // Wait up to 5s for navigation to settle
            try
            {
                await page.WaitForLoadStateAsync(LoadState.NetworkIdle,
                    new PageWaitForLoadStateOptions { Timeout = 5000 });
            }
            catch
            {
                // Timeout is acceptable - we only need the final URL
            }

            var url = page.Url;
            var isLoggedIn = !url.Contains("authwall") && !url.Contains("login");

            await context.DisposeAsync();
            await page.DisposeAsync();

            return isLoggedIn;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error verificando autenticacion");
            return false;
        }
    }

    private async Task OpenBrowserForLoginAsync(CancellationToken ct)
    {
        _logger.LogInformation("Abriendo navegador para login manual...");
        _logger.LogInformation("Por favor inicia sesion en LinkedIn. Tienes {Minutes} minutos.", LoginTimeoutMinutes);

        if (!await EnsurePlaywrightAsync())
        {
            return;
        }

        // Abrir Chromium headed (visible)
        _browser = await _playwright!.Chromium.LaunchAsync(
            new BrowserTypeLaunchOptions
            {
                Headless = false,
                Args = new[] { "--disable-blink-features=AutomationControlled" }
            });

        var context = await _browser.NewContextAsync();
        var page = await context.NewPageAsync();

        // Navegar a LinkedIn
        await page.GotoAsync("https://www.linkedin.com/login");

        // Esperar hasta N minutos a que el usuario haga login
        var deadline = DateTime.Now.AddMinutes(LoginTimeoutMinutes);
        var lastUrl = page.Url;

        while (DateTime.Now < deadline && !ct.IsCancellationRequested)
        {
            var currentUrl = page.Url;

            // Verificar si el usuario ya no esta en la pagina de login
            if (!currentUrl.Contains("login") && !currentUrl.Contains("authwall"))
                break;

            // Detectar si el usuario cerro el navegador (Playwright lanza excepcion al navegar)
            if (IsPageClosed(page))
            {
                _logger.LogWarning("El usuario cerro el navegador. Reintentando...");
                await CleanupBrowserAsync(context, page);
                throw new OperationCanceledException("El navegador fue cerrado. Por favor, intenta de nuevo.");
            }

            lastUrl = currentUrl;
            await Task.Delay(1000, ct);
        }

        // Verificar si se agoto el tiempo
        if (page.Url.Contains("login") || page.Url.Contains("authwall"))
        {
            _logger.LogWarning("Tiempo de login agotado ({Minutes} minutos)", LoginTimeoutMinutes);
            await CleanupBrowserAsync(context, page);
            throw new OperationCanceledException(
                $"Tiempo de login agotado ({LoginTimeoutMinutes} minutos). Intenta de nuevo con la opcion [2].");
        }

        // Extraer cookies del navegador
        var playwrightCookies = await context.CookiesAsync();

        // Convertir a CookieData del Services namespace (lo que CookiesManager espera)
        var cookieDataList = playwrightCookies
            .Select(c => new CookieData
            {
                Name = c.Name,
                Value = c.Value,
                Domain = c.Domain ?? ".linkedin.com",
                Path = c.Path ?? "/",
                Expires = c.Expires > 0
                    ? DateTimeOffset.FromUnixTimeSeconds((long)c.Expires).UtcDateTime
                    : (DateTime?)null
            })
            .ToList();

        // Guardar cookies
        await _cookiesManager.SaveCookiesAsync(_settings.CookiesPath, cookieDataList);
        _logger.LogInformation("Cookies guardadas exitosamente en {Path}", _settings.CookiesPath);

        // Cleanup
        await CleanupBrowserAsync(context, page);
    }

    /// <summary>
    /// Checks if a Playwright page has been closed by attempting a safe access.
    /// Returns true if the page is no longer usable.
    /// </summary>
    private static bool IsPageClosed(IPage page)
    {
        try
        {
            _ = page.Url;
            return false;
        }
        catch
        {
            return true;
        }
    }

    /// <summary>
    /// Safely disposes browser resources, catching and logging any disposal errors.
    /// </summary>
    private async Task CleanupBrowserAsync(IBrowserContext context, IPage page)
    {
        try
        {
            await page.DisposeAsync();
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error disposing page (expected if browser was closed)");
        }

        try
        {
            await context.DisposeAsync();
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error disposing browser context (expected if browser was closed)");
        }

        try
        {
            if (_browser != null)
            {
                await _browser.DisposeAsync();
                _browser = null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error disposing browser (expected if browser was closed)");
            _browser = null;
        }
    }

    /// <summary>
    /// Initializes Playwright and verifies Chromium is available.
    /// Returns true if Playwright is ready to use, false otherwise.
    /// </summary>
    private async Task<bool> EnsurePlaywrightAsync()
    {
        if (_playwright != null)
            return true;

        try
        {
            _playwright = await Playwright.CreateAsync();
        }
        catch (DllNotFoundException ex)
        {
            _logger.LogError(ex, "Playwright no esta instalado");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("\n╔══════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║  Playwright no esta instalado.                              ║");
            Console.WriteLine("║                                                             ║");
            Console.WriteLine("║  Ejecuta el siguiente comando para instalarlo:              ║");
            Console.WriteLine("║  pwsh -Command \"playwright install chromium\"                ║");
            Console.WriteLine("║                                                             ║");
            Console.WriteLine("║  Las funcionalidades que requieren autenticacion de         ║");
            Console.WriteLine("║  LinkedIn no estaran disponibles hasta que instales         ║");
            Console.WriteLine("║  Playwright.                                                ║");
            Console.WriteLine("╚══════════════════════════════════════════════════════════════╝\n");
            Console.ResetColor();
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inicializando Playwright");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("\n╔══════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║  Playwright no esta instalado.                              ║");
            Console.WriteLine("║                                                             ║");
            Console.WriteLine("║  Ejecuta el siguiente comando para instalarlo:              ║");
            Console.WriteLine("║  pwsh -Command \"playwright install chromium\"                ║");
            Console.WriteLine("╚══════════════════════════════════════════════════════════════╝\n");
            Console.ResetColor();
            return false;
        }

        // Verificar si Chromium esta instalado, si no dar instrucciones al usuario
        try
        {
            var testBrowser = await _playwright.Chromium.LaunchAsync(
                new BrowserTypeLaunchOptions { Headless = true });
            await testBrowser.DisposeAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Chromium no esta instalado. Ejecuta: pwsh -Command \"playwright install chromium\"");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("\n╔══════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║  Chromium no esta instalado.                                ║");
            Console.WriteLine("║                                                             ║");
            Console.WriteLine("║  Ejecuta el siguiente comando para instalarlo:              ║");
            Console.WriteLine("║  pwsh -Command \"playwright install chromium\"                ║");
            Console.WriteLine("║                                                             ║");
            Console.WriteLine("║  Las funcionalidades que requieren autenticacion de         ║");
            Console.WriteLine("║  LinkedIn no estaran disponibles hasta que instales         ║");
            Console.WriteLine("║  Chromium.                                                  ║");
            Console.WriteLine("╚══════════════════════════════════════════════════════════════╝\n");
            Console.ResetColor();

            // Cleanup Playwright instance since Chromium is not usable
            try
            {
                _playwright.Dispose();
            }
            catch
            {
                // Best effort cleanup
            }
            _playwright = null;
            return false;
        }
    }
}
