using System.Diagnostics;
using System.Text;
using Discord;
using Discord.WebSocket;
using DiscordMusicBot.Commands.Interfaces;
using DiscordMusicBot.Models;
using DiscordMusicBot.Services;
using DiscordMusicBot.Services.Interfaces;
using DiscordMusicBot.Utils;

namespace DiscordMusicBot.Commands.CommandArgs
{
    public class CmdSendQueueResult : ICommand
    {
        private const int QUEUE_DISPLAY_LIMIT = 10;

        private SocketSlashCommand _command;
        private SongData[] _songList;

        public CmdSendQueueResult(SocketSlashCommand command, SongData[] songList)
        {
            _command = command;
            _songList = songList;
        }

        public async Task ExecuteAsync()
        {
            var embedBuilder = new EmbedBuilder()
                // .WithTitle("Queued Songs")
                // .WithDescription("Here are the songs in the queue:")
                .WithColor(Color.Blue);

            if(_songList.Length == 0) embedBuilder.AddField("No songs playing", "):");
            for (int i = 0; i < _songList.Length; i++)
            {
                string title = _songList[i].Title;
                string length = _songList[i].Length;
                if (length == "NA") 
                    length = "LIVE!";
                else
                    length = FormatHelper.FormatLengthWithDescriptor(length);
                    
                if (i == 0)
                {
                    embedBuilder.AddField("Now Playing", $"{title} [{length}]", inline: false);
                }
                else
                {
                    embedBuilder.AddField("Queue Position #" + (i + 1), $"{title} [{length}]", inline: true);
                }

                if (i > QUEUE_DISPLAY_LIMIT)
                {
                    embedBuilder.AddField("More Songs", "------------------------ More Hidden ------------------------", inline: false);
                    break;
                }
            }

            // Send the constructed queue display as an ephemeral embed message
            await _command.RespondAsync(embed: embedBuilder.Build(), ephemeral: true);
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