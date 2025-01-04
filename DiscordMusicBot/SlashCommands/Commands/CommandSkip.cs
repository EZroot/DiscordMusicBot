using Discord.WebSocket;
using DiscordMusicBot.SlashCommands.Interfaces;
using Discord;
using DiscordMusicBot.Services;
using DiscordMusicBot.Services.Interfaces;

namespace DiscordMusicBot.SlashCommands.Commands
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
