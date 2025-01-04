using Discord.WebSocket;
using DiscordMusicBot.Services.Interfaces;
using DiscordMusicBot.Services;
using Discord;
using DiscordMusicBot.SlashCommands.Interfaces;

namespace DiscordMusicBot.SlashCommands.Commands
{
    internal class CommandLeave : IDiscordCommand
    {
        private string _commandName = "leave";
        public string CommandName => _commandName;

        public SlashCommandBuilder Register()
        {
            return new SlashCommandBuilder()
            .WithName(_commandName)
            .WithDescription("Show recent song history");
        }

        public async Task ExecuteAsync(SocketSlashCommand command)
        {
            await Service.Get<IServiceAudioManager>().LeaveVoice(command);
        }

    }
}
