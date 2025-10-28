using System.Linq;
using FluentValidation;
using Keep2DFilesBot.Shared.Models;
using Keep2DFilesBot.Shared.Results;

namespace Keep2DFilesBot.Features.DownloadFile;

public sealed class DownloadFileService(
    DownloadFileHandler handler,
    IValidator<DownloadFileCommand> validator)
{
    private readonly DownloadFileHandler _handler = handler;
    private readonly IValidator<DownloadFileCommand> _validator = validator;

    public async Task<Result<FileMetadata>> DownloadAsync(DownloadFileCommand command, CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(command, cancellationToken);

        if (!validationResult.IsValid)
        {
            var error = validationResult.Errors.FirstOrDefault()?.ErrorMessage ?? "Данные команды некорректны";
            return Result<FileMetadata>.Failure(error);
        }

        return await _handler.HandleAsync(command, cancellationToken);
    }
}
