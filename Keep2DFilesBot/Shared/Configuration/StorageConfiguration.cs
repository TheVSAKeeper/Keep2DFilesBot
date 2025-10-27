namespace Keep2DFilesBot.Shared.Configuration;

public sealed class StorageConfiguration
{
    public const string SectionName = "StorageConfiguration";

    public required string BasePath { get; init; }

    public string DateFormat { get; init; } = "yyyy-MM-dd";

    public bool SaveMetadata { get; init; } = true;
}
