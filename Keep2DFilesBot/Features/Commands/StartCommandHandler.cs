using System.Threading;
using System.Threading.Tasks;

namespace Keep2DFilesBot.Features.Commands;

public sealed class StartCommandHandler : ICommandHandler
{
    public string Command => "/start";

    public ValueTask<bool> CanHandleAsync(CommandContext context, CancellationToken cancellationToken)
    {
        return ValueTask.FromResult(true);
    }

    public ValueTask<CommandHandlerResult> HandleAsync(CommandContext context, CancellationToken cancellationToken)
    {
        const string text = "👋 Добро пожаловать в Keep2DFilesBot! Отправьте ссылку, чтобы бот скачал файл и сохранил его.";
        return ValueTask.FromResult(CommandHandlerResult.Success(text));
    }
}
