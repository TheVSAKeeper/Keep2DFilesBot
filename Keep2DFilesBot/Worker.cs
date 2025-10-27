using Keep2DFilesBot.Features.Commands;
using Keep2DFilesBot.Features.DownloadFile;
using Keep2DFilesBot.Shared.Models;
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Keep2DFilesBot;

public sealed class Worker(
    ITelegramBotClient botClient,
    IServiceProvider serviceProvider,
    CommandRouter commandRouter,
    ILogger<Worker> logger) : BackgroundService
{
    private readonly ITelegramBotClient _botClient = botClient;
    private readonly IServiceProvider _serviceProvider = serviceProvider;
    private readonly CommandRouter _commandRouter = commandRouter;
    private readonly ILogger<Worker> _logger = logger;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation("Запущено получение обновлений Telegram");
        }

        var receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = new[] { UpdateType.Message }
        };

        _botClient.StartReceiving(
            HandleUpdateAsync,
            HandleErrorAsync,
            receiverOptions,
            stoppingToken);

        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
        }
    }

    private async Task HandleUpdateAsync(ITelegramBotClient client, Update update, CancellationToken ct)
    {
        if (update.Message is not { } message)
            return;

        if (message.Text is null)
            return;

        if (message.Text.StartsWith('/'))
        {
            await HandleCommandAsync(client, message, ct);
            return;
        }

        if (!IsDownloadCandidate(message.Text))
        {
            await client.SendMessage(
                chatId: message.Chat.Id,
                text: "Отправьте прямую ссылку на файл (HTTP/HTTPS)",
                cancellationToken: ct);
            return;
        }

        if (message.From is null)
            return;

        using var scope = _serviceProvider.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<DownloadFileHandler>();

        var command = new DownloadFileCommand
        {
            Url = message.Text,
            UserId = (UserId)message.From.Id,
            ChatId = message.Chat.Id,
            MessageId = message.MessageId
        };

        var result = await handler.HandleAsync(command, ct);

        if (result.IsSuccess)
        {
            var metadata = result.Value!;
            await client.SendMessage(
                chatId: message.Chat.Id,
                text: $"✅ Файл сохранён\nИмя: {metadata.FileName}\nРазмер: {metadata.FormattedSize}",
                cancellationToken: ct);
        }
        else
        {
            await client.SendMessage(
                chatId: message.Chat.Id,
                text: $"❌ Ошибка: {result.Error}",
                cancellationToken: ct);
        }
    }

    private async Task HandleCommandAsync(ITelegramBotClient client, Message message, CancellationToken ct)
    {
        var parts = message.Text!
            .Split(' ', StringSplitOptions.RemoveEmptyEntries);

        var command = parts.Length > 0 ? parts[0].ToLowerInvariant() : string.Empty;
        var arguments = parts.Length > 1 ? parts.Skip(1).ToArray() : Array.Empty<string>();

        var context = new CommandContext(
            client,
            message,
            command,
            arguments);

        var result = await _commandRouter.RouteAsync(context, ct);

        if (result.IsSuccess && result.Payload is { } payload)
        {
            await client.SendMessage(
                chatId: message.Chat.Id,
                text: payload.Text,
                cancellationToken: ct);
            return;
        }

        if (result.IsFailure && result.Error is { Length: > 0 })
        {
            await client.SendMessage(
                chatId: message.Chat.Id,
                text: result.Error,
                cancellationToken: ct);
        }
    }

    private Task HandleErrorAsync(ITelegramBotClient client, Exception exception, CancellationToken ct)
    {
        var errorMessage = exception switch
        {
            ApiRequestException apiRequestException =>
                $"Ошибка Telegram API: {apiRequestException.ErrorCode} — {apiRequestException.Message}",
            _ => exception.Message
        };

        _logger.LogError(exception, "Ошибка получения обновлений Telegram: {Message}", errorMessage);
        return Task.CompletedTask;
    }

    private static bool IsDownloadCandidate(string text)
    {
        return Url.Create(text).IsSuccess;
    }
}
