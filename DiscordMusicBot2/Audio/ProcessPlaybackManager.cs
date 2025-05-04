using Discord.Audio;
using DiscordMusicBot2.Events;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace DiscordMusicBot2.Audio
{
    /// <summary>
    /// Maintains exactly one <c>yt‑dlp → ffmpeg → Discord</c> pipeline.
    /// Raises <see cref="OnSongFinishedEvent"/> when the track ends on its own
    /// (i.e. not skipped or pre‑empted by another /play).
    /// </summary>
    internal class ProcessPlaybackManager
    {
        private const int AUDIO_BYTE_SIZE = 8192;    // ~40 ms @48 kHz stereo
        private float m_volume = 0.1f;

        private readonly object m_threadGate = new();
        private readonly IAudioClient m_audioClient;

        private Process? m_ffmpeg;
        private AudioOutStream? m_discord;
        private CancellationTokenSource? m_cts;

        public ProcessPlaybackManager(IAudioClient discordClient) =>
            m_audioClient = discordClient;

        public async Task SetVolume(float newVolume)
        {
            Debug.Log($"<color=magenta>Volume change:</color> {m_volume} → {newVolume}");
            m_volume = newVolume > 100 ? 1.0f : newVolume * 0.01f;
        }

        public async Task PlayAsync(string youtubeUrl)
        {
            // 0) Tear down anything left over
            Stop();
            m_cts = new CancellationTokenSource();
            var ct = m_cts.Token;

            // small pause to ensure pipes are fully closed
            await Task.Delay(50, ct).ConfigureAwait(false);

            // 1) Download full track
            var tmpFile = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.m4a");
            try
            {
                var dlInfo = new ProcessStartInfo
                {
                    FileName = "yt-dlp",
                    Arguments = $"-f \"bestaudio[abr<=128]\" -o \"{tmpFile}\" \"{youtubeUrl}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardError = true
                };
                using var downloadProc = new Process { StartInfo = dlInfo };
                downloadProc.Start();
                _ = PumpStderrAsync(downloadProc, "yt-dlp");
                await downloadProc.WaitForExitAsync(ct).ConfigureAwait(false);

                if (ct.IsCancellationRequested || downloadProc.ExitCode != 0)
                    return;
            }
            catch (OperationCanceledException)
            {
                return;
            }

            // 2) Spawn FFmpeg (with -nostdin) to decode the downloaded file
            m_ffmpeg = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "ffmpeg",
                    Arguments = $"-hide_banner -loglevel error -nostdin -i \"{tmpFile}\" -ac 2 -ar 48000 -f s16le pipe:1",
                    UseShellExecute = false,
                    CreateNoWindow = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                },
                EnableRaisingEvents = true
            };
            m_ffmpeg.Exited += (_, __) =>
            {
                if (!(m_cts?.IsCancellationRequested ?? true) && m_ffmpeg.ExitCode == 0)
                {
                    //Debug.Log("<color=cyan>Track finished.</color>");
                    //EventHub.Raise(new OnSongFinishedEvent());
                }
            };
            m_ffmpeg.Start();
            _ = PumpStderrAsync(m_ffmpeg, "ffmpeg");

            // 3) Pre‑buffer to avoid underflow
            const int PREBUFFER_SECONDS = 5;
            int prebufferBytes = 48000 * 2 * 2 * PREBUFFER_SECONDS;
            using var lookahead = new MemoryStream();
            var buffer = new byte[AUDIO_BYTE_SIZE];
            int buffered = 0;

            while (buffered < prebufferBytes)
            {
                int read = await m_ffmpeg.StandardOutput.BaseStream
                                .ReadAsync(buffer, 0, buffer.Length, ct)
                                .ConfigureAwait(false);
                if (read <= 0) break;
                lookahead.Write(buffer, 0, read);
                buffered += read;
            }
            lookahead.Position = 0;

            // 4) Create Discord stream & drain the pre‑buffer
            var discordStream = m_audioClient.CreatePCMStream(AudioApplication.Music);
            m_discord = discordStream;

            while (lookahead.Position < lookahead.Length)
            {
                int read = lookahead.Read(buffer, 0, buffer.Length);
                AdjustVolumeInline(buffer, read, m_volume);
                await discordStream.WriteAsync(buffer, 0, read, ct).ConfigureAwait(false);
            }

            // 5) Continue streaming until EOF, then clean up
            try
            {
                int read;
                while ((read = await m_ffmpeg.StandardOutput.BaseStream
                                   .ReadAsync(buffer, 0, buffer.Length, ct)
                                   .ConfigureAwait(false)) > 0)
                {
                    AdjustVolumeInline(buffer, read, m_volume);
                    await discordStream.WriteAsync(buffer, 0, read, ct).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException) { }
            finally
            {
                await discordStream.FlushAsync().ConfigureAwait(false);
                Stop();
                TryDelete(tmpFile);
                Debug.Log($"<color=green>Deleted temp file:</color> {tmpFile}");
                Debug.Log("<color=cyan>Track finished.</color>");
                EventHub.Raise(new OnSongFinishedEvent());
            }
        }

        private static async Task PumpStderrAsync(Process proc, string tag)
        {
            using var reader = proc.StandardError;
            while (await reader.ReadLineAsync().ConfigureAwait(false) is string line)
                Debug.Log($"<color=red>[{tag}]</color> {line}");
        }

        // Inline byte math to avoid extra allocations
        private void AdjustVolumeInline(byte[] buffer, int count, float volume)
        {
            for (int i = 0; i < count; i += 2)
            {
                short sample = (short)((buffer[i + 1] << 8) | buffer[i]);
                int adj = (int)(sample * volume);
                adj = Math.Clamp(adj, short.MinValue, short.MaxValue);
                short outSamp = (short)adj;
                buffer[i] = (byte)(outSamp & 0xFF);
                buffer[i + 1] = (byte)((outSamp >> 8) & 0xFF);
            }
        }

        public void Stop()
        {
            lock (m_threadGate)
            {
                m_cts?.Cancel();

                if (m_ffmpeg != null && !m_ffmpeg.HasExited)
                {
                    Debug.Log("<color=yellow>Stopping ffmpeg process...</color>");
                }
                TryKillDispose(ref m_ffmpeg);
                Debug.Log("<color=yellow>ffmpeg disposed.</color>");

                if (m_discord != null)
                {
                    Debug.Log("<color=yellow>Disposing Discord stream...</color>");
                    m_discord.Dispose();
                }

                m_cts?.Dispose();
                m_cts = null;
                m_discord = null;
            }
        }

        private static void TryKillDispose(ref Process? proc)
        {
            try
            {
                if (proc != null && !proc.HasExited)
                    proc.Kill(true);
            }
            catch { }
            finally
            {
                proc?.Dispose();
                proc = null;
            }
        }

        private static void TryDelete(string path)
        {
            try
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
            catch { }
        }
    }
}
