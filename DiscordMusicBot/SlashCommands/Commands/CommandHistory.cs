using Discord.WebSocket;
using Discord;
using DiscordMusicBot.SlashCommands.Interfaces;
using DiscordMusicBot.Services.Interfaces;
using DiscordMusicBot.Services;
using System.Text;
using DiscordMusicBot.Commands;
using DiscordMusicBot.Commands.CommandArgs;

namespace DiscordMusicBot.SlashCommands.Commands
{
    internal class CommandHistory : IDiscordCommand
    {
        private string _commandName = "history";
        public string CommandName => _commandName;

        public SlashCommandBuilder Register()
        {
            return new SlashCommandBuilder()
            .WithName(_commandName)
            .WithDescription("Show recent song history");
        }

        public async Task ExecuteAsync(SocketSlashCommand command)
        {
            await CommandHub.ExecuteCommand(new CmdSendHistoryResult(command));
        }

    }
}
