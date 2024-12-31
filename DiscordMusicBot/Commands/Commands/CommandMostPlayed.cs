using Discord.WebSocket;
using Discord;
using DiscordMusicBot.Commands.Interfaces;
using DiscordMusicBot.Services.Interfaces;
using DiscordMusicBot.Services;
using System.Text;

namespace DiscordMusicBot.Commands.Commands
{
    internal class CommandMostPlayed : IDiscordCommand
    {
        private string _commandName = "mostplayed";
        public string CommandName => _commandName;

        public SlashCommandBuilder Register()
        {
            return new SlashCommandBuilder()
            .WithName(_commandName)
            .WithDescription("Display the most played songs");
        }

        public async Task ExecuteAsync(SocketSlashCommand command)
        {
            var analytics = Service.Get<IServiceAnalytics>();
            var mostPlayedHistory = analytics.AnalyticData.GlobalMostPlayedSongs;
            if (mostPlayedHistory == null || mostPlayedHistory.Count == 0)
            {
                await command.RespondAsync(text: $"There have been no songs recorded yet.", ephemeral: true);
                return;
            }
            var displayText = new StringBuilder();
            displayText.AppendLine("* -- --  -- -- Most Played Songs -- --  -- -- *");
            for (var i = 0; i < mostPlayedHistory.Count; i++)
            {
                var title = $"{mostPlayedHistory[i].SongData.Title} \t\t ({mostPlayedHistory[i].SongData.Url.Insert(5, "\u200B")})";
                var num = mostPlayedHistory[i].NumberOfPlays;
                displayText.AppendLine($"[{num}]    {title}");
            }
            await command.RespondAsync(text: $"{displayText}", ephemeral: true);
        }

    }
}
