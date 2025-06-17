using Discord.Audio;
using System.Diagnostics;

namespace DiscordMusicBot2.Helpers
{
    public static class ProcessPlaybackHelper
    {
        private static float m_volume = 0.1f;

        public static Process StartMasterFfmpeg()
        {
            var psi = new ProcessStartInfo
            {
                FileName = "ffmpeg",
                Arguments =
                    "-hide_banner -loglevel error -re " +
                    "-f mpegts -i pipe:0 " +
                    "-af aresample=async=1 " +
                    "-ac 2 -ar 48000 -f s16le pipe:1",
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            return Process.Start(psi)
                   ?? throw new InvalidOperationException("Could not start master ffmpeg");
        }

        public static async Task FeedTrackIntoMaster(Process master, string filePath)
        {
            Debug.Log($"Feeding file path: {filePath}");
            //var path = await DownloadYoutubeVideo(filePath, CancellationToken.None, true);
            //Debug.Log($"Got streamer path {path} - Trying to play");
            var psi = new ProcessStartInfo
            {
                FileName = "ffmpeg",
                Arguments =
                    $"-hide_banner -loglevel error -re -i \"{filePath}\" " +
                    "-c:a copy -bsf:a aac_adtstoasc -f mpegts pipe:1",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var conv = Process.Start(psi)
                           ?? throw new InvalidOperationException("Could not start converter ffmpeg");
            await conv.StandardOutput.BaseStream
                      .CopyToAsync(master.StandardInput.BaseStream)
                      .ConfigureAwait(false);
            await conv.WaitForExitAsync().ConfigureAwait(false);
        }


        public static async Task StreamUntilEOF(Process ffmpeg, AudioOutStream aos, byte[] buffer, CancellationToken ct)
        {
            int read;
            while ((read = await ffmpeg.StandardOutput.BaseStream
                               .ReadAsync(buffer, 0, buffer.Length, ct)
                               .ConfigureAwait(false)) > 0)
            {
                AdjustVolumeInline(buffer, read, m_volume);
                await aos.WriteAsync(buffer, 0, read, ct).ConfigureAwait(false);
            }
        }

        // Inline byte math to avoid extra allocations
        private static void AdjustVolumeInline(byte[] buffer, int count, float volume)
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

        public static async Task<MemoryStream> CreatePrebufferAudioStream(Process ffmpeg, byte[] buffer, CancellationToken ct)
        {
            const int PREBUFFER_SECONDS = 5;
            int prebufferBytes = 48000 * 2 * 2 * PREBUFFER_SECONDS;
            var lookahead = new MemoryStream();
            int buffered = 0;

            while (buffered < prebufferBytes)
            {
                int read = await ffmpeg.StandardOutput.BaseStream
                                .ReadAsync(buffer, 0, buffer.Length, ct)
                                .ConfigureAwait(false);
                if (read <= 0) break;
                lookahead.Write(buffer, 0, read);
                buffered += read;
            }
            lookahead.Position = 0;
            return lookahead;
        }

        public static async Task DrainPrebufferAudioStream(MemoryStream ms, AudioOutStream aos, byte[] buffer, CancellationToken ct)
        {
            while (ms.Position < ms.Length)
            {
                int read = ms.Read(buffer, 0, buffer.Length);
                AdjustVolumeInline(buffer, read, m_volume);
                await aos.WriteAsync(buffer, 0, read, ct).ConfigureAwait(false);
            }
            ms.Dispose();
        }

        public static Process? CreateFfmpegStream(string tmpFile, CancellationTokenSource cts)
        {
            // 2) Spawn FFmpeg with stdout+stderr
            var ffmpeg = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "ffmpeg",
                    // bump to info if you want progress logs
                    Arguments = "-hide_banner -loglevel info -nostdin -re -i \""
                                          + tmpFile + "\" -ac 2 -ar 48000 -af aresample=async=1 -f s16le pipe:1",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                },
                EnableRaisingEvents = true
            };
            ffmpeg.Exited += (_, __) =>
            {
                if (!(cts.IsCancellationRequested) && ffmpeg.ExitCode == 0)
                    Console.WriteLine("[ffmpeg] Exited cleanly.");
            };
            ffmpeg.Start();
            //_ = PumpStdoutAsync(m_ffmpeg, "ffmpeg");
            _ = PumpStderrAsync(ffmpeg, "ffmpeg");
            return ffmpeg;
        }

        /// <summary>
        /// Downloads a Youtube video using yt-dlp and returns the path to the downloaded file.
        /// </summary>
        /// <param name="youtubeUrl"></param>
        /// <returns>Tmp file path</returns>
        /// <exception cref="InvalidOperationException"></exception>
        public static async Task<string?> DownloadYoutubeVideo(string youtubeUrl, CancellationToken ct,bool debugMode = false)
        {
            var tmpFile = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.m4a");
            var dlInfo = new ProcessStartInfo
            {
                FileName = "yt-dlp",
                Arguments = $"--no-playlist -f \"bestaudio\" -o \"{tmpFile}\" \"{youtubeUrl}\"",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
            };
            using var downloadProc = Process.Start(dlInfo)
                                   ?? throw new InvalidOperationException("Could not start yt-dlp");

            if (debugMode)
            {
                _ = PumpStdoutAsync(downloadProc, "yt-dlp");
                _ = PumpStderrAsync(downloadProc, "yt-dlp");
            }
            await downloadProc.WaitForExitAsync(ct).ConfigureAwait(false);

            if (ct.IsCancellationRequested || downloadProc.ExitCode != 0)
                return null;

            return tmpFile;
        }

        public static async Task PumpStdoutAsync(Process proc, string tag)
        {
            using var reader = proc.StandardOutput;
            while (await reader.ReadLineAsync().ConfigureAwait(false) is string line)
                Console.WriteLine($"[{tag} OUT] {line}");
        }

        public static async Task PumpStderrAsync(Process proc, string tag)
        {
            using var reader = proc.StandardError;
            while (await reader.ReadLineAsync().ConfigureAwait(false) is string line)
                Console.WriteLine($"[{tag} ERR] {line}");
        }
    }
}
