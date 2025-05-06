using Discord.WebSocket;
using Discord;
using DiscordMusicBot2.Audio.Interface;
using DiscordMusicBot2.Chat.Commands.Interface;
using DiscordMusicBot2.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordMusicBot2.Chat.Commands
{
    internal class CommandSkip : IBotCommand
    {
        public string CommandName => "skip";
        public SlashCommandBuilder Register()
        {
            return new SlashCommandBuilder()
                       .WithName(CommandName)
                       .WithDescription("skips the god damn mother fuckin song");
        }

        public async Task ExecuteAsync(SocketSlashCommand options)
        {
            await options.DeferAsync(ephemeral:true);
            await Service.Get<IServiceAudio>().Skip();
            await options.FollowupAsync(text: "Skipped the current track!", ephemeral: true);
        }
    }
}
