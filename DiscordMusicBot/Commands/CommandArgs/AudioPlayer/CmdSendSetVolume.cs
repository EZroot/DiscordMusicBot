using Discord;
using Discord.WebSocket;
using DiscordMusicBot.Commands.Interfaces;
using DiscordMusicBot.Services;
using DiscordMusicBot.Services.Interfaces;
using DiscordMusicBot.Utils;

namespace DiscordMusicBot.Commands.CommandArgs.AudioPlayer
{
    public class CmdSendSetVolume : ICommand
    {
        private SocketSlashCommand _command;

        public CmdSendSetVolume(SocketSlashCommand command)
        {
            _command = command;
        }

        public async Task ExecuteAsync()
        {
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