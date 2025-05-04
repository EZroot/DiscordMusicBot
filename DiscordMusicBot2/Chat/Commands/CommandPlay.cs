using Discord;
using Discord.WebSocket;
using DiscordMusicBot2.Audio.Interface;
using DiscordMusicBot2.Chat.Commands.Interface;
using DiscordMusicBot2.Services;

namespace DiscordMusicBot2.Chat.Commands
{
    internal class CommandPlay : IBotCommand
    {
        public string CommandName => "play";
        public SlashCommandBuilder Register()
        {
            return new SlashCommandBuilder()
                       .WithName(CommandName)
                       .WithDescription("attempt to play a youtube url")
                       .AddOption("key", ApplicationCommandOptionType.String, "url...", isRequired: true);
        }

        public async Task ExecuteAsync(SocketSlashCommand options)
        {
            var urlOption = options.Data.Options.FirstOrDefault();
            if (urlOption == null || urlOption.Value == null)
            {
                await options.RespondAsync(text: "Invalid input. This shouldn't happen!", ephemeral: true);
                return;
            }

            var videoUrl = urlOption.Value.ToString();
            var user = options.User as IGuildUser;
            var userName = user?.Username;
            var voiceChannel = user?.VoiceChannel;
            if (voiceChannel == null)
            {
                await options.RespondAsync(text: "You need to be in a voice channel to play music!", ephemeral: true);
                return;
            }

            //await options.DeferAsync();
            var audioService = Service.Get<IServiceAudio>();
            await audioService.JoinVoiceChannel(voiceChannel);

            if (videoUrl == null)
            {
                await options.RespondAsync(text: "Invalid URL!", ephemeral: true);
                return;
            }
            Debug.Log(userName + " is playing: " + videoUrl);
            await options.RespondAsync(text: $"Playing {videoUrl} in {voiceChannel.Name}...", ephemeral: true);
            await audioService.Play(videoUrl);
        }
    }
}
