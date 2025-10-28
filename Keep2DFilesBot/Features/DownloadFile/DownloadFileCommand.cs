using System;
using Keep2DFilesBot.Shared.Models;

namespace Keep2DFilesBot.Features.DownloadFile;

public sealed record DownloadFileCommand
{
    public required string Url { get; init; }

    public required UserId UserId { get; init; }

    public required long ChatId { get; init; }

    public required int MessageId { get; init; }

    public IProgress<DownloadProgress>? Progress { get; init; }
}
