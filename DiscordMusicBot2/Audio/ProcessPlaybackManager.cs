using Discord.Audio;
using DiscordMusicBot2.Events;
using System.Diagnostics;

namespace DiscordMusicBot2.Audio
{
    /// <summary>
    /// Maintains exactly one <c>yt-dlp → ffmpeg → Discord</c> pipeline.
    /// Raises <see cref="OnSongFinishedEvent"/> when the track ends on its own
    /// (i.e. not skipped or pre-empted by another /play).
    /// </summary>
    internal class ProcessPlaybackManager
    {
        private const bool DEBUG_MODE = true;
        private const int AUDIO_BYTE_SIZE = 3840;    // ~40 ms @48 kHz stereo
        private const int PREBUFFER_SECONDS = 5;

        private float _volume = 0.25f;
        private readonly object _threadGate = new();
        private readonly IAudioClient _audioClient;

        private Process? _ffmpeg;
        private AudioOutStream? _discordStream;
        private CancellationTokenSource? _cts;

        public ProcessPlaybackManager(IAudioClient discordClient) =>
            _audioClient = discordClient;

        public Task SetVolume(float newVolume)
        {
            Debug.Log($"<color=magenta>Volume change:</color> {_volume} → {newVolume}");
            _volume = newVolume > 100 ? 1.0f : newVolume * 0.01f;
            return Task.CompletedTask;
        }

        public async Task PlayAsync(string youtubeUrl)
        {
            Stop();
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
            }, "yt-dlp", ct);

            // 2) Start ffmpeg reading from that file
            _ffmpeg = StartFfmpegProcess(
                "-hide_banner -loglevel info -nostdin " +
                "-re " +
                "-thread_queue_size 512 " +
                $"-i \"{tmpFile}\" " +
                "-vn " +
                "-ac 2 -ar 48000 -sample_fmt s16 " +
                "-fflags +genpts " +
                "-af \"aresample=async=1000:resampler=soxr\" " +
                "-f s16le pipe:1");

            // 3) Open Discord stream
            _discordStream = _audioClient.CreatePCMStream(AudioApplication.Music);

            // 4) Prebuffer + pump into Discord
            await PrebufferAndStreamAsync(_ffmpeg.StandardOutput.BaseStream, _discordStream, ct, false);

            // 5) Clean up
            await _discordStream.FlushAsync().ConfigureAwait(false);
            Stop();
            TryDelete(tmpFile);
            Debug.Log($"<color=green>Deleted temp file:</color> {tmpFile}");
            Debug.Log("<color=cyan>Track finished.</color>");
            EventHub.Raise(new OnSongFinishedEvent());
        }

        public async Task PlayLiveYoutubeAsync(string youtubeUrl, bool usePrebuffer = true)
        {
            Stop();
            _cts = new CancellationTokenSource();
            var ct = _cts.Token;

            // 1) Get direct media URL
            string mediaUrl = null!;
            await RunProcessAsync(new ProcessStartInfo
            {
                FileName = "yt-dlp",
                Arguments = $"--no-playlist -g \"{youtubeUrl}\"",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            }, "yt-dlp", ct, async output =>
            {
                mediaUrl = (await output.ReadLineAsync().ConfigureAwait(false))?.Trim()
                           ?? throw new InvalidOperationException("yt-dlp returned no URL");
            });

            // 2) Start ffmpeg on media URL
            _ffmpeg = StartFfmpegProcess(
                "-hide_banner -loglevel info -nostdin " +
                "-reconnect 1 -reconnect_streamed 1 -reconnect_delay_max 5 " +
                "-re -i \"" + mediaUrl + "\" -ac 2 -ar 48000 -f s16le pipe:1");

            // 3) Create Discord stream
            _discordStream = _audioClient.CreatePCMStream(AudioApplication.Music);

            // 4) Optional prebuffer + stream
            await PrebufferAndStreamAsync(_ffmpeg.StandardOutput.BaseStream, _discordStream, ct, usePrebuffer);

            // 5) Done
            await _discordStream.FlushAsync().ConfigureAwait(false);
            Stop();
            Debug.Log("<color=cyan>Track finished (Live youtube).</color>");
            EventHub.Raise(new OnSongFinishedEvent());
        }

        public async Task PlayLiveAsync(string streamUrl)
        {
            Stop();
            _cts = new CancellationTokenSource();
            var ct = _cts.Token;

            // Start ffmpeg directly on the stream URL
            _ffmpeg = StartFfmpegProcess(
                "-hide_banner -loglevel info -nostdin " +
                "-reconnect 1 -reconnect_streamed 1 -reconnect_delay_max 5 " +
                "-re -i \"" + streamUrl + "\" -ac 2 -ar 48000 -f s16le pipe:1");

            _discordStream = _audioClient.CreatePCMStream(AudioApplication.Music);

            await PrebufferAndStreamAsync(_ffmpeg.StandardOutput.BaseStream, _discordStream, ct);

            await _discordStream.FlushAsync().ConfigureAwait(false);
            Stop();
            Debug.Log("<color=cyan>Track finished. (Live Anything)</color>");
            EventHub.Raise(new OnSongFinishedEvent());
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

        /// <summary>
        /// Pulls <paramref name="prebuffer"/> bytes first, then streams continuously to Discord.
        /// </summary>
        private async Task PrebufferAndStreamAsync(
            Stream ffmpegOut,
            AudioOutStream discordStream,
            CancellationToken ct,
            bool doPrebuffer = true)
        {
            byte[] buffer = new byte[AUDIO_BYTE_SIZE];
            Stream? lookahead = null;

            if (doPrebuffer)
            {
                int prebufferBytes = 48_000 /*Hz*/ * 2 /*ch*/ * 2 /*bytes*/ * PREBUFFER_SECONDS;
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

            // Drain lookahead
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

            // Continuous stream
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

        // Inline byte math to avoid extra allocations
        private void AdjustVolumeInline(byte[] buffer, int count, float volume)
        {
            for (int i = 0; i < count; i += 2)
            {
                short sample = (short)((buffer[i + 1] << 8) | buffer[i]);
                int adj = Math.Clamp((int)(sample * volume), short.MinValue, short.MaxValue);
                short outSamp = (short)adj;
                buffer[i] = (byte)(outSamp & 0xFF);
                buffer[i + 1] = (byte)((outSamp >> 8) & 0xFF);
            }
        }

        public void Stop()
        {
            lock (_threadGate)
            {
                _cts?.Cancel();

                if (_ffmpeg is { HasExited: false })
                    Debug.Log("<color=yellow>Stopping ffmpeg process...</color>");

                TryKillDispose(ref _ffmpeg);

                if (_discordStream != null)
                {
                    Debug.Log("<color=yellow>Disposing Discord stream...</color>");
                    _discordStream.Dispose();
                }

                _cts?.Dispose();
                _cts = null;
                _discordStream = null;
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
