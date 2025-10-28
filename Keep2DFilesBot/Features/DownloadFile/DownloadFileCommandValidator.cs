using FluentValidation;
using Keep2DFilesBot.Shared.Models;

namespace Keep2DFilesBot.Features.DownloadFile;

public sealed class DownloadFileCommandValidator : AbstractValidator<DownloadFileCommand>
{
    public DownloadFileCommandValidator()
    {
        RuleFor(x => x.Url)
            .NotEmpty().WithMessage("Не указана ссылка на файл")
            .Must(url => Url.Create(url).IsSuccess)
            .WithMessage("Указана некорректная ссылка на файл");

        RuleFor(x => x.UserId)
            .Must(id => id != default)
            .WithMessage("Не задан идентификатор пользователя");

        RuleFor(x => x.ChatId)
            .NotEqual(0)
            .WithMessage("Не задан идентификатор чата");

        RuleFor(x => x.MessageId)
            .GreaterThan(0)
            .WithMessage("Не задан идентификатор сообщения");
    }
}
