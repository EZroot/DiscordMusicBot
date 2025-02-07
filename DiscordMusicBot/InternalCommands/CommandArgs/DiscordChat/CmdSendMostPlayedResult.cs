using System.Diagnostics;
using System.Text;
using Discord;
using Discord.WebSocket;
using DiscordMusicBot.InternalCommands.Interfaces;
using DiscordMusicBot.Models;
using DiscordMusicBot.Services;
using DiscordMusicBot.Services.Interfaces;
using DiscordMusicBot.Utils;

namespace DiscordMusicBot.InternalCommands.CommandArgs.DiscordChat
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
            var mostPlayedHistory = analytics.GetTopGlobalSongs();
            if (mostPlayedHistory == null || mostPlayedHistory.Count == 0)
            {
                await _command.RespondAsync(text: $"There have been no songs recorded yet.", ephemeral: true);
                return;
            }

            var embedBuilder = new EmbedBuilder()
                .WithTitle("Most Popular Songs")
                .WithDescription("A list of the most played songs of the bots life")
                .WithColor(Color.Blue);

            foreach(var sad in mostPlayedHistory)
            {
                
                var length = sad.SongData.Length;
                if (length == "NA") 
                    length = "LIVE!";
                else
                    length = FormatHelper.FormatLengthWithDescriptor(length);

                embedBuilder.AddField($"[{sad.NumberOfPlays}] {sad.SongData.Title}",
                 $"[{sad.SongData.Url.Replace("https://","")}]({sad.SongData.Url}) [{length}]", inline: true);
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