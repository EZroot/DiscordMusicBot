using Discord;
using Discord.WebSocket;
using DiscordMusicBot2.Audio.Interface;
using DiscordMusicBot2.Chat.Commands.Interface;
using DiscordMusicBot2.Helpers;
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
            await options.DeferAsync(ephemeral:true);

            var songDetails = await YoutubeHelper.GetVideoDetails(videoUrl);
            var title = songDetails[0];
            var length = songDetails[1];

            //if (string.IsNullOrEmpty(length))
            //{
            //    Debug.Log("<color=red>WARNING:</color>Failed to get song length - probably a live stream!");
            //    await options.FollowupAsync(text: $"Invalid Url or is a Live Stream (Unsupported atm)", ephemeral: true);
            //    return;
            //}

            await options.FollowupAsync(text: $"Adding {title} to Queue", ephemeral: true);
            await audioService.Play(videoUrl);
        }
    }
}
