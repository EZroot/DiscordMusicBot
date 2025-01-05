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
            Debug.Log($"<color=magenta>Volume set:</color> {_volume} -> {newVolume} ");
            _volume = newVolume;
            await Task.CompletedTask;
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
            if (_ffmpegProcess != null) 
                return; 

            try
            {
                _ffmpegProcess = ProcessHelper.CreateStream(url);
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
                    byte[] buffer = new byte[AUDIO_BYTE_SIZE];
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
                    Debug.Log($"<color=red>Error during streaming: {ex.Message}.");
                }
                finally
                {
                    await discord.DisposeAsync();
                }
            }
            catch (Exception ex)
            {
                Debug.Log($"<color=red>Error during ffmpeg creation: {ex.Message}.");
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
