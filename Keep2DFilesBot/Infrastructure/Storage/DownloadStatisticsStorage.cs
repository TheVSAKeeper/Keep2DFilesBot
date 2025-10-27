using System.Text.Json;
using Keep2DFilesBot.Shared.Configuration;
using Keep2DFilesBot.Shared.Models;
using Keep2DFilesBot.Shared.Results;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Keep2DFilesBot.Infrastructure.Storage;

public sealed class DownloadStatisticsStorage
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true
    };

    private readonly string _filePath;
    private readonly SemaphoreSlim _sync = new(1, 1);
    private readonly ILogger<DownloadStatisticsStorage> _logger;

    public DownloadStatisticsStorage(IOptions<StorageConfiguration> options, ILogger<DownloadStatisticsStorage> logger)
    {
        ArgumentNullException.ThrowIfNull(options);
        var basePath = options.Value.BasePath;
        ArgumentException.ThrowIfNullOrWhiteSpace(basePath);

        _filePath = Path.Combine(basePath, "stats.json");
        _logger = logger;
    }

    public async Task<Result<UserDownloadStatistics>> RecordAsync(FileMetadata metadata, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(metadata);

        await _sync.WaitAsync(cancellationToken);

        try
        {
            var statistics = await LoadAsync(cancellationToken);
            var key = metadata.UserId.ToString();

            statistics.TryGetValue(key, out var current);
            var updatedDownloads = current?.Downloads + 1 ?? 1;
            var updatedSize = current?.TotalSize + metadata.Size ?? metadata.Size;
            var updated = new UserDownloadStatistics(updatedDownloads, updatedSize);
            statistics[key] = updated;

            await SaveAsync(statistics, cancellationToken);

            return Result<UserDownloadStatistics>.Success(updated);
        }
        catch (OperationCanceledException)
        {
            return Result<UserDownloadStatistics>.Failure("Операция обновления статистики отменена");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            _logger.LogError(ex, "Ошибка обновления статистики скачиваний");
            return Result<UserDownloadStatistics>.Failure("Ошибка при сохранении статистики скачиваний");
        }
        finally
        {
            _sync.Release();
        }
    }

    public async Task<Result<UserDownloadStatistics>> GetAsync(UserId userId, CancellationToken cancellationToken)
    {
        await _sync.WaitAsync(cancellationToken);

        try
        {
            var statistics = await LoadAsync(cancellationToken);
            var key = userId.ToString();

            if (!statistics.TryGetValue(key, out var value))
            {
                return Result<UserDownloadStatistics>.Success(new UserDownloadStatistics(0, 0));
            }

            return Result<UserDownloadStatistics>.Success(value);
        }
        catch (OperationCanceledException)
        {
            return Result<UserDownloadStatistics>.Failure("Операция чтения статистики отменена");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            _logger.LogError(ex, "Ошибка чтения статистики скачиваний");
            return Result<UserDownloadStatistics>.Failure("Ошибка при чтении статистики скачиваний");
        }
        finally
        {
            _sync.Release();
        }
    }

    private async Task<Dictionary<string, UserDownloadStatistics>> LoadAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(_filePath))
        {
            return new Dictionary<string, UserDownloadStatistics>(StringComparer.OrdinalIgnoreCase);
        }

        await using var stream = File.OpenRead(_filePath);
        var statistics = await JsonSerializer.DeserializeAsync<Dictionary<string, UserDownloadStatistics>>(stream, SerializerOptions, cancellationToken);
        return statistics ?? new Dictionary<string, UserDownloadStatistics>(StringComparer.OrdinalIgnoreCase);
    }

    private async Task SaveAsync(Dictionary<string, UserDownloadStatistics> statistics, CancellationToken cancellationToken)
    {
        var directory = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await using var stream = File.Create(_filePath);
        await JsonSerializer.SerializeAsync(stream, statistics, SerializerOptions, cancellationToken);
        await stream.FlushAsync(cancellationToken);
    }
}
