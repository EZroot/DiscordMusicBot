using DiscordMusicBot.Models;
namespace DiscordMusicBot.Services.Interfaces
{
    internal interface IServiceDataManager : IService
    {
        BotData LoadConfig();
        AnalyticData LoadAnalytics();
        Task SaveAnalytics(AnalyticData data);

    }
}
