using DiscordMusicBot.Models;
namespace DiscordMusicBot.Services.Interfaces
{
    internal interface IServiceAnalytics : IService
    {
        AnalyticData AnalyticData { get; }
        Task Initialize();
        Task AddSongAnalytics(string userName, SongData songData);
    }
}
