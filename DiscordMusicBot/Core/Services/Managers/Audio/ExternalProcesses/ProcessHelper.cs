using System.Diagnostics;

namespace DiscordMusicBot.Services.Managers.Audio.ExternalProcesses
{
    public class ProcessHelper
    {
        private const float GLOBAL_VOLUME = 1f;

        public static async Task<string[]> GetProcessResults(string processName, string args, int readLineCount = 1)
        {
            var output = new string[readLineCount];
            try
            {
                var ytDlpProcess = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = processName,
                        Arguments = args,
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    }
                };

                ytDlpProcess.Start();

                for (var i = 0; i < readLineCount; i++)
                {
                    output[i] = await ytDlpProcess.StandardOutput.ReadLineAsync();
                }

                await ytDlpProcess.WaitForExitAsync();

                if (ytDlpProcess.ExitCode != 0)
                {
                    string error = await ytDlpProcess.StandardError.ReadToEndAsync();
                    Utils.Debug.Log($"<color=red>ERROR: {error}");
                }
            }
            catch (Exception e)
            {
                Utils.Debug.Log("<color=red>ERROR: " + e.Message);
            }
            return output;
        }
    
        public static Process CreateStream(string url)
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "ffmpeg",
                    Arguments = $"-hide_banner -loglevel error -reconnect 1 -reconnect_streamed 1 " +
                                $"-reconnect_delay_max 5 -i \"{url}\" " + 
                                $"-af \"loudnorm=I=-16:LRA=11,volume={GLOBAL_VOLUME}\" " + 
                                $"-ac 2 -f s16le -ar 48000 pipe:1",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                },
                EnableRaisingEvents = true
            };


            process.Start();

            Task.Run(async () =>
            {
                try
                {
                    string line;
                    while ((line = await process.StandardError.ReadLineAsync()) != null)
                    {
                        //Ignore errors to avoid console spam
                        // Debug.Log($"STREAM:> <color=yellow>{line}</color>");
                    }
                }
                catch (Exception ex)
                {
                    Utils.Debug.Log($"Error reading ffmpeg output: {ex.Message}");
                }
            });
            return process;
        }
    }
}