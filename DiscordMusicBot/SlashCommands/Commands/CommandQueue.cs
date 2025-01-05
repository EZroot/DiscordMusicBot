using Discord.WebSocket;
using DiscordMusicBot.SlashCommands.Interfaces;
using Discord;
using DiscordMusicBot.Services.Interfaces;
using DiscordMusicBot.Services;

namespace DiscordMusicBot.SlashCommands.Commands
{
    internal class CommandQueue : IDiscordCommand
    {
        private string _commandName = "queue";
        public string CommandName => _commandName;

        public SlashCommandBuilder Register()
        {
            return new SlashCommandBuilder()
            .WithName(_commandName)
            .WithDescription("Displays current queue");
        }

        public async Task ExecuteAsync(SocketSlashCommand command)
        {
            await Service.Get<IServiceAudioPlaybackService>().GetCurrentSongQueue(command);
        }

    }
}
