using Discord.WebSocket;
using Discord;
using DiscordMusicBot.SlashCommands.Interfaces;
using DiscordMusicBot.Services;
using DiscordMusicBot.Services.Interfaces;
using DiscordMusicBot.Utils;

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
            var urlOption = command.Data.Options.First();
            string videoUrl = urlOption?.Value?.ToString();
            var user = command.User.Username;
            
            await Service.Get<IServiceAudioPlaybackService>().JoinVoice(command);

            if (Service.Get<IServiceYtdlp>().IsYouTubeUrl(videoUrl))
            {
                await command.RespondAsync(text: $"Searching: `{videoUrl}`", ephemeral: true);
                var song = await Service.Get<IServiceAudioPlaybackService>().PlaySong(user, videoUrl);
                await command.ModifyOriginalResponseAsync((m) => m.Content = $"Added **{song.Value.Title}** to Queue!");
                return;
            }
            else
            {
                Debug.Log($"<color=magenta>{user}:></color> <color=red>'{videoUrl}' Invalid url.</color>");
                await command.RespondAsync(text: $"`{videoUrl}` is not a valid youtube url.", ephemeral: true);
            }
        }

    }
}
