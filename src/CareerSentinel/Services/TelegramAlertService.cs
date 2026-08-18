using CareerSentinel.Configuration;
using CareerSentinel.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;

namespace CareerSentinel.Services;

public class TelegramAlertService
{
    private readonly Lazy<TelegramBotClient?> _botClientLazy;
    private readonly ILogger<TelegramAlertService> _logger;
    private readonly long _chatId;
    private readonly bool _isConfigured;

    private const string PlaceholderToken = "PLACEHOLDER_BOT_TOKEN";

    public TelegramAlertService(IOptions<AppSettings> settings, ILogger<TelegramAlertService> logger)
    {
        _logger = logger;
        var telegramSettings = settings.Value.Telegram;

        var token = telegramSettings.BotToken;
        var chatIdRaw = telegramSettings.ChatId;

        if (string.IsNullOrWhiteSpace(token)
            || token.Equals(PlaceholderToken, StringComparison.OrdinalIgnoreCase)
            || string.IsNullOrWhiteSpace(chatIdRaw))
        {
            _isConfigured = false;
            _chatId = 0;
            _logger.LogWarning(
                "Telegram no esta configurado. BotToken o ChatId vacios o con valores por defecto. " +
                "Las alertas de Telegram no se enviaran.");
        }
        else
        {
            _isConfigured = long.TryParse(chatIdRaw, out var parsedChatId) && parsedChatId > 0;
            _chatId = parsedChatId;

            if (!_isConfigured)
            {
                _logger.LogWarning(
                    "El ChatId de Telegram no es un numero valido: '{ChatId}'. " +
                    "Las alertas de Telegram no se enviaran.", chatIdRaw);
            }
        }

        _botClientLazy = new Lazy<TelegramBotClient?>(() =>
        {
            if (!_isConfigured || string.IsNullOrWhiteSpace(token))
                return null;

            return new TelegramBotClient(token);
        });
    }

    public async Task SendAlertAsync(JobOffer job, EvaluationResult evaluation, CancellationToken ct = default)
    {
        if (!_isConfigured)
        {
            _logger.LogWarning("Telegram ChatId no configurado, saltando alerta");
            return;
        }

        var botClient = _botClientLazy.Value;
        if (botClient is null)
        {
            _logger.LogWarning("Telegram no configurado. Se omite envio de alerta para {Title}.", job.Title);
            return;
        }

        var matchIcon = evaluation.Match ? "✅ Match" : "❌ No Match";
        var cumpleItems = evaluation.Cumple.Count > 0
            ? string.Join("\n• ", evaluation.Cumple)
            : "(ninguno identificado)";
        var noCumpleItems = evaluation.NoCumple.Count > 0
            ? string.Join("\n• ", evaluation.NoCumple)
            : "(ninguno identificado)";

        var message = $"""
             📋 {job.Title} @ {job.Company}
             📊 Score: {evaluation.Score}/100 | {matchIcon}

             ✅ CUMPLE:
             • {cumpleItems}

             ❌ NO CUMPLE:
             • {noCumpleItems}

             💬 {evaluation.Razon}

             🔗 {job.Url}
             """;

        try
        {
            await botClient.SendMessage(
                _chatId,
                message,
                ParseMode.Markdown,
                cancellationToken: ct);

            _logger.LogInformation("Alerta enviada por Telegram: {Title} (Score: {Score})", job.Title, evaluation.Score);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al enviar alerta por Telegram para {Title}", job.Title);
        }
    }

    public async Task SendDailySummaryAsync(List<(JobOffer Job, EvaluationResult Result)> matches, CancellationToken ct = default)
    {
        if (matches.Count == 0) return;

        if (!_isConfigured)
        {
            _logger.LogWarning("Telegram ChatId no configurado, saltando resumen diario");
            return;
        }

        var botClient = _botClientLazy.Value;
        if (botClient is null)
        {
            _logger.LogWarning("Telegram no configurado. Se omite envio de resumen diario con {Count} matches.", matches.Count);
            return;
        }

        var topMatches = matches.OrderByDescending(m => m.Result.Score).Take(10).ToList();

        var message = new System.Text.StringBuilder();
        message.AppendLine($"Resumen diario - {topMatches.Count} matches encontrados");
        message.AppendLine();

        foreach (var (job, result) in topMatches)
        {
            message.AppendLine($"* {job.Title} @ {job.Company} (Score: {result.Score})");
            message.AppendLine($"  {result.Razon}");
            message.AppendLine();
        }

        try
        {
            await botClient.SendMessage(
                _chatId,
                message.ToString(),
                cancellationToken: ct);

            _logger.LogInformation("Resumen diario enviado por Telegram con {Count} matches", topMatches.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al enviar resumen diario por Telegram");
        }
    }
}
