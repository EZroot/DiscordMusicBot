using Discord;
using Discord.Audio;
using Discord.WebSocket;
using DiscordMusicBot.Commands.Interfaces;
using DiscordMusicBot.Events;
using DiscordMusicBot.Events.EventArgs;
using DiscordMusicBot.Services;
using DiscordMusicBot.Services.Interfaces;
using DiscordMusicBot.Services.Managers.Audio;
using DiscordMusicBot.Utils;

namespace DiscordMusicBot.Commands.CommandArgs.AudioPlayer
{
    public class CmdSendPlayNextSong : ICommand
    {
        private AudioQueuer _audioQueuer;
        private IAudioClient _client;

        public CmdSendPlayNextSong(IAudioClient client, AudioQueuer audioQueuer)
        {
            _audioQueuer = audioQueuer;
            _client = client;
        }

        public async Task ExecuteAsync()
        {
            if (_audioQueuer.SongCount == 0) { return; }
            _audioQueuer.CurrentPlayingSong = _audioQueuer.Dequeue();
            if (_audioQueuer.CurrentPlayingSong != null)
            {
                var title = _audioQueuer.CurrentPlayingSong.Value.Title;
                var formatTitle = title.Length > 50 ? title.Substring(0, 42) : title;
                Debug.Log($"<color=magenta>Attempting to play</color>: <color=white>{formatTitle} [{_audioQueuer.CurrentPlayingSong.Value.Length}]</color>");
                EventHub.Raise(new EvOnPlayNextSong() { Title = _audioQueuer.CurrentPlayingSong.Value.Title, Url = _audioQueuer.CurrentPlayingSong.Value.Url, Length = _audioQueuer.CurrentPlayingSong.Value.Length });
                await Service.Get<IServiceYtdlp>().StreamToDiscord(_client, _audioQueuer.CurrentPlayingSong.Value.Url);
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