using Discord.WebSocket;
using DiscordMusicBot2.Services.Interface;

namespace DiscordMusicBot2.Chat.Interface
{
    internal interface IServiceSlashCommands : IService
    {
        Task ExecuteCommand(SocketSlashCommand slashCommand);
        Task RegisterAllCommands(SocketGuild guild);
    }
}
