using Discord.Audio;
using DiscordMusicBot.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordMusicBot.Services.Interfaces
{
    internal interface IServiceYtdlp : IService
    {
        List<SearchResultData> SearchResults { get; }
        Task<List<SearchResultData>>? SearchYoutube(string query, int maxResults = 5);
        Task StreamToDiscord(IAudioClient client, string videoUrl);
        Task<string> GetSongTitle(string videoUrl);
        bool IsYouTubeUrl(string url);
    }
}
