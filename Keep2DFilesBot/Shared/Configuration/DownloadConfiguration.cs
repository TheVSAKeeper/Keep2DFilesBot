namespace Keep2DFilesBot.Shared.Configuration;

public sealed class DownloadConfiguration
{
    public const string SectionName = "DownloadConfiguration";

    public long MaxFileSize { get; init; } = 100 * 1024 * 1024;

    public int TimeoutSeconds { get; init; } = 300;

    public int RetryCount { get; init; } = 3;

    public int RetryDelaySeconds { get; init; } = 2;
}
