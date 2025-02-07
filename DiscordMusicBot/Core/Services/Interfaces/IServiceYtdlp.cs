using Discord.Audio;
using DiscordMusicBot.Models;

namespace DiscordMusicBot.Services.Interfaces
{
    internal interface IServiceYtdlp : IService
    {
        List<SongData> SearchResults { get; }
        List<SongData> SearchResultsHistory { get; }
        Task<List<SongData>>? SearchYoutube(string query, int maxResults = 5);
        Task StreamToDiscord(IAudioClient client, string videoUrl);
        Task<string> GetSongTitle(string videoUrl);
        Task<SongData> GetSongDetails(string videoUrl);
        bool IsYouTubeUrl(string url);
    }
}
