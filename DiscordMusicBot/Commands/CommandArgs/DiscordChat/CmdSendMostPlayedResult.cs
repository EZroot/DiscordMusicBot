using System.Diagnostics;
using System.Text;
using Discord;
using Discord.WebSocket;
using DiscordMusicBot.Commands.Interfaces;
using DiscordMusicBot.Models;
using DiscordMusicBot.Services;
using DiscordMusicBot.Services.Interfaces;
using DiscordMusicBot.Utils;

namespace DiscordMusicBot.Commands.CommandArgs.DiscordChat
{
    public class CmdSendMostPlayedResult : ICommand
    {
        private SocketSlashCommand _command;

        public CmdSendMostPlayedResult(SocketSlashCommand command)
        {
            _command = command;
        }

        public async Task ExecuteAsync()
        {
            var analytics = Service.Get<IServiceAnalytics>();
            var mostPlayedHistory = analytics.AnalyticData.GlobalMostPlayedSongs;
            if (mostPlayedHistory == null || mostPlayedHistory.Count == 0)
            {
                await _command.RespondAsync(text: $"There have been no songs recorded yet.", ephemeral: true);
                return;
            }

            var embedBuilder = new EmbedBuilder()
                .WithTitle("Most Popular Songs")
                .WithDescription("A list of the most played songs of the bots life")
                .WithColor(Color.Blue);

            foreach(var kvp in mostPlayedHistory)
            {
                var val = kvp.Value;
                
                var length = val.SongData.Length;
                if (length == "NA") 
                    length = "LIVE!";
                else
                    length = FormatHelper.FormatLengthWithDescriptor(length);

                embedBuilder.AddField($"[{val.NumberOfPlays}] {val.SongData.Title}",
                 $"[{val.SongData.Url.Replace("https://","")}]({val.SongData.Url}) [{length}]", inline: true);
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