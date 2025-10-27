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
    ILogger<Worker> logger) : BackgroundService
{
    private readonly ITelegramBotClient _botClient = botClient;
    private readonly IServiceProvider _serviceProvider = serviceProvider;
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
        var command = message.Text!.Split(' ')[0].ToLowerInvariant();

        switch (command)
        {
            case "/start":
                await client.SendMessage(
                    chatId: message.Chat.Id,
                    text: "👋 Добро пожаловать в Keep2DFilesBot! Отправьте ссылку, чтобы бот скачал файл и сохранил его.",
                    cancellationToken: ct);
                break;

            case "/help":
                await client.SendMessage(
                    chatId: message.Chat.Id,
                    text: "Отправьте прямую HTTP/HTTPS ссылку на файл. Бот скачает файл и сохранит его с вашими метаданными.",
                    cancellationToken: ct);
                break;

            default:
                await client.SendMessage(
                    chatId: message.Chat.Id,
                    text: "Неизвестная команда. Используйте /help для справки.",
                    cancellationToken: ct);
                break;
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
