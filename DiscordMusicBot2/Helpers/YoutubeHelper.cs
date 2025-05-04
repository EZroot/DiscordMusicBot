using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordMusicBot2.Helpers
{
    public static class YoutubeHelper
    {
        /// <summary>
        /// Returns Title [0] and Duration [1]
        /// </summary>
        /// <param name="videoUrl"></param>
        /// <returns></returns>
        public static async Task<string[]> GetVideoDetails(string videoUrl)
        {
            var lineCount = 2;
            var output = new string[lineCount];
            try
            {
                var ytDlpProcess = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "yt-dlp",
                        Arguments = $"--get-title --get-duration --no-playlist {videoUrl}",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    }
                };

                ytDlpProcess.Start();

                for (var i = 0; i < lineCount; i++)
                {
                    output[i] = await ytDlpProcess.StandardOutput.ReadLineAsync();
                }

                await ytDlpProcess.WaitForExitAsync();

                if (ytDlpProcess.ExitCode != 0)
                {
                    string error = await ytDlpProcess.StandardError.ReadToEndAsync();
                    Debug.Log($"<color=red>ERROR: {error}");
                }
            }
            catch (Exception e)
            {
                Debug.Log("<color=red>ERROR: " + e.Message);
            }
            return output;
        }

        /// <summary>
        /// Parses “mm:ss” or “hh:mm:ss” (or “m:ss” etc) into a TimeSpan.
        /// Returns null on any parse error.
        /// </summary>
        public static TimeSpan? ParseDuration(string? duration)
        {
            if (string.IsNullOrWhiteSpace(duration))
                return null;

            var parts = duration.Split(':');
            // reject anything strange
            if (parts.Length < 2 || parts.Length > 3)
                return null;

            // try parse every segment
            if (!parts.All(p => int.TryParse(p, out _)))
                return null;

            int hours = 0, mins = 0, secs = 0;
            if (parts.Length == 2)
            {
                // mm:ss
                mins = int.Parse(parts[0]);
                secs = int.Parse(parts[1]);
            }
            else
            {
                // hh:mm:ss
                hours = int.Parse(parts[0]);
                mins = int.Parse(parts[1]);
                secs = int.Parse(parts[2]);
            }

            return new TimeSpan(hours, mins, secs);
        }
    }
}
