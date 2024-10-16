using Discord.WebSocket;
using DiscordMusicBot.Commands.Interfaces;
using Discord;

namespace DiscordMusicBot.Commands.Commands
{
    internal class CommandRadio : IDiscordCommand
    {
        private string _commandName = "radio";
        public string CommandName => _commandName;

        public SlashCommandBuilder Register()
        {
            return new SlashCommandBuilder()
            .WithName(_commandName)
            .WithDescription("Display radio stations available");
        }

        public async Task ExecuteAsync(SocketSlashCommand command)
        {
            var radios = new string[] {
                    "KRock (CB) - http://newcap.leanstream.co/CKXXFM",
                    "KRock (Gander) - http://newcap.leanstream.co/CKXDFM",
                    "OZFM - https://ozfm.streamb.live/SB00174",
                    "Classic Rock - https://cast1.torontocast.com:4610/stream" };
            var message = "";
            for (int i = 0; i < radios.Length; i++)
            {
                string? radio = radios[i];
                message += $"#{i + 1} {radio}\n";
            }
            message += "***Paste one of these in /play***";
            await command.RespondAsync($"**Stations**\n{message}", ephemeral: true);
        }

    }
}
