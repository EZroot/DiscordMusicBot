using Discord;
using Discord.WebSocket;
using DiscordMusicBot2.Audio.Interface;
using DiscordMusicBot2.Bot.Interface;
using DiscordMusicBot2.Chat.Commands.Interface;
using DiscordMusicBot2.Services;

namespace DiscordMusicBot2.Chat.Commands
{
    internal class CommandLeave : IBotCommand
    {
        public string CommandName => "leave";
        public SlashCommandBuilder Register()
        {
            return new SlashCommandBuilder()
                       .WithName(CommandName)
                       .WithDescription("leave voice channel");
        }

        public async Task ExecuteAsync(SocketSlashCommand options)
        {
            var user = options.User as IGuildUser;
            var currentServer = Service.Get<IServiceBot>().Guild;
            var botUser = currentServer?.CurrentUser as IGuildUser;
            var vc = botUser.VoiceChannel;
            if (vc != null)
            {
                await Service.Get<IServiceAudio>().LeaveCurrentVoiceChannel(vc);
                await options.RespondAsync(text: $"Left {vc.Name}!", ephemeral: true);
            }
            else
            {
                await options.RespondAsync(text: "I am not in a voice channel, idiot", ephemeral: true);
                return;
            }
        }
    }
}
