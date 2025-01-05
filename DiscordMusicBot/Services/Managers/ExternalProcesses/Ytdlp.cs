using Discord.Audio;
using DiscordMusicBot.Models;
using DiscordMusicBot.Services.Interfaces;
using System.Diagnostics;
using Debug = DiscordMusicBot.Utils.Debug;

namespace DiscordMusicBot.Services.Managers.ExternalProcesses
{
    internal class Ytdlp : IServiceYtdlp
    {
        private List<SongData> _searchResults = new List<SongData>();
        public List<SongData> SearchResults => _searchResults;

        private List<SongData> _searchResultsHistory = new List<SongData>();
        public List<SongData> SearchResultsHistory => _searchResultsHistory;

        public async Task<List<SongData>>? SearchYoutube(string query, int maxResults = 5)
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "yt-dlp",
                    Arguments = $"ytsearch{maxResults}:\"{query}\" --flat-playlist --print \"title,url,duration\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };

            try
            {
                process.Start();
                string output = await process.StandardOutput.ReadToEndAsync();
                string error = await process.StandardError.ReadToEndAsync();
                process.WaitForExit();

                if (process.ExitCode != 0)
                {
                    Debug.Log($"yt-dlp error: {error}");
                    return _searchResults;
                }

                _searchResults.Clear();

                string[] lines = output.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);

                for (int i = 0; i < lines.Length; i += 3)
                {
                    // Check if there are enough lines to process the title, URL, and duration
                    if (i + 2 < lines.Length)
                    {
                        var title = lines[i].Trim();
                        var url = lines[i + 1].Trim();
                        var durationFormatted = lines[i + 2].Trim();
                        if(double.TryParse(durationFormatted, out var durationInSeconds))  // Parse the duration as double
                        {
                            durationFormatted = FormatDuration(durationInSeconds); // Format the duration to "MM:SS" or "HH:MM:SS"
                        }else{
                            //assume its live if it cant be parsed
                            durationFormatted = "LIVE!";
                        }

                        var foundSong = new SongData
                        {
                            Id = Guid.NewGuid().ToString(),
                            Title = title,
                            Url = url,
                            Length = durationFormatted
                        };

                        _searchResults.Add(foundSong);
                        _searchResultsHistory.Add(foundSong);
                    }
                    else
                    {
                        Debug.Log("Skipping incomplete search result entry. Output may be incorrectly formatted.");
                    }
                }

            }
            catch (Exception ex)
            {
                Debug.Log($"Exception occurred: {ex.Message}");
            }
            return _searchResults;
        }

        public async Task StreamToDiscord(IAudioClient client, string videoUrl)
        {
            var ytDlpProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "yt-dlp",
                    Arguments = $"-f bestaudio -g {videoUrl}",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                }
            };

            ytDlpProcess.Start();
            string streamUrl = await ytDlpProcess.StandardOutput.ReadLineAsync();
            await ytDlpProcess.WaitForExitAsync();

            if (ytDlpProcess.ExitCode != 0)
            {
                string error = await ytDlpProcess.StandardError.ReadToEndAsync();
                Debug.Log($"yt-dlp error: {error}");
                return;
            }

            try
            {
                await Service.Get<IServiceFFmpeg>().StreamToDiscord(client, streamUrl);
            }
            catch (Exception ex)
            {
            }
        }

        public async Task<string> GetSongTitle(string videoUrl)
        {
            var ytDlpProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "yt-dlp",
                    Arguments = $"--get-title {videoUrl}",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                }
            };

            ytDlpProcess.Start();
            string title = await ytDlpProcess.StandardOutput.ReadLineAsync();
            await ytDlpProcess.WaitForExitAsync();

            if (ytDlpProcess.ExitCode != 0)
            {
                string error = await ytDlpProcess.StandardError.ReadToEndAsync();
                Debug.Log($"yt-dlp error: {error}");
                return null;
            }

            return title;
        }

        public async Task<SongData> GetSongDetails(string videoUrl)
        {
            var ytDlpProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "yt-dlp",
                    Arguments = $"--get-title --get-duration --no-playlist  {videoUrl}",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                }
            };

            ytDlpProcess.Start();
            string title = await ytDlpProcess.StandardOutput.ReadLineAsync();
            string duration = await ytDlpProcess.StandardOutput.ReadLineAsync();
            await ytDlpProcess.WaitForExitAsync();

            if (ytDlpProcess.ExitCode != 0)
            {
                string error = await ytDlpProcess.StandardError.ReadToEndAsync();
                Debug.Log($"yt-dlp error: {error}");
            }

            return new SongData() { Title = title, Length = duration, Url = videoUrl };
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
