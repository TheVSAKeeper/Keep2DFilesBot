using System.IO;
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
        var downloadService = scope.ServiceProvider.GetRequiredService<DownloadFileService>();

        var progressMessage = await TrySendProgressMessageAsync(client, message, ct);
        TelegramDownloadProgressReporter? progressReporter = null;

        if (progressMessage is not null)
        {
            progressReporter = new TelegramDownloadProgressReporter(
                client,
                message.Chat.Id,
                progressMessage.MessageId,
                _logger);
        }

        var command = new DownloadFileCommand
        {
            Url = message.Text,
            UserId = (UserId)message.From.Id,
            ChatId = message.Chat.Id,
            MessageId = message.MessageId,
            Progress = progressReporter
        };

        var result = await downloadService.DownloadAsync(command, ct);

        if (progressReporter is not null)
        {
            await progressReporter.CompleteAsync();
        }

        if (result.IsSuccess)
        {
            var metadata = result.Value!;
            TelegramUploadProgressReporter? uploadReporter = null;

            if (progressMessage is not null)
            {
                uploadReporter = new TelegramUploadProgressReporter(
                    client,
                    message.Chat.Id,
                    progressMessage.MessageId,
                    _logger);
            }

            var sent = await TrySendDocumentAsync(
                client,
                message,
                progressMessage,
                metadata,
                uploadReporter,
                ct);

            uploadReporter?.Dispose();

            if (!sent)
            {
                await TryEditOrSendFinalAsync(
                    client,
                    message,
                    progressMessage,
                    $"❌ Не удалось отправить файл\nИмя: {metadata.FileName}",
                    ct);
            }
        }
        else
        {
            await TryEditOrSendFinalAsync(
                client,
                message,
                progressMessage,
                $"❌ Ошибка: {result.Error}",
                ct);
        }

        progressReporter?.Dispose();
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

    private async Task<Message?> TrySendProgressMessageAsync(ITelegramBotClient client, Message message, CancellationToken ct)
    {
        try
        {
            return await client.SendMessage(
                chatId: message.Chat.Id,
                text: "⬇️ Скачивание файла\nПодготовка...",
                cancellationToken: ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Не удалось отправить сообщение с прогрессом");
            return null;
        }
    }

    private async Task TryEditOrSendFinalAsync(
        ITelegramBotClient client,
        Message sourceMessage,
        Message? progressMessage,
        string text,
        CancellationToken ct)
    {
        if (progressMessage is not null)
        {
            try
            {
                await client.EditMessageText(
                    chatId: sourceMessage.Chat.Id,
                    messageId: progressMessage.MessageId,
                    text: text,
                    cancellationToken: ct);
                return;
            }
            catch (ApiRequestException ex) when (ex.ErrorCode == 400 && ex.Message.Contains("message is not modified", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Не удалось обновить сообщение с прогрессом");
            }
        }

        await client.SendMessage(
            chatId: sourceMessage.Chat.Id,
            text: text,
            cancellationToken: ct);
    }

    private async Task<bool> TrySendDocumentAsync(
        ITelegramBotClient client,
        Message sourceMessage,
        Message? progressMessage,
        FileMetadata metadata,
        TelegramUploadProgressReporter? progressReporter,
        CancellationToken ct)
    {
        try
        {
            await using var fileStream = File.OpenRead(metadata.FilePath);
            var totalBytes = fileStream.Length;
            Stream stream = fileStream;

            if (progressReporter is not null)
            {
                stream = new ProgressStream(fileStream, totalBytes, progressReporter);
            }

            var caption = $"✅ Файл отправлен\nИмя: {metadata.FileName}\nРазмер: {metadata.FormattedSize}";

            if (progressMessage is not null)
            {
                var document = new InputFileStream(stream, metadata.FileName);
                var media = new InputMediaDocument(document)
                {
                    Caption = caption
                };

                await client.EditMessageMedia(
                    chatId: sourceMessage.Chat.Id,
                    messageId: progressMessage.MessageId,
                    media: media,
                    cancellationToken: ct);
            }
            else
            {
                var document = new InputFileStream(stream, metadata.FileName);

                await client.SendDocument(
                    chatId: sourceMessage.Chat.Id,
                    document: document,
                    caption: caption,
                    cancellationToken: ct);
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Не удалось отправить файл пользователю");
            return false;
        }
        finally
        {
            if (progressReporter is not null)
            {
                await progressReporter.CompleteAsync();
            }
        }
    }

    private sealed class ProgressStream : Stream
    {
        private readonly Stream _inner;
        private readonly long _totalBytes;
        private readonly IProgress<DownloadProgress> _progress;
        private long _reportedBytes;

        public ProgressStream(Stream inner, long totalBytes, IProgress<DownloadProgress> progress)
        {
            _inner = inner;
            _totalBytes = totalBytes;
            _progress = progress;
            _progress.Report(new DownloadProgress(0, totalBytes));
        }

        public override bool CanRead => _inner.CanRead;
        public override bool CanSeek => _inner.CanSeek;
        public override bool CanWrite => _inner.CanWrite;
        public override long Length => _inner.Length;

        public override long Position
        {
            get => _inner.Position;
            set => _inner.Position = value;
        }

        public override void Flush() => _inner.Flush();

        public override int Read(byte[] buffer, int offset, int count)
        {
            var read = _inner.Read(buffer, offset, count);
            Report(read);
            return read;
        }

        public override int Read(Span<byte> buffer)
        {
            var read = _inner.Read(buffer);
            Report(read);
            return read;
        }

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            return ReadInternalAsync(buffer, cancellationToken);
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            return ReadInternalAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();
        }

        private async ValueTask<int> ReadInternalAsync(Memory<byte> buffer, CancellationToken cancellationToken)
        {
            var read = await _inner.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
            Report(read);
            return read;
        }

        public override long Seek(long offset, SeekOrigin origin) => _inner.Seek(offset, origin);

        public override void SetLength(long value) => _inner.SetLength(value);

        public override void Write(byte[] buffer, int offset, int count)
        {
            _inner.Write(buffer, offset, count);
        }

        public override void Write(ReadOnlySpan<byte> buffer)
        {
            _inner.Write(buffer);
        }

        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            return _inner.WriteAsync(buffer, cancellationToken);
        }

        public override Task FlushAsync(CancellationToken cancellationToken) => _inner.FlushAsync(cancellationToken);

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _inner.Dispose();
            }

            base.Dispose(disposing);
        }

        private void Report(int bytesRead)
        {
            if (bytesRead <= 0)
            {
                return;
            }

            _reportedBytes += bytesRead;
            _progress.Report(new DownloadProgress(_reportedBytes, _totalBytes));
        }
    }
}
