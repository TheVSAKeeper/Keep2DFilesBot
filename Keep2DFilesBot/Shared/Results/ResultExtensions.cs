namespace Keep2DFilesBot.Shared.Results;

public static class ResultExtensions
{
    public static Result<T> Ensure<T>(
        this Result<T> result,
        Func<T, bool> predicate,
        string errorMessage)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        ArgumentException.ThrowIfNullOrWhiteSpace(errorMessage);

        if (result.IsFailure)
            return result;

        return predicate(result.Value!)
            ? result
            : Result<T>.Failure(errorMessage);
    }

    public static async Task<Result<T>> EnsureAsync<T>(
        this Result<T> result,
        Func<T, Task<bool>> predicate,
        string errorMessage)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        ArgumentException.ThrowIfNullOrWhiteSpace(errorMessage);

        if (result.IsFailure)
            return result;

        return await predicate(result.Value!).ConfigureAwait(false)
            ? result
            : Result<T>.Failure(errorMessage);
    }

    public static Result<T> Tap<T>(this Result<T> result, Action<T> action)
    {
        ArgumentNullException.ThrowIfNull(action);

        if (result.IsSuccess)
            action(result.Value!);

        return result;
    }

    public static async Task<Result<T>> TapAsync<T>(
        this Result<T> result,
        Func<T, Task> action)
    {
        ArgumentNullException.ThrowIfNull(action);

        if (result.IsSuccess)
            await action(result.Value!).ConfigureAwait(false);

        return result;
    }

    public static Result<T> OnFailure<T>(this Result<T> result, Action<string> action)
    {
        ArgumentNullException.ThrowIfNull(action);

        if (result.IsFailure)
            action(result.Error!);

        return result;
    }
}
