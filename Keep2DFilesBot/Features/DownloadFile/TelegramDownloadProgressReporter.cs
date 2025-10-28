using System.Globalization;
using Keep2DFilesBot.Shared.Models;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Exceptions;

namespace Keep2DFilesBot.Features.DownloadFile;

public sealed class TelegramDownloadProgressReporter : IProgress<DownloadProgress>, IDisposable
{
    private readonly ITelegramBotClient _botClient;
    private readonly long _chatId;
    private readonly int _messageId;
    private readonly ILogger _logger;
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private int? _lastPercent;
    private long _lastBytes;
    private DateTime _lastUpdate;
    private bool _isCompleted;

    public TelegramDownloadProgressReporter(
        ITelegramBotClient botClient,
        long chatId,
        int messageId,
        ILogger logger)
    {
        _botClient = botClient;
        _chatId = chatId;
        _messageId = messageId;
        _logger = logger;
    }

    public void Report(DownloadProgress value)
    {
        _ = ProcessReportAsync(value);
    }

    public async Task CompleteAsync()
    {
        await _semaphore.WaitAsync().ConfigureAwait(false);
        _isCompleted = true;
        _semaphore.Release();
    }

    private async Task ProcessReportAsync(DownloadProgress value)
    {
        try
        {
            await _semaphore.WaitAsync().ConfigureAwait(false);

            if (_isCompleted)
            {
                return;
            }

            if (!ShouldUpdate(value))
            {
                return;
            }

            var text = BuildText(value);

            try
            {
                await _botClient.EditMessageText(
                    chatId: _chatId,
                    messageId: _messageId,
                    text: text).ConfigureAwait(false);

                UpdateState(value);
            }
            catch (ApiRequestException ex) when (IsNotModifiedError(ex))
            {
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Не удалось обновить сообщение с прогрессом");
            }
        }
        finally
        {
            if (_semaphore.CurrentCount == 0)
            {
                _semaphore.Release();
            }
        }
    }

    private bool ShouldUpdate(DownloadProgress value)
    {
        var now = DateTime.UtcNow;

        if (value.TotalBytes is long total && total > 0)
        {
            var percent = (int)Math.Clamp((double)value.BytesReceived * 100 / total, 0, 100);

            if (_lastPercent is { } lastPercent && percent < 100)
            {
                if (percent < lastPercent + 5)
                {
                    return false;
                }
            }

            if (_lastPercent is { } existing && percent == existing && value.BytesReceived != total)
            {
                return false;
            }

            return true;
        }

        if (_lastUpdate != DateTime.MinValue)
        {
            var bytesDelta = value.BytesReceived - _lastBytes;

            if (bytesDelta < 512 * 1024 && (now - _lastUpdate) < TimeSpan.FromSeconds(2))
            {
                return false;
            }
        }

        return true;
    }

    private void UpdateState(DownloadProgress value)
    {
        _lastBytes = value.BytesReceived;
        _lastUpdate = DateTime.UtcNow;

        if (value.TotalBytes is long total && total > 0)
        {
            _lastPercent = (int)Math.Clamp((double)value.BytesReceived * 100 / total, 0, 100);
        }
    }

    private static bool IsNotModifiedError(ApiRequestException exception)
    {
        return exception.ErrorCode == 400 && exception.Message.Contains("message is not modified", StringComparison.OrdinalIgnoreCase);
    }

    private static string BuildText(DownloadProgress value)
    {
        if (value.TotalBytes is long total && total > 0)
        {
            var percent = (int)Math.Clamp((double)value.BytesReceived * 100 / total, 0, 100);
            var downloaded = FormatSize(value.BytesReceived);
            var totalText = FormatSize(total);
            return $"⬇️ Скачивание файла\nПрогресс: {percent}% ({downloaded} из {totalText})";
        }

        var received = FormatSize(value.BytesReceived);
        return $"⬇️ Скачивание файла\nПолучено: {received}";
    }

    private static string FormatSize(long bytes)
    {
        if (bytes < 1024)
        {
            return $"{bytes} байт";
        }

        var kilobytes = bytes / 1024.0;
        if (kilobytes < 1024)
        {
            return $"{kilobytes:F2} КБ";
        }

        var megabytes = kilobytes / 1024.0;
        if (megabytes < 1024)
        {
            return $"{megabytes:F2} МБ";
        }

        var gigabytes = megabytes / 1024.0;
        return $"{gigabytes:F2} ГБ";
    }

    public void Dispose()
    {
        _semaphore.Dispose();
    }
}
