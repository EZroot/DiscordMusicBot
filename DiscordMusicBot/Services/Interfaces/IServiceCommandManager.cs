using Discord.WebSocket;
namespace DiscordMusicBot.Services.Interfaces
{
    internal interface IServiceCommandManager : IService
    {
        Task ExecuteCommand(SocketSlashCommand slashCommand);
        Task RegisterAllCommands(SocketGuild guild);
    }
}
