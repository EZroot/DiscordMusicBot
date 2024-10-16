using Discord.Audio;
using DiscordMusicBot.Models;
using DiscordMusicBot.Services.Interfaces;
using DiscordMusicBot.Utils;
using System.Diagnostics;
using System.Threading;
using Debug = DiscordMusicBot.Utils.Debug;

namespace DiscordMusicBot.Services.Services.ExternalProcesses
{
    internal class Ytdlp : IServiceYtdlp
    {
        private List<SearchResultData>? _searchResults;
        private bool _resultsReady;

        public List<SearchResultData> SearchResults => _searchResults;
        public async Task<List<SearchResultData>>? SearchYoutube(string query, int maxResults = 5)
        {
            _searchResults = new List<SearchResultData>();
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

                string[] lines = output.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);

                for (int i = 0; i < lines.Length; i += 3)
                {
                    // Check if there are enough lines to process the title, URL, and duration
                    if (i + 2 < lines.Length)
                    {
                        var title = lines[i].Trim();
                        var url = lines[i + 1].Trim();
                        var duration = lines[i + 2].Trim();

                        Debug.Log($"Found: <color=cyan>{title}</color> <color=magenta>{url}</color>");

                        double.TryParse(duration, out double dduration);

                        var durationResult = dduration / 60d;
                        _searchResults.Add(new SearchResultData
                        {
                            Title = title,
                            Url = url,
                            Duration = durationResult.ToString("0.00")
                        });
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
                Console.WriteLine($"yt-dlp error: {error}");
                return null;
            }

            return title;
        }

        public bool IsYouTubeUrl(string url)
        {
            return url.Contains("youtube.com") || url.Contains("youtu.be");
        }
    }
}
