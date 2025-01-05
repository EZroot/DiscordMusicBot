using Discord.Audio;
using DiscordMusicBot.Commands.Interfaces;
using DiscordMusicBot.Events;
using DiscordMusicBot.Events.EventArgs;
using DiscordMusicBot.Models;
using DiscordMusicBot.Services;
using DiscordMusicBot.Services.Interfaces;
using DiscordMusicBot.Services.Managers.Audio;
using DiscordMusicBot.Utils;

namespace DiscordMusicBot.Commands.CommandArgs.AudioPlayer
{
    public class CmdSendAddSong : ICommand
    {
        private IAudioClient _audioClient;
        private AudioQueuer _audioQueuer;
        private SongData _song;

        public CmdSendAddSong(IAudioClient client, AudioQueuer audioQueuer, SongData song)
        {
            _audioClient = client;
            _audioQueuer = audioQueuer;
            _song = song;
        }

        public async Task ExecuteAsync()
        {
            _audioQueuer.Enqueue(new SongData { Title = _song.Title, Url = _song.Url, Length = _song.Length });
            if (_audioQueuer.SongCount == 1 && !Service.Get<IServiceFFmpeg>().IsSongPlaying)
            {
                _audioQueuer.CurrentPlayingSong = _audioQueuer.Dequeue();
                if (_audioQueuer.CurrentPlayingSong != null)
                {
                    var formatTitle = _song.Title.Length > 50 ? _song.Title.Substring(0, 42) : _song.Title;
                    Debug.Log($"<color=magenta>Attempting to play</color>: <color=white>{formatTitle} [{_audioQueuer.CurrentPlayingSong.Value.Length}]</color>");
                    EventHub.Raise(new EvOnPlayNextSong() { Title = _audioQueuer.CurrentPlayingSong.Value.Title, Url = _audioQueuer.CurrentPlayingSong.Value.Url, Length = _audioQueuer.CurrentPlayingSong.Value.Length });
                }
            }
        }


        public async Task Redo()
        {
            Utils.Debug.Log($"<color=red>Error: Redo unavailable in {this}");
            await Task.CompletedTask;
        }

        public async Task Undo()
        {
            Utils.Debug.Log($"<color=red>Error: Undo unavailable in {this}");
            await Task.CompletedTask;
        }
    }
}