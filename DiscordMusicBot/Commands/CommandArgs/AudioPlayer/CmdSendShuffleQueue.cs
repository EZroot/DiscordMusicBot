using DiscordMusicBot.Commands.Interfaces;
using DiscordMusicBot.Services;
using DiscordMusicBot.Services.Interfaces;
using DiscordMusicBot.Services.Managers.Audio;

namespace DiscordMusicBot.Commands.CommandArgs.AudioPlayer
{
    public class CmdSendShuffleQueue : ICommand
    {
        private ThreadSafeSongQueue _audioQueuer;
        public CmdSendShuffleQueue(ThreadSafeSongQueue audioQueuer)
        {
            _audioQueuer = audioQueuer;
        }

        public async Task ExecuteAsync()
        {
            await _audioQueuer.ShuffleQueue();
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