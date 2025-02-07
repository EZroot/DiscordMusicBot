using Discord;
using Discord.Audio;
using Discord.WebSocket;
using DiscordMusicBot.Models;
using DiscordMusicBot.Services.Interfaces;
using DiscordMusicBot.Utils;
using DiscordMusicBot.InternalCommands;
using DiscordMusicBot.InternalCommands.CommandArgs.DiscordChat;
using DiscordMusicBot.InternalCommands.CommandArgs.AudioPlayer;

/*

    TODO: REMOVE SLASHCOMMAND STUFF FROM THIS WHERE POSSIBLE
    
*/
namespace DiscordMusicBot.Services.Managers.Audio
{
    internal class AudioPlaybackService : IServiceAudioPlaybackService
    {
        private IAudioClient? _audioClient;
        private readonly ThreadSafeSongQueue _audioQueuer = new ThreadSafeSongQueue();
        public int SongCount => _audioQueuer.SongCount;

        public async Task<SongData?> PlaySong(string user, string url)
        {
            var songDetails = await Service.Get<IServiceYtdlp>().GetSongDetails(url);
            await QueueSongToPlay(songDetails);
            return songDetails;
        }

        public async Task QueueSongToPlay(SongData song)
        {
            await CommandHub.ExecuteCommand(new CmdSendAddSong(_audioClient, _audioQueuer, song));
            await PlayNextSong();
        }

        public async Task PlayNextSong()
        {
            await CommandHub.ExecuteCommand(new CmdSendPlayNextSong(_audioClient, _audioQueuer));
        }

        public async Task GetCurrentSongQueue(SocketSlashCommand command)
        {
            var songArr = _audioQueuer.SongQueueArray;
            await CommandHub.ExecuteCommand(new CmdSendQueueResult(command, songArr));
        }

        public async Task ShuffleQueue(SocketSlashCommand command)
        {
            if (_audioQueuer.SongCount > 0)
            {
                await CommandHub.ExecuteCommand(new CmdSendShuffleQueue(_audioQueuer));
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
            var volume = (float)(double)option.Value;
            await CommandHub.ExecuteCommand(new CmdSendSetVolume((float)volume));
            await command.RespondAsync(text: $"Volume set: {volume.ToString("0")}/100", ephemeral: true);
        }

        public async Task JoinVoice(SocketSlashCommand command)
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

        public Task ClearSong()
        {
            Debug.Log("Clearing song..");
            _audioQueuer.CurrentPlayingSong = null;
            return Task.CompletedTask;
        }
    }
}
