using Discord;
using Discord.Audio;
using Discord.WebSocket;
using DiscordMusicBot.Events.EventArgs;
using DiscordMusicBot.Events;
using DiscordMusicBot.Models;
using DiscordMusicBot.Services.Interfaces;
using DiscordMusicBot.Utils;
using DiscordMusicBot.Commands;
using DiscordMusicBot.Commands.CommandArgs.DiscordChat;

namespace DiscordMusicBot.Services.Managers.Audio
{
    internal class AudioManager : IServiceAudioManager
    {
        private IAudioClient? _audioClient;
        private readonly AudioQueuer _audioQueuer = new AudioQueuer();
        public int SongCount => _audioQueuer.SongCount;

        public async Task PlaySong(SocketSlashCommand command)
        {
            await CheckAndJoinVoice(command);

            var urlOption = command.Data.Options.First();
            string videoUrl = urlOption?.Value?.ToString();
            var user = command.User.Username;
            
            if (Service.Get<IServiceYtdlp>().IsYouTubeUrl(videoUrl))
            {
                await command.RespondAsync(text: $"Searching: `{videoUrl}`", ephemeral: true);
                var songDetails = await Service.Get<IServiceYtdlp>().GetSongDetails(videoUrl);
                await Service.Get<IServiceAnalytics>().AddSongAnalytics(user, new SongData { Title = songDetails.Title, Url = videoUrl, Length = songDetails.Length });
                await command.ModifyOriginalResponseAsync((m) => m.Content = $"Added **{songDetails.Title}** to Queue!");
                await PlaySong(songDetails.Title, videoUrl, songDetails.Length);
                return;
            }
            else
            {
                Debug.Log($"'{videoUrl}' Invalid url.");
                await command.RespondAsync(text: $"`{videoUrl}` is not a valid youtube url.", ephemeral: true);
            }
        }

        public async Task PlaySong(string title, string url, string length)
        {
            // _songDataQueue.Enqueue(new SongData { Title = title, Url = url, Length = length });
            _audioQueuer.Enqueue(new SongData { Title = title, Url = url, Length = length });
            if (_audioQueuer.SongCount == 1 && !Service.Get<IServiceFFmpeg>().IsSongPlaying)
            {
                _audioQueuer.CurrentPlayingSong = _audioQueuer.Dequeue();
                if(_audioQueuer.CurrentPlayingSong != null)
                {
                    var formatTitle = title.Length > 50 ? title.Substring(0,42) : title;
                    Debug.Log($"<color=magenta>Attempting to play</color>: <color=white>{formatTitle} [{_audioQueuer.CurrentPlayingSong.Value.Length}]</color>");
                    EventHub.Raise(new EvOnPlayNextSong() { Title = _audioQueuer.CurrentPlayingSong.Value.Title, Url = _audioQueuer.CurrentPlayingSong.Value.Url, Length = _audioQueuer.CurrentPlayingSong.Value.Length });
                }
            }

            try
            {
                await Service.Get<IServiceYtdlp>().StreamToDiscord(_audioClient, url);
            }
            catch (Exception ex)
            {
                // Log exceptions that may occur during the streaming process
                Debug.Log($"<color=red>Error during streaming the song:</color> Title = <color=cyan>{title}</color>, URL = <color=magenta>{url}</color>. <color=red>Exception: {ex.Message}</color>");
            }
        }

        public async Task PlayNextSong(IAudioClient client)
        {
            if (_audioQueuer.SongCount == 0) { return; }
            _audioQueuer.CurrentPlayingSong = _audioQueuer.Dequeue();
            if(_audioQueuer.CurrentPlayingSong != null)
            {
                var title = _audioQueuer.CurrentPlayingSong.Value.Title;
                var formatTitle = title.Length > 50 ? title.Substring(0,42) : title;
                Debug.Log($"<color=magenta>Attempting to play</color>: <color=white>{formatTitle} [{_audioQueuer.CurrentPlayingSong.Value.Length}]</color>");
                EventHub.Raise(new EvOnPlayNextSong() { Title = _audioQueuer.CurrentPlayingSong.Value.Title, Url = _audioQueuer.CurrentPlayingSong.Value.Url, Length = _audioQueuer.CurrentPlayingSong.Value.Length });
                await Service.Get<IServiceYtdlp>().StreamToDiscord(_audioClient, _audioQueuer.CurrentPlayingSong.Value.Url);
            }
            await Task.CompletedTask;
        }

        public async Task SongQueue(SocketSlashCommand command)
        {
            var songArr = _audioQueuer.SongQueue;
            await CommandHub.ExecuteCommand(new CmdSendQueueResult(command, songArr));
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
            var volume = (float)(double)(option?.Value);
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
                    if (_audioQueuer.SongCount > 0 || Service.Get<IServiceFFmpeg>().IsSongPlaying) Service.Get<IServiceFFmpeg>().ForceClose();
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
