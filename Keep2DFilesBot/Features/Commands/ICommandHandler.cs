using System.Threading;
using System.Threading.Tasks;

namespace Keep2DFilesBot.Features.Commands;

public interface ICommandHandler
{
    string Command { get; }

    ValueTask<bool> CanHandleAsync(CommandContext context, CancellationToken cancellationToken);

    ValueTask<CommandHandlerResult> HandleAsync(CommandContext context, CancellationToken cancellationToken);
}
