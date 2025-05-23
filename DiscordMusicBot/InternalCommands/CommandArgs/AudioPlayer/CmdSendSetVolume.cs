using DiscordMusicBot.InternalCommands.Interfaces;
using DiscordMusicBot.Services;
using DiscordMusicBot.Services.Interfaces;

namespace DiscordMusicBot.InternalCommands.CommandArgs.AudioPlayer
{
    public class CmdSendSetVolume : ICommand
    {
        private float _vol;

        public CmdSendSetVolume(float volume)
        {
            _vol = volume;
        }

        public async Task ExecuteAsync()
        {
            if (_vol >= 0 && _vol <= 100)
            {
                _vol = _vol / 100f;
                await Service.Get<IServiceFFmpeg>().SetVolume(_vol);
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