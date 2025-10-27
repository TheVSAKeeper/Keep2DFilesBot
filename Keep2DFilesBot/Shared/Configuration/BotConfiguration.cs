namespace Keep2DFilesBot.Shared.Configuration;

public sealed class BotConfiguration
{
    public const string SectionName = "BotConfiguration";

    public required string Token { get; init; }

    public required long[] AllowedUsers { get; init; }

    public required string WorkingDirectory { get; init; }

    public bool IsPublic { get; init; }
}
