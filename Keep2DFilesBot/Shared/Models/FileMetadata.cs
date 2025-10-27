using System;

namespace Keep2DFilesBot.Shared.Models;

public sealed record FileMetadata
{
    public required string FilePath { get; init; }
    public required string FileName { get; init; }
    public required long Size { get; init; }
    public required string ContentType { get; init; }
    public required DateTime DownloadedAt { get; init; }
    public required UserId UserId { get; init; }

    public string FormattedSize => Size switch
    {
        < 1024 => $"{Size} байт",
        < 1024 * 1024 => $"{Size / 1024.0:F2} КБ",
        < 1024 * 1024 * 1024 => $"{Size / (1024.0 * 1024.0):F2} МБ",
        _ => $"{Size / (1024.0 * 1024.0 * 1024.0):F2} ГБ"
    };
}
