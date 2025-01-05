using DiscordMusicBot.Commands.Interfaces;
using DiscordMusicBot.Utils;

namespace DiscordMusicBot.Commands
{
    internal static class CommandHub
    {
        public static async Task ExecuteCommand(ICommand command)
        {
            await command.ExecuteAsync();
        }
    }
}