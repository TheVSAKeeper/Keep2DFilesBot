using System.Net;
using System.Net.Http;
using Keep2DFilesBot.Infrastructure.Storage;
using Keep2DFilesBot.Shared.Configuration;
using Keep2DFilesBot.Shared.Models;
using Keep2DFilesBot.Shared.Results;
using Microsoft.Extensions.Options;

namespace Keep2DFilesBot.Features.DownloadFile;

public sealed class DownloadFileHandler(
    IHttpClientFactory httpClientFactory,
    FileStorage fileStorage,
    ILogger<DownloadFileHandler> logger,
    IOptions<DownloadConfiguration> options)
{
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
    private readonly FileStorage _fileStorage = fileStorage;
    private readonly ILogger<DownloadFileHandler> _logger = logger;
    private readonly DownloadConfiguration _config = options.Value;

    public async Task<Result<FileMetadata>> HandleAsync(DownloadFileCommand command, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(command);

        return await Url.Create(command.Url)
            .ThenAsync(url => DownloadFileAsync(url, ct))
            .ThenAsync(response => EnsureFileSizeAsync(response, ct))
            .ThenAsync(response => SaveFileAsync(response, command.UserId, ct));
    }

    private async Task<Result<HttpResponseMessage>> DownloadFileAsync(Url url, CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("Начато скачивание {Url}", url);

            var httpClient = _httpClientFactory.CreateClient("DownloadClient");
            var response = await httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("HTTP ошибка {StatusCode} при скачивании {Url}", response.StatusCode, url);
                response.Dispose();
                return Result<HttpResponseMessage>.Failure(CreateHttpError(response.StatusCode, response.ReasonPhrase));
            }

            return Result<HttpResponseMessage>.Success(response);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Сетевая ошибка при скачивании {Url}", url);
            return Result<HttpResponseMessage>.Failure("Ошибка сети при скачивании файла");
        }
        catch (TaskCanceledException)
        {
            _logger.LogWarning("Таймаут при скачивании {Url}", url);
            return Result<HttpResponseMessage>.Failure("Превышено время ожидания при скачивании файла");
        }
    }

    private async Task<Result<HttpResponseMessage>> EnsureFileSizeAsync(HttpResponseMessage response, CancellationToken ct)
    {
        if (response.Content.Headers.ContentLength is long length)
        {
            if (length > _config.MaxFileSize)
            {
                response.Dispose();
                _logger.LogWarning("Файл слишком большой: {Size} байт (максимум {Max})", length, _config.MaxFileSize);
                return Result<HttpResponseMessage>.Failure("Размер файла превышает допустимое значение");
            }
        }
        else if (_config.MaxFileSize > 0)
        {
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            linkedCts.CancelAfter(TimeSpan.FromSeconds(Math.Max(1, _config.TimeoutSeconds)));
            try
            {
                await response.Content.LoadIntoBufferAsync(_config.MaxFileSize + 1);
            }
            catch (HttpRequestException ex)
            {
                response.Dispose();
                _logger.LogError(ex, "Ошибка проверки размера файла");
                return Result<HttpResponseMessage>.Failure("Не удалось проверить размер файла");
            }
            catch (OperationCanceledException)
            {
                response.Dispose();
                _logger.LogWarning("Превышено время проверки размера файла");
                return Result<HttpResponseMessage>.Failure("Не удалось проверить размер файла");
            }

            if (response.Content.Headers.ContentLength is long bufferedLength && bufferedLength > _config.MaxFileSize)
            {
                response.Dispose();
                _logger.LogWarning("Файл слишком большой после буферизации: {Size} байт", bufferedLength);
                return Result<HttpResponseMessage>.Failure("Размер файла превышает допустимое значение");
            }
        }

        return Result<HttpResponseMessage>.Success(response);
    }

    private async Task<Result<FileMetadata>> SaveFileAsync(HttpResponseMessage response, UserId userId, CancellationToken ct)
    {
        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        var fileName = ResolveFileName(response);
        var contentType = response.Content.Headers.ContentType?.MediaType ?? "application/octet-stream";

        var result = await _fileStorage.SaveAsync(stream, userId, fileName, contentType, ct);
        response.Dispose();
        return result;
    }

    private static string CreateHttpError(HttpStatusCode statusCode, string? reason)
    {
        var description = reason ?? statusCode.ToString();
        return $"HTTP {(int)statusCode}: {description}";
    }

    private static string ResolveFileName(HttpResponseMessage response)
    {
        if (response.Content.Headers.ContentDisposition?.FileNameStar is { } extendedName)
            return extendedName.Trim('"');

        if (response.Content.Headers.ContentDisposition?.FileName is { } fileName)
            return fileName.Trim('"');

        if (response.RequestMessage?.RequestUri is { } uri)
        {
            var lastSegment = uri.Segments.LastOrDefault();
            if (!string.IsNullOrWhiteSpace(lastSegment) && lastSegment != "/")
                return Uri.UnescapeDataString(lastSegment);
        }

        return string.Empty;
    }
}
