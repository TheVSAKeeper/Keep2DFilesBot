using System.Text.Json;
using Keep2DFilesBot.Shared.Models;
using Keep2DFilesBot.Shared.Results;
using Microsoft.Extensions.Logging;

namespace Keep2DFilesBot.Infrastructure.Storage;

public sealed class MetadataStorage
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true
    };

    private readonly ILogger<MetadataStorage> _logger;

    public MetadataStorage(ILogger<MetadataStorage> logger)
    {
        _logger = logger;
    }

    public async Task<Result<FileMetadata>> SaveAsync(FileMetadata metadata, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(metadata);

        var metadataPath = $"{metadata.FilePath}.json";

        try
        {
            await using var stream = File.Create(metadataPath);
            await JsonSerializer.SerializeAsync(stream, metadata, SerializerOptions, cancellationToken);
            await stream.FlushAsync(cancellationToken);

            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug("Метаданные сохранены: {Path}", metadataPath);
            }

            return Result<FileMetadata>.Success(metadata);
        }
        catch (OperationCanceledException)
        {
            return Result<FileMetadata>.Failure("Операция сохранения метаданных отменена");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _logger.LogError(ex, "Ошибка сохранения метаданных: {Path}", metadataPath);
            return Result<FileMetadata>.Failure("Ошибка при сохранении метаданных файла");
        }
    }
}
