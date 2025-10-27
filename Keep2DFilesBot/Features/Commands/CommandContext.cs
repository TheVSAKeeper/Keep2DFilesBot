using System.Collections.Generic;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace Keep2DFilesBot.Features.Commands;

public sealed record CommandContext(
    ITelegramBotClient Client,
    Message Message,
    string Command,
    IReadOnlyList<string> Arguments);
