using CareerSentinel.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CareerSentinel.Services;

public class AntiBotHttpClientHandler : DelegatingHandler
{
    private readonly AntiBotSettings _settings;
    private readonly ILogger<AntiBotHttpClientHandler> _logger;
    
    private static readonly string[] UserAgents = new[]
    {
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36",
        "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36",
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:133.0) Gecko/20100101 Firefox/133.0",
        "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36",
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/130.0.0.0 Safari/537.36 Edg/130.0.0.0",
    };

    private static readonly Random Rng = new();

    public AntiBotHttpClientHandler(IOptions<AppSettings> settings, ILogger<AntiBotHttpClientHandler> logger)
    {
        _settings = settings.Value.AntiBot;
        _logger = logger;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // 1. Delay estocastico (jitter)
        if (_settings.MinDelayMs > 0 && _settings.MaxDelayMs > _settings.MinDelayMs)
        {
            var delay = Random.Shared.Next(_settings.MinDelayMs, _settings.MaxDelayMs);
            _logger.LogDebug("AntiBot delay: {Delay}ms", delay);
            await Task.Delay(delay, cancellationToken);
        }

        // 2. Rotacion de User-Agent
        if (_settings.EnableUserAgentRotation)
        {
            request.Headers.Remove("User-Agent");
            request.Headers.Add("User-Agent", UserAgents[Rng.Next(UserAgents.Length)]);
        }

        // 3. Proxy (si esta habilitado)
        // El proxy se configura a nivel de HttpClientFactory, no aqui

        return await base.SendAsync(request, cancellationToken);
    }
}
