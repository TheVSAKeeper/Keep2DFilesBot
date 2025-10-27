using System.Globalization;
using Keep2DFilesBot.Shared.Configuration;
using Keep2DFilesBot.Shared.Models;
using Keep2DFilesBot.Shared.Results;
using Microsoft.Extensions.Options;

namespace Keep2DFilesBot.Infrastructure.Storage;

public sealed class FileStorage(IOptions<StorageConfiguration> options, ILogger<FileStorage> logger)
{
    private readonly StorageConfiguration _config = options.Value;
    private readonly ILogger<FileStorage> _logger = logger;

    public async Task<Result<FileMetadata>> SaveAsync(
        Stream stream,
        UserId userId,
        string? fileName,
        string contentType,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentException.ThrowIfNullOrWhiteSpace(contentType);

        try
        {
            var name = NormalizeFileName(fileName, contentType);
            var basePath = _config.BasePath;
            ArgumentException.ThrowIfNullOrWhiteSpace(basePath);

            var userDirectory = Path.Combine(basePath, userId.ToString());
            var dateDirectory = Path.Combine(
                userDirectory,
                DateTime.UtcNow.ToString(_config.DateFormat, CultureInfo.InvariantCulture));

            Directory.CreateDirectory(dateDirectory);

            var filePath = Path.Combine(dateDirectory, name);

            await using var destination = File.Create(filePath);
            await stream.CopyToAsync(destination, ct);
            await destination.FlushAsync(ct);

            var metadata = new FileMetadata
            {
                FilePath = filePath,
                FileName = name,
                Size = destination.Length,
                ContentType = contentType,
                DownloadedAt = DateTime.UtcNow,
                UserId = userId
            };

            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation(
                    "Файл сохранён: {FilePath}, пользователь: {UserId}, размер: {Size}",
                    filePath,
                    userId,
                    metadata.Size);
            }

            return Result<FileMetadata>.Success(metadata);
        }
        catch (OperationCanceledException)
        {
            return Result<FileMetadata>.Failure("Операция сохранения отменена");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _logger.LogError(ex, "Ошибка сохранения файла для пользователя {UserId}", userId);
            return Result<FileMetadata>.Failure("Ошибка при сохранении файла");
        }
    }

    private static string NormalizeFileName(string? fileName, string contentType)
    {
        var candidate = string.IsNullOrWhiteSpace(fileName)
            ? CreateFallbackName(contentType)
            : fileName.Trim();

        var invalidCharacters = Path.GetInvalidFileNameChars();
        if (candidate.IndexOfAny(invalidCharacters) >= 0)
        {
            candidate = new string(candidate.Select(ch => invalidCharacters.Contains(ch) ? '_' : ch).ToArray());
        }

        return candidate.Length == 0 ? CreateFallbackName(contentType) : candidate;
    }

    private static string CreateFallbackName(string contentType)
    {
        var extension = contentType.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) switch
        {
            [_, var type] when !string.IsNullOrWhiteSpace(type) => $".{type}",
            _ => ".bin"
        };

        return $"{Guid.NewGuid()}{extension}";
    }
}
