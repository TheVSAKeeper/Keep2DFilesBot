using System;

namespace Keep2DFilesBot.Features.Commands;

public sealed class CommandHandlerResult
{
    private CommandHandlerResult(bool isSuccess, string? error, CommandResultPayload? payload)
    {
        IsSuccess = isSuccess;
        Error = error;
        Payload = payload;
    }

    public bool IsSuccess { get; }

    public bool IsFailure => !IsSuccess;

    public string? Error { get; }

    public CommandResultPayload? Payload { get; }

    public static CommandHandlerResult Success(string text)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);
        return Success(new CommandResultPayload(text));
    }

    public static CommandHandlerResult Success(CommandResultPayload payload) => new(true, null, payload);

    public static CommandHandlerResult Failure(string message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        return new(false, message, null);
    }
}
