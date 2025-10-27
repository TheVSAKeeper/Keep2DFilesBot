using System.Threading;
using System.Threading.Tasks;

namespace Keep2DFilesBot.Features.Commands;

public sealed class StatsCommandHandler : ICommandHandler
{
    public string Command => "/stats";

    public ValueTask<bool> CanHandleAsync(CommandContext context, CancellationToken cancellationToken) => ValueTask.FromResult(true);

    public ValueTask<CommandHandlerResult> HandleAsync(CommandContext context, CancellationToken cancellationToken)
    {
        const string text = "Статистика пока недоступна. Функция в разработке.";
        return ValueTask.FromResult(CommandHandlerResult.Success(text));
    }
}
