using Discord.WebSocket;
using DiscordMusicBot.SlashCommands.Interfaces;
using Discord;
using DiscordMusicBot.Services;
using DiscordMusicBot.Services.Interfaces;

namespace DiscordMusicBot.SlashCommands.Commands
{
    internal class CommandShuffle : IDiscordCommand
    {
        private string _commandName = "shuffle";
        public string CommandName => _commandName;

        public SlashCommandBuilder Register()
        {
            return new SlashCommandBuilder()
            .WithName(_commandName)
            .WithDescription("Shuffle's the queued songs.");
        }

        public async Task ExecuteAsync(SocketSlashCommand command)
        {
            await Service.Get<IServiceAudioManager>().ShuffleQueue(command);
        }

    }
}
