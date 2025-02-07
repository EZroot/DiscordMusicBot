using DiscordMusicBot.Models;
namespace DiscordMusicBot.Services.Interfaces
{
    internal interface IServiceAnalytics : IService
    {
        Task InitializeAsync();
        Task AddSongAnalyticsAsync(string userName, SongData songData);
        List<SongAnalyticData> GetTopGlobalSongs(int top = 10);
        List<SongAnalyticData> GetUserTopSongs(string userName, int top = 10);
        List<SongData> GetRecentSongs(int count = 10);
    }
}
