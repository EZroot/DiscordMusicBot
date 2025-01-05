using DiscordMusicBot.InternalCommands.Interfaces;
using DiscordMusicBot.Utils;

namespace DiscordMusicBot.InternalCommands
{
    internal static class CommandHub
    {
        public static async Task ExecuteCommand(ICommand command)
        {
            await command.ExecuteAsync();
        }
    }
}