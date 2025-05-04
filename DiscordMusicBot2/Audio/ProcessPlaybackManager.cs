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
        private const bool DEBUG_MODE = true;

        private const int AUDIO_BYTE_SIZE = 8192;    // ~40 ms @48 kHz stereo
        private float m_volume = 0.15f;

        private readonly object m_threadGate = new();
        private readonly IAudioClient m_audioClient;

        private Process? m_ffmpeg;
        private AudioOutStream? m_discord;
        private CancellationTokenSource? m_cts;

        public ProcessPlaybackManager(IAudioClient discordClient) =>
            m_audioClient = discordClient;

        public Task SetVolume(float newVolume)
        {
            Debug.Log($"<color=magenta>Volume change:</color> {m_volume} → {newVolume}");
            m_volume = newVolume > 100 ? 1.0f : newVolume * 0.01f;
            return Task.CompletedTask;
        }

        /// <summary>
        /// Download a youtube video through YT-DLP and then stream that temp file with FFMPEG
        /// </summary>
        /// <param name="youtubeUrl"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public async Task PlayAsync(string youtubeUrl)
        {
            Stop();
            var cts = new CancellationTokenSource();
            var ct = cts.Token;

            // 1) Download with both stdout & stderr redirected
            var tmpFile = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.m4a");
            var dlInfo = new ProcessStartInfo
            {
                FileName = "yt-dlp",
                Arguments = $"--no-playlist -f \"bestaudio[abr<=128]\" -o \"{tmpFile}\" \"{youtubeUrl}\"",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
            };
            using var downloadProc = Process.Start(dlInfo)
                                   ?? throw new InvalidOperationException("Could not start yt-dlp");

            if (DEBUG_MODE)
            {
                _ = PumpStdoutAsync(downloadProc, "yt-dlp");
                _ = PumpStderrAsync(downloadProc, "yt-dlp");
            }
            await downloadProc.WaitForExitAsync(ct).ConfigureAwait(false);

            if (ct.IsCancellationRequested || downloadProc.ExitCode != 0)
                return;

            // 2) Spawn FFmpeg with stdout+stderr
            m_ffmpeg = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "ffmpeg",
                    // bump to info if you want progress logs
                    Arguments = "-hide_banner -loglevel info -nostdin -i \""
                                          + tmpFile + "\" -ac 2 -ar 48000 -f s16le pipe:1",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                },
                EnableRaisingEvents = true
            };
            m_ffmpeg.Exited += (_, __) =>
            {
                if (!(cts.IsCancellationRequested) && m_ffmpeg.ExitCode == 0)
                    Console.WriteLine("[ffmpeg] Exited cleanly.");
            };
            m_ffmpeg.Start();
            //_ = PumpStdoutAsync(m_ffmpeg, "ffmpeg");
            //_ = PumpStderrAsync(m_ffmpeg, "ffmpeg");

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

        /// <summary>
        /// Live‐streams a YouTube URL by resolving its direct audio URL with yt-dlp,
        /// then feeding that URL into ffmpeg → Discord with a pre‐buffer.
        /// </summary>
        public async Task PlayLiveYoutubeAsync(string youtubeUrl)
        {
            Stop();
            m_cts = new CancellationTokenSource();
            var ct = m_cts.Token;

            // 1) Use yt-dlp to get the direct media URL (no download)
            string mediaUrl;
            {
                var getUrlInfo = new ProcessStartInfo
                {
                    FileName = "yt-dlp",
                    Arguments = $"--no-playlist -f \"bestaudio[abr<=128]\" -g \"{youtubeUrl}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                };
                using var proc = Process.Start(getUrlInfo)
                               ?? throw new InvalidOperationException("Could not start yt-dlp");
                mediaUrl = (await proc.StandardOutput.ReadLineAsync().ConfigureAwait(false))?.Trim()
                           ?? throw new InvalidOperationException("yt-dlp returned no URL");
                await proc.WaitForExitAsync(ct).ConfigureAwait(false);
                if (proc.ExitCode != 0)
                    throw new InvalidOperationException("yt-dlp failed to extract URL");
            }

            // 2) Spawn FFmpeg reading directly from that media URL
            m_ffmpeg = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "ffmpeg",
                    Arguments =
                        "-hide_banner -loglevel info -nostdin " +
                        "-reconnect 1 -reconnect_streamed 1 -reconnect_delay_max 5 " +
                        $"-i \"{mediaUrl}\" " +
                        "-ac 2 -ar 48000 -f s16le pipe:1",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                },
                EnableRaisingEvents = true
            };
            m_ffmpeg.Exited += (_, __) =>
            {
                if (!ct.IsCancellationRequested && m_ffmpeg.ExitCode == 0)
                    Console.WriteLine("[ffmpeg] exited cleanly");
            };
            m_ffmpeg.Start();

            if (DEBUG_MODE)
            {
                //_ = PumpStdoutAsync(m_ffmpeg, "ffmpeg");
                _ = PumpStderrAsync(m_ffmpeg, "ffmpeg");
            }

            // 3) Pre‐buffer ~5 seconds exactly the same as your other methods
            const int PREBUFFER_SECONDS = 5;
            int prebufferBytes = 48000/*Hz*/ * 2/*ch*/ * 2/*bytes*/ * PREBUFFER_SECONDS;
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

            // 4) Create the Discord stream & drain the pre‐buffer
            var discordStream = m_audioClient.CreatePCMStream(AudioApplication.Music);
            m_discord = discordStream;
            while (lookahead.Position < lookahead.Length)
            {
                int read = lookahead.Read(buffer, 0, buffer.Length);
                AdjustVolumeInline(buffer, read, m_volume);
                await discordStream.WriteAsync(buffer, 0, read, ct).ConfigureAwait(false);
            }

            // 5) Continue streaming until EOF or cancellation
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
                Debug.Log("<color=cyan>Live YouTube stream finished or stopped.</color>");
                EventHub.Raise(new OnSongFinishedEvent());
            }
        }

        /// <summary>
        /// Play a live stream from the given URL through FFMPEG.
        /// </summary>
        /// <param name="streamUrl"></param>
        /// <returns></returns>
        public async Task PlayLiveAsync(string streamUrl)
        {
            // 1) Cancel any existing playback
            Stop();
            m_cts = new CancellationTokenSource();
            var ct = m_cts.Token;

            // 2) Spawn FFmpeg reading directly from the stream URL
            m_ffmpeg = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "ffmpeg",
                    Arguments = "-hide_banner -loglevel info -nostdin " +
                                "-reconnect 1 -reconnect_streamed 1 -reconnect_delay_max 5 " +
                                $"-i \"{streamUrl}\" -ac 2 -ar 48000 -f s16le pipe:1",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                },
                EnableRaisingEvents = true
            };
            m_ffmpeg.Exited += (_, __) =>
            {
                if (!ct.IsCancellationRequested && m_ffmpeg.ExitCode == 0)
                    Console.WriteLine("[ffmpeg] Exited cleanly.");
            };
            m_ffmpeg.Start();

            if (DEBUG_MODE)
            {
                //_ = PumpStdoutAsync(m_ffmpeg, "ffmpeg");
                _ = PumpStderrAsync(m_ffmpeg, "ffmpeg");
            }

            // 3) Pre-buffer ~5 seconds to avoid drop-outs
            const int PREBUFFER_SECONDS = 5;
            int prebufferBytes = 48000 /*Hz*/ * 2 /*channels*/ * 2 /*bytes-per-sample*/ * PREBUFFER_SECONDS;
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

            // 4) Create the Discord PCM stream & drain the pre-buffer
            var discordStream = m_audioClient.CreatePCMStream(AudioApplication.Music);
            m_discord = discordStream;

            while (lookahead.Position < lookahead.Length)
            {
                int read = lookahead.Read(buffer, 0, buffer.Length);
                AdjustVolumeInline(buffer, read, m_volume);
                await discordStream.WriteAsync(buffer, 0, read, ct).ConfigureAwait(false);
            }

            // 5) Continue streaming until the source ends or we cancel
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
                Debug.Log("<color=cyan>Live stream finished or stopped.</color>");
                EventHub.Raise(new OnSongFinishedEvent());
            }
        }

        private static async Task PumpStdoutAsync(Process proc, string tag)
        {
            using var reader = proc.StandardOutput;
            while (await reader.ReadLineAsync().ConfigureAwait(false) is string line)
                Console.WriteLine($"[{tag} OUT] {line}");
        }

        private static async Task PumpStderrAsync(Process proc, string tag)
        {
            using var reader = proc.StandardError;
            while (await reader.ReadLineAsync().ConfigureAwait(false) is string line)
                Console.WriteLine($"[{tag} ERR] {line}");
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
