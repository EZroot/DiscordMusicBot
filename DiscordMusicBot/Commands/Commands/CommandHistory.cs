using Discord.WebSocket;
using Discord;
using DiscordMusicBot.Commands.Interfaces;
using DiscordMusicBot.Services.Interfaces;
using DiscordMusicBot.Services;
using System;
using System.Text;

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
            var analytics = Service.Get<IServiceAnalytics>();
            var recentHistory = analytics.AnalyticData.RecentSongHistory;
            if(recentHistory == null || recentHistory.Length == 0)
            {
                await command.RespondAsync(text: $"There are no songs in recent history.", ephemeral: true);
                return;
            }
            var displayText = new StringBuilder();
            displayText.AppendLine("* -- --  -- -- Recent Song History -- --  -- -- *");
            for (var i = 0; i < recentHistory.Length; i++)
            {
                var title = recentHistory[i].Title;
                if(title != null && title != "null")
                    displayText.AppendLine($"{i}#   {title}");
            }
            await command.RespondAsync(text: $"{displayText}", ephemeral: true);
        }

    }
}
