using Discord.Audio;
using DiscordMusicBot.Events;
using DiscordMusicBot.Events.EventArgs;
using DiscordMusicBot.Services.Interfaces;
using System.Diagnostics;
using Debug = DiscordMusicBot.Utils.Debug;

namespace DiscordMusicBot.Services.Managers.Audio.ExternalProcesses
{
    internal class FFmpeg : IServiceFFmpeg
    {
        private const int AUDIO_BYTE_SIZE = 3840;
        private Process? _ffmpegProcess = null;
        private float _volume = 0.1f; // Default volume

        public bool IsSongPlaying => _ffmpegProcess != null;

        public async Task SetVolume(float newVolume)
        {
            Debug.Log($"<color=magenta>Volume set:</color> {_volume} -> {newVolume}");
            _volume = newVolume;
            await Task.CompletedTask;
        }

        public bool ForceClose()
        {
            if (_ffmpegProcess != null)
            {
                Debug.Log("<color=cyan>Force closing ffmpeg process.</color>");
                _ffmpegProcess.Kill();
                Debug.Log("<color=cyan>ffmpeg process killed successfully.</color>");
                return true;
            }
            Debug.Log("<color=yellow>No ffmpeg process to force close.</color>");
            return false;
        }

        public async Task StreamToDiscord(IAudioClient client, string url)
        {
            //Debug.Log($"<color=cyan>Entering FFmpeg StreamToDiscord with URL: {url}</color>");
            if (_ffmpegProcess != null)
            {
                Debug.Log("<color=yellow>ffmpeg process already running. Aborting new stream.</color>");
                return;
            }

            try
            {
                //Debug.Log($"<color=cyan>Creating FFmpeg process for URL: {url}</color>");
                _ffmpegProcess = ProcessHelper.CreateStream(url);
                Debug.Log("<color=cyan>FFmpeg process created successfully.</color>");

                _ffmpegProcess.Exited += (sender, args) =>
                {
                    Debug.Log("<color=cyan>FFmpeg process exited. Raising FFmpeg exit event.</color>");
                    EventHub.Raise(new EvOnFFmpegExit());
                    _ffmpegProcess.Dispose();
                    _ffmpegProcess = null;
                    Debug.Log("<color=cyan>Triggering playback of next song after FFmpeg exit.</color>");
                    Service.Get<IServiceAudioPlaybackService>().PlayNextSong();
                };

                var output = _ffmpegProcess.StandardOutput.BaseStream;
                Debug.Log("<color=cyan>Opened FFmpeg output stream.</color>");
                var discord = client.CreatePCMStream(AudioApplication.Mixed);
                Debug.Log("<color=cyan>Created Discord PCM stream.</color>");
                try
                {
                    byte[] buffer = new byte[AUDIO_BYTE_SIZE];
                    int bytesRead;
                    while ((bytesRead = await output.ReadAsync(buffer, 0, buffer.Length)) > 0)
                    {
                        //Debug.Log($"<color=cyan>Read {bytesRead} bytes from FFmpeg output.</color>");
                        AdjustVolume(buffer, bytesRead, _volume);
                        await discord.WriteAsync(buffer, 0, bytesRead);
                        //Debug.Log($"<color=cyan>Wrote {bytesRead} bytes to Discord stream.</color>");
                    }
                    Debug.Log("<color=cyan>Completed streaming; flushing Discord stream.</color>");
                    await discord.FlushAsync();
                }
                catch (Exception ex)
                {
                    Debug.Log($"<color=red>Error during streaming: {ex.Message}.</color>");
                }
                finally
                {
                    Debug.Log("<color=cyan>Disposing Discord PCM stream.</color>");
                    await discord.DisposeAsync();
                }
            }
            catch (Exception ex)
            {
                Debug.Log($"<color=red>Error during ffmpeg creation: {ex.Message}.</color>");
            }
        }

        // Adjust bytes to represent volume
        private void AdjustVolume(byte[] buffer, int bytesRead, float volume)
        {
            //Debug.Log($"<color=cyan>Adjusting volume for {bytesRead} bytes with volume {volume}.</color>");
            for (int i = 0; i < bytesRead; i += 2)
            {
                short sample = BitConverter.ToInt16(buffer, i);
                int adjustedSample = (int)(sample * volume);

                if (adjustedSample > short.MaxValue)
                    adjustedSample = short.MaxValue;
                else if (adjustedSample < short.MinValue)
                    adjustedSample = short.MinValue;

                byte[] bytes = BitConverter.GetBytes((short)adjustedSample);
                buffer[i] = bytes[0];
                buffer[i + 1] = bytes[1];
            }
            //Debug.Log("<color=cyan>Volume adjustment completed for current buffer.</color>");
        }
    }
}
