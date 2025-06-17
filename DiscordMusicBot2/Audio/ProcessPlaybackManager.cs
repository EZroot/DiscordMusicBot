using Discord.Audio;
using DiscordMusicBot2.Events;
using System.Diagnostics;

namespace DiscordMusicBot2.Audio
{
    /// <summary>
    /// Maintains exactly one <c>yt-dlp → ffmpeg → Discord</c> pipeline.
    /// Raises <see cref="OnSongFinishedEvent"/> when the track ends on its own.
    /// </summary>
    internal class ProcessPlaybackManager
    {
        private const bool DEBUG_MODE = true;
        private const int AUDIO_BYTE_SIZE = 3840;   // exactly 20 ms @48 kHz stereo
        private const int PREBUFFER_SECONDS = 10;

        private float _volume = 0.1f;
        private readonly object _threadGate = new();
        private readonly IAudioClient _audioClient;
        private readonly AudioOutStream _discordStream;

        private Process? _ffmpeg;
        private CancellationTokenSource? _cts;

        public ProcessPlaybackManager(IAudioClient discordClient)
        {
            _audioClient = discordClient;
            // create the Discord stream once and reuse it
            _discordStream = _audioClient.CreatePCMStream(AudioApplication.Music);
        }

        public Task SetVolume(float newVolume)
        {
            Debug.Log($"<color=magenta>Volume change:</color> {_volume} → {newVolume}");
            _volume = newVolume > 100 ? 1.0f : newVolume * 0.01f;
            return Task.CompletedTask;
        }

        public async Task PlayAsync(string youtubeUrl)
        {
            // stop any existing playback (but do NOT dispose the Discord stream)
            await StopAsync().ConfigureAwait(false);

            using var cts = new CancellationTokenSource();
            var ct = cts.Token;

            // 1) Download to temp file
            var tmpFile = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.m4a");
            await RunProcessAsync(new ProcessStartInfo
            {
                FileName = "yt-dlp",
                Arguments = $"--no-playlist -f \"bestaudio\" -o \"{tmpFile}\" \"{youtubeUrl}\"",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
            }, "yt-dlp", ct).ConfigureAwait(false);

            // 2) Spawn ffmpeg (no -re on the file!)
            _ffmpeg = StartFfmpegProcess(
                "-hide_banner -loglevel info -nostdin " +
                // no "-re " here  
                "-thread_queue_size 512 " +
                $"-i \"{tmpFile}\" " +
                "-vn " +                                // drop video
                "-ac 2 -ar 48000 -sample_fmt s16 " +
                "-fflags +genpts " +
                "-af \"aresample=async=1000:resampler=soxr\" " +
                "-f s16le pipe:1"
            );

            // 3) Prebuffer + stream
            await PrebufferAndStreamAsync(
                _ffmpeg.StandardOutput.BaseStream,
                _discordStream,
                ct
            ).ConfigureAwait(false);

            // 4) Flush and delete
            await _discordStream.FlushAsync().ConfigureAwait(false);
            TryDelete(tmpFile);

            Debug.Log($"<color=green>Deleted temp file:</color> {tmpFile}");
            Debug.Log("<color=cyan>Track finished.</color>");

            // 5) Fire next-song event
            await EventHub.RaiseAsync(new OnSongFinishedEvent()).ConfigureAwait(false);
        }

        /// <summary>
        /// Stops the FFmpeg process (after an optional short delay),
        /// but leaves the Discord stream open for the next track.
        /// </summary>
        public async Task StopAsync(int delayMs = 50)
        {
            // small delay to let Discord socket clear its last packets
            await Task.Delay(delayMs).ConfigureAwait(false);

            lock (_threadGate)
            {
                _cts?.Cancel();
                TryKillDispose(ref _ffmpeg);
                _cts?.Dispose();
                _cts = null;
            }
        }

        #region Helpers

