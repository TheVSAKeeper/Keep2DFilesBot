using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Keep2DFilesBot.Features.Commands;

public sealed class CommandRouter
{
    private readonly IReadOnlyDictionary<string, ICommandHandler> _handlers;

    public CommandRouter(IEnumerable<ICommandHandler> handlers)
    {
        var handlerList = handlers?.ToArray() ?? [];
        _handlers = handlerList
            .GroupBy(x => x.Command)
            .ToDictionary(
                group => group.Key,
                group => group.First(),
                StringComparer.OrdinalIgnoreCase);
    }

    public async ValueTask<CommandHandlerResult> RouteAsync(CommandContext context, CancellationToken cancellationToken)
    {
        if (_handlers.TryGetValue(context.Command, out var handler))
        {
            if (!await handler.CanHandleAsync(context, cancellationToken).ConfigureAwait(false))
            {
                return CommandHandlerResult.Failure("Команда недоступна");
            }

            return await handler.HandleAsync(context, cancellationToken).ConfigureAwait(false);
        }

        return CommandHandlerResult.Failure("Неизвестная команда. Используйте /help для справки.");
    }
}
