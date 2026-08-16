using CareerSentinel.Configuration;
using CareerSentinel.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;

namespace CareerSentinel.Services;

public class TelegramAlertService
{
    private readonly TelegramBotClient _botClient;
    private readonly ILogger<TelegramAlertService> _logger;
    private readonly long _chatId;

    public TelegramAlertService(IOptions<AppSettings> settings, ILogger<TelegramAlertService> logger)
    {
        _logger = logger;
        var telegramSettings = settings.Value.Telegram;
        _botClient = new TelegramBotClient(telegramSettings.BotToken);
        _chatId = long.Parse(telegramSettings.ChatId);
    }

    public async Task SendAlertAsync(JobOffer job, EvaluationResult evaluation, CancellationToken ct = default)
    {
        var message = $"""
             Nuevo match fuerte encontrado

             Puesto: {job.Title}
             Empresa: {job.Company}
             Ubicación: {job.Location}
             Score: {evaluation.Score}/100
             Keywords: {job.SourceKeyword}

             Justificación:
             {evaluation.Justification}

             CV Adaptado:
             {evaluation.AdaptedCv}

             Ver oferta: {job.Url}
             """;

        try
        {
            await _botClient.SendMessage(
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

        var topMatches = matches.OrderByDescending(m => m.Result.Score).Take(10).ToList();

        var message = new System.Text.StringBuilder();
        message.AppendLine($"Resumen diario - {topMatches.Count} matches encontrados");
        message.AppendLine();

        foreach (var (job, result) in topMatches)
        {
            message.AppendLine($"â€¢ {job.Title} @ {job.Company} (Score: {result.Score})");
            message.AppendLine($"  {result.Justification}");
            message.AppendLine();
        }

        try
        {
            await _botClient.SendMessage(
                _chatId,
                message.ToString(),
                ParseMode.Markdown,
                cancellationToken: ct);

            _logger.LogInformation("Resumen diario enviado por Telegram con {Count} matches", topMatches.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al enviar resumen diario por Telegram");
        }
    }
}

