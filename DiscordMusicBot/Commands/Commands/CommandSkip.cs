using Discord.WebSocket;
using DiscordMusicBot.Commands.Interfaces;
using Discord;
using DiscordMusicBot.Services;
using DiscordMusicBot.Services.Interfaces;

namespace DiscordMusicBot.Commands.Commands
{
    internal class CommandSkip : IDiscordCommand
    {
        private string _commandName = "skip";
        public string CommandName => _commandName;

        public SlashCommandBuilder Register()
        {
            return new SlashCommandBuilder()
            .WithName(_commandName)
            .WithDescription("Skips the current song");
        }

        public async Task ExecuteAsync(SocketSlashCommand command)
        {
            await Service.Get<IServiceAudioManager>().SkipSong(command);
        }

    }
}
