using Discord.WebSocket;
using Discord;
using DiscordMusicBot.Commands.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DiscordMusicBot.Services;
using DiscordMusicBot.Services.Interfaces;

namespace DiscordMusicBot.Commands.Commands
{
    internal class CommandVolume : IDiscordCommand
    {
        private string _commandName = "volume";
        public string CommandName => _commandName;

        public SlashCommandBuilder Register()
        {
            return new SlashCommandBuilder()
            .WithName(_commandName)
            .WithDescription("Joins a voice channel and plays audio")
            .AddOption("vol", ApplicationCommandOptionType.Number, "volume (1-100)", isRequired: true);
        }

        public async Task ExecuteAsync(SocketSlashCommand command)
        {
            await Service.Get<IServiceAudioManager>().ChangeVolume(command);
        }

    }
}
