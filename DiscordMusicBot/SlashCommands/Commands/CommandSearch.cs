using Discord.WebSocket;
using Discord;
using DiscordMusicBot.SlashCommands.Interfaces;
using DiscordMusicBot.Commands;
using DiscordMusicBot.Commands.CommandArgs;

namespace DiscordMusicBot.SlashCommands.Commands
{
    internal class CommandSearch : IDiscordCommand
    {
        private string _commandName = "search";
        public string CommandName => _commandName;

        public SlashCommandBuilder Register()
        {
            return new SlashCommandBuilder()
            .WithName(_commandName)
            .WithDescription("Searches youtube based on the keyword")
            .AddOption("key", ApplicationCommandOptionType.String, "Search...", isRequired: true);
        }

        public async Task ExecuteAsync(SocketSlashCommand command)
        {
            await CommandHub.ExecuteCommand(new CmdSendSearchResult(command));
        }
    }
}