        private Process StartFfmpegProcess(string arguments)
        {
            var proc = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "ffmpeg",
                    Arguments = arguments,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                },
                EnableRaisingEvents = true
            };
            proc.Exited += (_, __) =>
            {
                if ((_cts?.IsCancellationRequested ?? true) == false && proc.ExitCode == 0)
                    Console.WriteLine("[ffmpeg] exited cleanly.");
            };
            proc.Start();
            if (DEBUG_MODE)
                _ = PumpStderrAsync(proc, "ffmpeg");
            return proc;
        }

        private async Task RunProcessAsync(
            ProcessStartInfo psi,
            string tag,
            CancellationToken ct,
            Func<StreamReader, Task>? onStdout = null)
        {
            using var proc = Process.Start(psi)
                        ?? throw new InvalidOperationException($"Could not start {psi.FileName}");
            if (DEBUG_MODE)
            {
                _ = PumpStdoutAsync(proc, tag);
                _ = PumpStderrAsync(proc, tag);
            }
            if (onStdout != null)
                await onStdout(proc.StandardOutput).ConfigureAwait(false);
            await proc.WaitForExitAsync(ct).ConfigureAwait(false);
            if (ct.IsCancellationRequested || proc.ExitCode != 0)
                throw new OperationCanceledException($"{tag} failed or cancelled");
        }

        private async Task PrebufferAndStreamAsync(
            Stream ffmpegOut,
            AudioOutStream discordStream,
            CancellationToken ct,
            bool doPrebuffer = true)
        {
            byte[] buffer = new byte[AUDIO_BYTE_SIZE];
            MemoryStream? lookahead = null;

            if (doPrebuffer)
            {
                int prebufferBytes = 48_000 * 2 * 2 * PREBUFFER_SECONDS;
                lookahead = new MemoryStream();
                int buffered = 0;
                while (buffered < prebufferBytes)
                {
                    int read = await ffmpegOut.ReadAsync(buffer, 0, buffer.Length, ct).ConfigureAwait(false);
                    if (read <= 0) break;
                    await lookahead.WriteAsync(buffer, 0, read, ct).ConfigureAwait(false);
                    buffered += read;
                }
                lookahead.Position = 0;
            }

            if (lookahead != null)
            {
                while (true)
                {
                    int read = await lookahead.ReadAsync(buffer, 0, buffer.Length, ct).ConfigureAwait(false);
                    if (read <= 0) break;
                    AdjustVolumeInline(buffer, read, _volume);
                    await discordStream.WriteAsync(buffer, 0, read, ct).ConfigureAwait(false);
                }
                lookahead.Dispose();
            }

            try
            {
                while (true)
                {
                    int read = await ffmpegOut.ReadAsync(buffer, 0, buffer.Length, ct).ConfigureAwait(false);
                    if (read <= 0) break;
                    AdjustVolumeInline(buffer, read, _volume);
                    await discordStream.WriteAsync(buffer, 0, read, ct).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException) { }
        }

        private static async Task PumpStdoutAsync(Process proc, string tag)
        {
            using var rdr = proc.StandardOutput;
            while (await rdr.ReadLineAsync().ConfigureAwait(false) is string line)
                Console.WriteLine($"[{tag} OUT] {line}");
        }

        private static async Task PumpStderrAsync(Process proc, string tag)
        {
            using var rdr = proc.StandardError;
            while (await rdr.ReadLineAsync().ConfigureAwait(false) is string line)
                Console.WriteLine($"[{tag} ERR] {line}");
        }

        private void AdjustVolumeInline(byte[] buffer, int count, float volume)
        {
            for (int i = 0; i < count; i += 2)
            {
                short sample = (short)((buffer[i + 1] << 8) | buffer[i]);
                int adj = Math.Clamp((int)(sample * volume), short.MinValue, short.MaxValue);
                short outSamp = (short)adj;
                buffer[i] = (byte)(outSamp & 0xFF);
                buffer[i + 1] = (byte)((outSamp >> 8) & (0xFF));
            }
        }

        private void TryKillDispose(ref Process? proc)
        {
            try { proc?.Kill(true); }
            catch { }
            finally { proc?.Dispose(); proc = null; }
        }

        private void TryDelete(string path)
        {
            try { File.Delete(path); }
            catch { }
        }

        #endregion
    }
}
