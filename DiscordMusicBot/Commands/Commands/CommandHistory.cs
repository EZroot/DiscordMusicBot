using Discord.WebSocket;
using Discord;
using DiscordMusicBot.Commands.Interfaces;
using DiscordMusicBot.Services.Interfaces;
using DiscordMusicBot.Services;
using System;
using System.Text;
using System.Diagnostics;

namespace DiscordMusicBot.Commands.Commands
{
    internal class CommandHistory : IDiscordCommand
    {
        private string _commandName = "history";
        public string CommandName => _commandName;

        public SlashCommandBuilder Register()
        {
            return new SlashCommandBuilder()
            .WithName(_commandName)
            .WithDescription("Show recent song history");
        }

        public async Task ExecuteAsync(SocketSlashCommand command)
        {
            Utils.Debug.Log($"Trying to show history...");
            var analytics = Service.Get<IServiceAnalytics>();
            var recentHistory = analytics.AnalyticData.RecentSongHistory;
            Utils.Debug.Log($"Got recent history");
            if (recentHistory == null || recentHistory.Length == 0)
            {
                Utils.Debug.Log($"NO SONGS");
                await command.RespondAsync(text: $"There are no songs in recent history.", ephemeral: true);
                return;
            }
            var displayText = new StringBuilder();
            displayText.AppendLine("* -- --  -- -- Recent Song History -- --  -- -- *");
            for (var i = 0; i < recentHistory.Length; i++)
            {
                if (string.IsNullOrEmpty(recentHistory[i].Title)) continue;

                try
                {
                    var title = $"{recentHistory[i].Title} {recentHistory[i].Url.Insert(5, "\u200B")}";
                    if (title != null && title != "null")
                        displayText.AppendLine($"{i}#   {title}");
                }
                catch (Exception e)
                {
                    Utils.Debug.Log("<color=red>Error: " + e.Message);
                }
            }
            await command.RespondAsync(text: $"{displayText}", ephemeral: true);
        }

    }
}
