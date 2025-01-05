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
    public class CmdSendHistoryResult : ICommand
    {
        private SocketSlashCommand _command;

        public CmdSendHistoryResult(SocketSlashCommand command)
        {
            _command = command;
        }

        public async Task ExecuteAsync()
        {
           var analytics = Service.Get<IServiceAnalytics>();
            var recentHistory = analytics.AnalyticData.RecentSongHistory;
            if (recentHistory == null || recentHistory.Count == 0)
            {
                await _command.RespondAsync(text: $"There are no songs in recent history.", ephemeral: true);
                return;
            }
            var embedBuilder = new EmbedBuilder()
                .WithTitle("Recent Song History")
                .WithDescription("The last 10 or so played songs")
                .WithColor(Color.Blue);
            for (var i = 0; i < recentHistory.Count; i++)
            {
                try
                {
                    var length = recentHistory[i].Length;
                    if (length == "NA") 
                        length = "LIVE!";
                    else
                        length = FormatHelper.FormatLengthWithDescriptor(length);

                    embedBuilder.AddField($"{recentHistory[i].Title}",
                     $"[{recentHistory[i].Url.Replace("https://","")}]({recentHistory[i].Url}) [{length}]", false);
                }
                catch (Exception e)
                {
                    Utils.Debug.Log("<color=red>Error: " + e.Message);
                }
            }
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