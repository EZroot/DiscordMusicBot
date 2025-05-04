using Discord.WebSocket;
using DiscordMusicBot2.Services.Interface;

namespace DiscordMusicBot2.Bot.Interface
{
    internal interface IServiceBot : IService
    {
        SocketGuild? Guild { get; }
        Task StartDiscordBot(string appId, string guildId);
        Task StopDiscordBot();
    }
}
