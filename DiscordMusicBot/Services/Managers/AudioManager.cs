using Discord;
using Discord.Audio;
using Discord.WebSocket;
using DiscordMusicBot.Events.Events;
using DiscordMusicBot.Events;
using DiscordMusicBot.Models;
using DiscordMusicBot.Services.Interfaces;
using DiscordMusicBot.Utils;
using System.Text;

namespace DiscordMusicBot.Services.Managers
{
    internal class AudioManager : IServiceAudioManager
    {
        private const int QUEUE_DISPLAY_LIMIT = 10;
        private IAudioClient? _audioClient;
        private Queue<SongData> _songDataQueue = new Queue<SongData>();
        private SongData _currentPlayingSong;

        public int SongCount => _songDataQueue.Count;

        public async Task PlaySong(SocketSlashCommand command)
        {
            await CheckAndJoinVoice(command);

            var urlOption = command.Data.Options.First();
            string videoUrl = urlOption?.Value?.ToString();
            var user = command.User.Username;
            if (Service.Get<IServiceYtdlp>().IsYouTubeUrl(videoUrl))
            {
                await command.RespondAsync(text: $"Searching: `{videoUrl}`", ephemeral: true);
                string title = await Service.Get<IServiceYtdlp>().GetSongTitle(videoUrl);
                await Service.Get<IServiceAnalytics>().AddSongAnalytics(user, new SongData { Title = title, Url = videoUrl });
                await command.ModifyOriginalResponseAsync((m) => m.Content = $"Added **{title}** to Queue!");
                await PlaySong(title, videoUrl);
                return;
            }
            else
            {
                Debug.Log($"'{videoUrl}' Invalid url.");
                await command.RespondAsync(text: $"`{videoUrl}` is not a valid youtube url.", ephemeral: true);
            }
        }

        public async Task PlaySong(string title, string url)
        {
            _songDataQueue.Enqueue(new SongData { Title = title, Url = url });
            //await PlayNextSong(_audioClient);
            if (_songDataQueue.Count == 1 && !Service.Get<IServiceFFmpeg>().IsSongPlaying)
            {
                _currentPlayingSong = _songDataQueue.Dequeue();
            }
            Debug.Log($"Added song: <color=cyan>{title}</color> <color=magenta>{url}</color> to queue.");
            await Service.Get<IServiceYtdlp>().StreamToDiscord(_audioClient, url);
        }

        public async Task PlayNextSong(IAudioClient client)
        {
            if (_songDataQueue.Count == 0) { return; }
            _currentPlayingSong = _songDataQueue.Dequeue();
            EventHub.Raise(new EvOnPlayNextSong() { Title = _currentPlayingSong.Title, Url = _currentPlayingSong.Url });
            await Service.Get<IServiceYtdlp>().StreamToDiscord(_audioClient, _currentPlayingSong.Url);
        }

        public async Task SongQueue(SocketSlashCommand command)
        {
            if (_songDataQueue == null || _songDataQueue.Count == 0)
            {
                if (_currentPlayingSong.Title != "" && Service.Get<IServiceFFmpeg>().IsSongPlaying)
                {
                    await command.RespondAsync(text: $"Currently playing: {_currentPlayingSong.Title}", ephemeral: true);
                    return;
                }
                await command.RespondAsync(text: $"There are no songs in queue.", ephemeral: true);
                return;
            }

            var songArr = _songDataQueue.ToArray();
            if (_currentPlayingSong.Title != "" && Service.Get<IServiceFFmpeg>().IsSongPlaying)
            {
                songArr = new SongData[_songDataQueue.Count + 1];
                songArr[0] = _currentPlayingSong;
                _songDataQueue.ToArray().CopyTo(songArr, 1);
            }
            var result = new StringBuilder();
            result.AppendLine("* -- --  -- -- Queued Songs -- --  -- -- *");
            for (var i = 0; i < songArr.Length; i++)
            {
                result.Append(i == 0 ? "Currently playing " : $"{i + 1}# ");
                if (i == 0) result.AppendLine();
                result.AppendLine(i == 0 ? $"- : {songArr[i].Title}\n" : songArr[i].Title);
                if (i > QUEUE_DISPLAY_LIMIT) { result.AppendLine("------------------------ More Hidden ------------------------"); break; } 
            }
            await command.RespondAsync(text: $"{result}", ephemeral: true);
        }

        public async Task SkipSong(SocketSlashCommand command)
        {
            if (Service.Get<IServiceFFmpeg>().ForceClose())
            {
                await command.RespondAsync(text: "Skipped song.", ephemeral: true);
                return;
            }
            await command.RespondAsync(text: "Failed to skip song. Current song is null.", ephemeral: true);
        }

        public async Task ChangeVolume(SocketSlashCommand command)
        {
            var option = command.Data.Options.First();
            var volume = (float)((double)(option?.Value));
            if (volume >= 0 && volume <= 100)
            {
                await command.RespondAsync(text: $"Volume set: {volume.ToString("0")}/100", ephemeral: true);
                volume = volume / 100f;
                await Service.Get<IServiceFFmpeg>().SetVolume(volume);
                return;
            }
            await command.RespondAsync(text: $"'{volume}' is invalid. Volume must be 0-100", ephemeral: true);
        }

        public async Task CheckAndJoinVoice(SocketSlashCommand command)
        {
            var user = command.User as IGuildUser;
            var voiceChannel = user?.VoiceChannel;

            if (voiceChannel == null)
            {
                Debug.Log("Bot failed to join voice");
                await command.RespondAsync("You need to join a voice channel first.", ephemeral: true);
                return;
            }

            if (_audioClient == null)
            {
                try
                {
                    Debug.Log("Joined voice channel");
                    _audioClient = await voiceChannel.ConnectAsync();
                    if (_songDataQueue.Count > 0 || Service.Get<IServiceFFmpeg>().IsSongPlaying) Service.Get<IServiceFFmpeg>().ForceClose();
                }
                catch (Exception ex)
                {
                    await command.RespondAsync($"Failed to connect to the voice channel: {ex.Message}", ephemeral: true);
                }
            }
        }
        public async Task LeaveVoice(SocketSlashCommand command)
        {
            if (_audioClient == null) return;
            await _audioClient.StopAsync();
            _audioClient = null;
            await command.RespondAsync(text: "Left voice channel.", ephemeral: true);
        }
    }
}
