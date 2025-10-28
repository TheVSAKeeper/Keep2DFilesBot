using System;

namespace Keep2DFilesBot.Shared.Models;

public readonly record struct DownloadProgress(long BytesReceived, long? TotalBytes);
