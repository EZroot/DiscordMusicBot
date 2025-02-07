using Discord.Audio;
using DiscordMusicBot.Models;
using DiscordMusicBot.Services.Interfaces;
using System.Diagnostics;
using Debug = DiscordMusicBot.Utils.Debug;

namespace DiscordMusicBot.Services.Managers.Audio.ExternalProcesses
{
    internal class Ytdlp : IServiceYtdlp
    {
        private List<SongData> _searchResults = new List<SongData>();
        private List<SongData> _searchResultsHistory = new List<SongData>();

        public List<SongData> SearchResults => _searchResults;
        public List<SongData> SearchResultsHistory => _searchResultsHistory;

        public async Task<List<SongData>?> SearchYoutube(string query, int maxResults = 5)
        {
            try
            {
                var process = "yt-dlp";
                var args = $"ytsearch{maxResults}:\"{query}\" --flat-playlist --print \"title,url,duration\"";
                var detailCount = 3; // 0: title, 1: url, 2: duration
                var totalResultCount = detailCount * maxResults;
                var result = await ProcessHelper.GetProcessResults(process, args, totalResultCount);

                if (result == null || result.Length < totalResultCount)
                {
                    Debug.Log("<color=red>Failed to get complete results.</color>");
                    return null;
                }

                _searchResults.Clear();

                for (var i = 0; i < totalResultCount; i += detailCount)
                {
                    var title = result[i];
                    var url = result[i + 1];
                    var durationInMs = result[i + 2];

                    if (double.TryParse(durationInMs, out var durationInSeconds))
                        durationInMs = FormatDuration(durationInSeconds); // Format to "MM:SS" or "HH:MM:SS"
                    else
                        durationInMs = "LIVE!";

                    var foundSong = new SongData
                    {
                        Id = Guid.NewGuid().ToString(),
                        Title = title,
                        Url = url,
                        Length = durationInMs
                    };

                    var formatTitle = title.Length > 50 ? title.Substring(0, 42) : title;
                    Debug.Log($"<color=magenta>YT</color>: <color=white>{formatTitle}</color> <color=blue>{url}</color> [<color=white>{durationInMs}</color>]");

                    _searchResults.Add(foundSong);
                    _searchResultsHistory.Add(foundSong);
                }
                return _searchResults;
            }
            catch (Exception ex)
            {
                Debug.Log($"<color=red>Error: {ex.Message}</color>");
                return null;
            }
        }

        public async Task StreamToDiscord(IAudioClient client, string videoUrl)
        {
            var process = "yt-dlp";
            var args = $"-f bestaudio -g {videoUrl}";
            var result = await ProcessHelper.GetProcessResults(process, args);
            try
            {
                await Service.Get<IServiceFFmpeg>().StreamToDiscord(client, result[0]);
            }
            catch (Exception ex)
            {
                Debug.Log("<color=red>YTDLP ERROR: " + ex.Message);
            }
        }

        public async Task<string> GetSongTitle(string videoUrl)
        {
            var process = "yt-dlp";
            var args = $"--get-title {videoUrl}";
            var result = await ProcessHelper.GetProcessResults(process, args);
            return result[0];
        }

        public async Task<SongData> GetSongDetails(string videoUrl)
        {
            var process = "yt-dlp";
            var args = $"--get-title --get-duration --no-playlist  {videoUrl}";
            var result = await ProcessHelper.GetProcessResults(process, args, 2);
            return new SongData() { Title = result[0], Length = result[1], Url = videoUrl };
        }

        public bool IsYouTubeUrl(string url)
        {
            return url.Contains("youtube.com") || url.Contains("youtu.be");
        }

        private string FormatDuration(double seconds)
        {
            TimeSpan time = TimeSpan.FromSeconds(seconds);
            return time.ToString(time.TotalHours >= 1 ? @"hh\:mm\:ss" : @"mm\:ss");
        }
    }
}
