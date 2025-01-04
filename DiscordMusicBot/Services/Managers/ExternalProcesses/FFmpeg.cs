using Discord.Audio;
using DiscordMusicBot.Events;
using DiscordMusicBot.Events.EventArgs;
using DiscordMusicBot.Services.Interfaces;
using System.Diagnostics;
using Debug = DiscordMusicBot.Utils.Debug;

namespace DiscordMusicBot.Services.Managers.ExternalProcesses
{
    internal class FFmpeg : IServiceFFmpeg
    {
        private const float GLOBAL_VOLUME = 1f;
        Process? _ffmpegProcess = null;

        private float _volume = 0.1f; // Default volume

        public bool IsSongPlaying => _ffmpegProcess != null;

        public async Task SetVolume(float newVolume)
        {
            Debug.Log($"Volume set: {_volume} -> {newVolume} ");
            _volume = newVolume;
            await Task.CompletedTask;
        }

        public Process CreateStream(string url)
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "ffmpeg",
                    Arguments = $"-hide_banner -loglevel error -reconnect 1 -reconnect_streamed 1 -reconnect_delay_max 5 -i \"{url}\" -af \"volume={GLOBAL_VOLUME}\" -ac 2 -f s16le -ar 48000 pipe:1",
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
                        Debug.Log($"FFMPEG:> <color=yellow>{line}</color>");
                    }
                }
                catch (Exception ex)
                {
                    Debug.Log($"Error reading ffmpeg output: {ex.Message}");
                }
            });
            return process;
        }

        public bool ForceClose()
        {
            if (_ffmpegProcess != null)
            {
                _ffmpegProcess.Kill();
                return true;
            }
            return false;
        }

        public async Task StreamToDiscord(IAudioClient client, string url)
        {
            try
            {
                //If song is currently playing, return
                if (_ffmpegProcess != null) { return; }
                _ffmpegProcess = CreateStream(url);
                // Subscribe to the ffmpeg process exit event, to play our next song
                _ffmpegProcess.Exited += (sender, args) =>
                {
                    EventHub.Raise(new EvOnFFmpegExit());
                    _ffmpegProcess.Dispose();
                    _ffmpegProcess = null;
                    Service.Get<IServiceAudioManager>().PlayNextSong(client);
                };
                var output = _ffmpegProcess.StandardOutput.BaseStream;
                var discord = client.CreatePCMStream(AudioApplication.Mixed);
                try
                {
                    byte[] buffer = new byte[3840];
                    int bytesRead;
                    while ((bytesRead = await output.ReadAsync(buffer, 0, buffer.Length)) > 0)
                    {
                        AdjustVolume(buffer, bytesRead, _volume);
                        await discord.WriteAsync(buffer, 0, bytesRead);
                    }
                    //await output.CopyToAsync(discord, cancellationToken); (This was replaced with the above code for dynamic volume control)
                    await discord.FlushAsync();
                }
                catch (Exception ex)
                {
                    Debug.Log($"Error during streaming: {ex.Message}.");
                }
                finally
                {
                    await discord.DisposeAsync();
                }
            }
            catch (Exception ex)
            {
                Debug.Log($"Error during ffmpeg creation: {ex.Message}.");
            }
            finally
            {
            }
        }

        //Adjust bytes to represent volume
        private void AdjustVolume(byte[] buffer, int bytesRead, float volume)
        {
            for (int i = 0; i < bytesRead; i += 2)
            {
                short sample = BitConverter.ToInt16(buffer, i);
                int adjustedSample = (int)(sample * volume);

                if (adjustedSample > short.MaxValue) adjustedSample = short.MaxValue;
                else if (adjustedSample < short.MinValue) adjustedSample = short.MinValue;

                byte[] bytes = BitConverter.GetBytes((short)adjustedSample);
                buffer[i] = bytes[0];
                buffer[i + 1] = bytes[1];
            }
        }
    }
}
