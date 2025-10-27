using System.Threading;
using System.Threading.Tasks;

namespace Keep2DFilesBot.Features.Commands;

public sealed class HelpCommandHandler : ICommandHandler
{
    public string Command => "/help";

    public ValueTask<bool> CanHandleAsync(CommandContext context, CancellationToken cancellationToken)
    {
        return ValueTask.FromResult(true);
    }

    public ValueTask<CommandHandlerResult> HandleAsync(CommandContext context, CancellationToken cancellationToken)
    {
        const string text = "Отправьте прямую HTTP/HTTPS ссылку на файл. Бот скачает файл и сохранит его.";
        return ValueTask.FromResult(CommandHandlerResult.Success(text));
    }
}
