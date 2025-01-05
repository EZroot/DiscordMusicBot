using Discord;
using Discord.Audio;
using Discord.WebSocket;
using DiscordMusicBot.Models;
using DiscordMusicBot.Services.Interfaces;
using DiscordMusicBot.Utils;
using DiscordMusicBot.InternalCommands;
using DiscordMusicBot.InternalCommands.CommandArgs.DiscordChat;
using DiscordMusicBot.InternalCommands.CommandArgs.AudioPlayer;

namespace DiscordMusicBot.Services.Managers.Audio
{
    internal class AudioPlaybackService : IServiceAudioPlaybackService
    {
        private IAudioClient? _audioClient;
        private readonly ThreadSafeSongQueue _audioQueuer = new ThreadSafeSongQueue();
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
                var songData = new SongData() { Id = "NA", Title = songDetails.Title, Url = videoUrl, Length = songDetails.Length };
                await PlaySong(songData);
                return;
            }
            else
            {
                Debug.Log($"<color=magenta>{user}:></color> <color=red>'{videoUrl}' Invalid url.</color>");
                await command.RespondAsync(text: $"`{videoUrl}` is not a valid youtube url.", ephemeral: true);
            }
        }

        public async Task PlaySong(SongData songData)
        {
            await CommandHub.ExecuteCommand(new CmdSendAddSong(_audioClient, _audioQueuer, songData));


            //It seems like this holds up the queue option, or something similar,
            //we need to spawn stream to discord off in a new task, but we need to manage it as well...
            
            try
            {
                await Service.Get<IServiceYtdlp>().StreamToDiscord(_audioClient, songData.Url);
            }
            catch (Exception ex)
            {
                // Log exceptions that may occur during the streaming process
                Debug.Log($"<color=red>Error during streaming the song:</color> Title = <color=cyan>{songData.Title}</color>, URL = <color=magenta>{songData.Url}</color>. <color=red>Exception: {ex.Message}</color>");
            }
        }

        public async Task PlayNextSong(IAudioClient client)
        {
            await CommandHub.ExecuteCommand(new CmdSendPlayNextSong(_audioClient, _audioQueuer));
        }

        public async Task SongQueue(SocketSlashCommand command)
        {
            var songArr = _audioQueuer.SongQueueArray;
            await CommandHub.ExecuteCommand(new CmdSendQueueResult(command, songArr));
        }

        public async Task ShuffleQueue(SocketSlashCommand command)
        {
            if(_audioQueuer.SongCount > 0)
            {
                await CommandHub.ExecuteCommand(new CmdSendShuffleQueue(_audioQueuer));
                await command.RespondAsync("Queued has been shuffled!", ephemeral: true);
                await CommandHub.ExecuteCommand(new CmdSendQueueResult(command, _audioQueuer.SongQueueArray));
                return;
            }

            await command.RespondAsync("There are no songs available to shuffle...", ephemeral: true);
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
            var volume = (string)option.Value;
            if(double.TryParse(volume, out var volumeD))
            {
                await CommandHub.ExecuteCommand(new CmdSendSetVolume((float)volumeD));
                await command.RespondAsync(text: $"Volume set: {volumeD.ToString("0")}/100", ephemeral: true);
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
                    Debug.Log($"<color=magenta>{user.Username}</color>:> Bot joined voice channel");
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
