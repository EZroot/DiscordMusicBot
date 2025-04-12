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
            Debug.Log($"<color=cyan>Entering SearchYoutube method with query: '{query}', maxResults: {maxResults}</color>");
            try
            {
                var process = "yt-dlp";
                var args = $"ytsearch{maxResults}:\"{query}\" --flat-playlist --print \"title,url,duration\"";
                Debug.Log($"<color=cyan>Starting yt-dlp process: {process} with arguments: {args}</color>");
                var detailCount = 3; // 0: title, 1: url, 2: duration
                var totalResultCount = detailCount * maxResults;

                var result = await ProcessHelper.GetProcessResults(process, args, totalResultCount);
                if (result == null)
                {
                    Debug.Log("<color=red>Process returned null result.</color>");
                    return null;
                }
                Debug.Log($"<color=cyan>yt-dlp process completed with {result.Length} outputs. Expected count: {totalResultCount}</color>");

                if (result.Length < totalResultCount)
                {
                    Debug.Log("<color=red>Failed to get complete results.</color>");
                    return null;
                }

                _searchResults.Clear();
                Debug.Log("<color=cyan>Cleared previous search results.</color>");

                for (var i = 0; i < totalResultCount; i += detailCount)
                {
                    Debug.Log($"<color=cyan>Processing result set starting at index: {i}</color>");
                    var title = result[i];
                    var url = result[i + 1];
                    var durationInMs = result[i + 2];

                    Debug.Log($"<color=cyan>Raw result - Title: '{title}', URL: '{url}', Duration: '{durationInMs}'</color>");

                    if (double.TryParse(durationInMs, out var durationInSeconds))
                    {
                        Debug.Log($"<color=cyan>Parsed duration '{durationInMs}' as seconds: {durationInSeconds}</color>");
                        durationInMs = FormatDuration(durationInSeconds); // Format to "MM:SS" or "HH:MM:SS"
                        Debug.Log($"<color=cyan>Formatted duration: {durationInMs}</color>");
                    }
                    else
                    {
                        Debug.Log("<color=yellow>Duration parsing failed; marking as LIVE!</color>");
                        durationInMs = "LIVE!";
                    }

                    var foundSong = new SongData
                    {
                        Id = Guid.NewGuid().ToString(),
                        Title = title,
                        Url = url,
                        Length = durationInMs
                    };

                    var formatTitle = title.Length > 50 ? title.Substring(0, 42) : title;
                    Debug.Log($"<color=magenta>YT</color>: <color=white>{formatTitle}</color> <color=blue>{url}</color> [<color=white>{durationInMs}</color>]");
                    Debug.Log("<color=cyan>Adding song to search results and history lists.</color>");

                    _searchResults.Add(foundSong);
                    _searchResultsHistory.Add(foundSong);
                }
                Debug.Log($"<color=cyan>Exiting SearchYoutube method with {_searchResults.Count} songs found.</color>");
                return _searchResults;
            }
            catch (Exception ex)
            {
                Debug.Log($"<color=red>Error in SearchYoutube: {ex.Message}</color>");
                return null;
            }
        }

        public async Task StreamToDiscord(IAudioClient client, string videoUrl)
        {
            //Debug.Log($"<color=cyan>Entering StreamToDiscord with videoUrl: {videoUrl}</color>");
            var process = "yt-dlp";
            var args = $"-f bestaudio -g {videoUrl}";
            Debug.Log($"<color=cyan>Starting yt-dlp process for streaming with arguments: {args}</color>");

            var result = await ProcessHelper.GetProcessResults(process, args);
            Debug.Log("<color=cyan>yt-dlp streaming process completed.</color>");

            try
            {
                Debug.Log("<color=cyan>Forwarding stream URL to FFmpeg service.</color>");
                await Service.Get<IServiceFFmpeg>().StreamToDiscord(client, result[0]);
                Debug.Log("<color=cyan>Stream successfully forwarded to Discord.</color>");
            }
            catch (Exception ex)
            {
                Debug.Log($"<color=red>YTDLP ERROR in StreamToDiscord: {ex.Message}</color>");
            }
        }

        public async Task<string> GetSongTitle(string videoUrl)
        {
            Debug.Log($"<color=cyan>Entering GetSongTitle with videoUrl: {videoUrl}</color>");
            var process = "yt-dlp";
            var args = $"--get-title {videoUrl}";
            Debug.Log($"<color=cyan>Starting yt-dlp process to get song title with arguments: {args}</color>");

            var result = await ProcessHelper.GetProcessResults(process, args);
            Debug.Log($"<color=cyan>Retrieved song title: {result[0]}</color>");
            return result[0];
        }

        public async Task<SongData> GetSongDetails(string videoUrl)
        {
            Debug.Log($"<color=cyan>Entering GetSongDetails with videoUrl: {videoUrl}</color>");
            var process = "yt-dlp";
            var args = $"--get-title --get-duration --no-playlist {videoUrl}";
            Debug.Log($"<color=cyan>Starting yt-dlp process for song details with arguments: {args}</color>");

            var result = await ProcessHelper.GetProcessResults(process, args, 2);
            Debug.Log($"<color=cyan>Retrieved song details: Title - {result[0]}, Duration - {result[1]}</color>");
            return new SongData() { Title = result[0], Length = result[1], Url = videoUrl };
        }

        public bool IsYouTubeUrl(string url)
        {
            Debug.Log($"<color=cyan>Checking if URL is a YouTube URL: {url}</color>");
            bool isYouTube = url.Contains("youtube.com") || url.Contains("youtu.be");
            Debug.Log($"<color=cyan>IsYouTubeUrl result: {isYouTube}</color>");
            return isYouTube;
        }

        private string FormatDuration(double seconds)
        {
            Debug.Log($"<color=cyan>Formatting duration from seconds: {seconds}</color>");
            TimeSpan time = TimeSpan.FromSeconds(seconds);
            var formattedTime = time.ToString(time.TotalHours >= 1 ? @"hh\:mm\:ss" : @"mm\:ss");
            Debug.Log($"<color=cyan>Formatted duration is: {formattedTime}</color>");
            return formattedTime;
        }
    }
}
