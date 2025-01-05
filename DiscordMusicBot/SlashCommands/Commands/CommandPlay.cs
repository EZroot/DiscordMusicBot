using Discord.WebSocket;
using Discord;
using DiscordMusicBot.SlashCommands.Interfaces;
using DiscordMusicBot.Services;
using DiscordMusicBot.Services.Interfaces;

namespace DiscordMusicBot.SlashCommands.Commands
{
    internal class CommandPlay : IDiscordCommand
    {
        private string _commandName = "play";
        public string CommandName => _commandName;

        public SlashCommandBuilder Register()
        {
            return new SlashCommandBuilder()
            .WithName(_commandName)
            .WithDescription("Joins a voice channel and plays audio")
            .AddOption("url", ApplicationCommandOptionType.String, "Youtube url", isRequired: true);
        }

        public async Task ExecuteAsync(SocketSlashCommand command)
        {
            await Service.Get<IServiceAudioManager>().PlaySong(command);
        }

    }
}
