using Discord.Audio;
using DiscordMusicBot.InternalCommands.Interfaces;
using DiscordMusicBot.Events;
using DiscordMusicBot.Events.EventArgs;
using DiscordMusicBot.Models;
using DiscordMusicBot.Services;
using DiscordMusicBot.Services.Interfaces;
using DiscordMusicBot.Services.Managers.Audio;
using DiscordMusicBot.Utils;

namespace DiscordMusicBot.InternalCommands.CommandArgs.AudioPlayer
{
    public class CmdSendAddSong : ICommand
    {
        private IAudioClient _audioClient;
        private ThreadSafeSongQueue _audioQueuer;
        private SongData _song;

        public CmdSendAddSong(IAudioClient client, ThreadSafeSongQueue audioQueuer, SongData song)
        {
            _audioClient = client;
            _audioQueuer = audioQueuer;
            _song = song;
        }

        public async Task ExecuteAsync()
        {
            //This needs to be redone, we should seperate outthe queueing of songs
            //and we should also come up with a better solution for streaming audio atm
            //when we queue a song, the stream starss, which will lock queueing for other songs till the stream stops or 'task' tops or whatever
            //you can see it happen if you remove the streaming from audiomanager, and just queue up songs normally
            var song = new SongData { Title = _song.Title, Url = _song.Url, Length = _song.Length };
            _audioQueuer.Enqueue(song);
            Debug.Log($"AudioQueue: Added to queue: {song.Title} {song.Url}");
            if (_audioQueuer.SongCount == 1 && !Service.Get<IServiceFFmpeg>().IsSongPlaying)
            {
                _audioQueuer.CurrentPlayingSong = _audioQueuer.Dequeue();
                Debug.Log("If you see this multiple times we got a problem");
                if (_audioQueuer.CurrentPlayingSong != null)
                {
                    var formatTitle = _song.Title.Length > 50 ? _song.Title.Substring(0, 42) : _song.Title;
                    Debug.Log($"<color=magenta>Unqueued & attempting to play</color>: <color=white>{formatTitle} [{_audioQueuer.CurrentPlayingSong.Value.Length}]</color>");
                    EventHub.Raise(new EvOnPlayNextSong() { Title = _audioQueuer.CurrentPlayingSong.Value.Title, Url = _audioQueuer.CurrentPlayingSong.Value.Url, Length = _audioQueuer.CurrentPlayingSong.Value.Length });
                }
            }
            await Task.CompletedTask;
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