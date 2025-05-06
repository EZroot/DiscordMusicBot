using Discord.WebSocket;
using Discord;
using DiscordMusicBot2.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DiscordMusicBot2.Chat.Commands.Interface;
using DiscordMusicBot2.Audio.Interface;

namespace DiscordMusicBot2.Chat.Commands
{
    internal class CommandVolume : IBotCommand
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
            var option = command.Data.Options.First();
            var volume = (float)(double)option.Value;
            var result = await Service.Get<IServiceAudio>().ChangeVolume(volume);
            if (result)
            {
                await command.RespondAsync(CommandName + " executed", ephemeral: true);
            }
            else
            {
                await command.RespondAsync("I need to be in voice before you can change my volume.", ephemeral: true);
            }
        }
    }
}
