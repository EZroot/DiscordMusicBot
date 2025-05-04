using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading.Tasks;

namespace DiscordMusicBot2.Github
{
    public class Downloader
    {
        private enum PlatformType { Windows, Linux, Mac, Unsupported }

        private readonly HttpClient _client;

        public Downloader()
        {
            _client = new HttpClient();
            _client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("DiscordMusicBot2", "1.0"));
        }

        public async Task DownloadAsync()
        {
            var platform = GetPlatform();
            if (platform == PlatformType.Unsupported)
            {
                Debug.Log("Unsupported OS for auto‑download.");
                return;
            }

            // 1) Download yt-dlp
            await DownloadTool(
                repoApiUrl: "https://api.github.com/repos/yt-dlp/yt-dlp/releases/latest",
                fileKey: platform == PlatformType.Windows ? "yt-dlp.exe" : "yt-dlp",
                matchName: name =>
                {
                    if (platform == PlatformType.Windows) return name.EndsWith("yt-dlp.exe");
                    return name.Equals("yt-dlp");
                }
            );

            // 2) Download aria2
            await DownloadTool(
                repoApiUrl: "https://api.github.com/repos/aria2/aria2/releases/latest",
                fileKey: platform switch
                {
                    PlatformType.Windows => "aria2c.exe",
                    PlatformType.Linux => "aria2c",   // we'll just save the binary name
                    PlatformType.Mac => "aria2c",   // same as Linux
                    _ => throw new InvalidOperationException()
                },
                matchName: name =>
                {
                    return platform switch
                    {
                        PlatformType.Windows => name.Contains("win") && name.EndsWith(".zip"),
                        PlatformType.Linux => name.EndsWith(".tar.xz") || name.EndsWith(".tar.gz"),
                        PlatformType.Mac => name.EndsWith(".tar.xz") || name.EndsWith(".tar.gz"),
                        _ => false
                    };
                },
                postProcess: async (bytes, assetName) =>
                {
                    if (platform == PlatformType.Windows && assetName.EndsWith(".zip"))
                    {
                        // write zip, extract aria2c.exe, delete zip
                        var zipPath = Path.GetTempFileName() + ".zip";
                        await File.WriteAllBytesAsync(zipPath, bytes);
                        ZipFile.ExtractToDirectory(zipPath, ".", true);
                        File.Delete(zipPath);
                        Debug.Log("aria2 extracted and ready.");
                    }
                    else
                    {
                        // just write the raw binary or archive
                        var outName = platform == PlatformType.Windows
                            ? "aria2c.exe"
                            : "aria2c";
                        await File.WriteAllBytesAsync(outName, bytes);
                        if (platform != PlatformType.Windows)
                            UnixMakeExecutable(outName);
                        Debug.Log("aria2 downloaded.");
                    }
                }
            );

            Debug.Log("All tools are up-to-date.");
        }

        private async Task DownloadTool(string repoApiUrl, string fileKey, Func<string, bool> matchName, Func<byte[], string, Task>? postProcess = null)
        {
            // fetch release json
            var json = await _client.GetStringAsync(repoApiUrl);
            using var doc = JsonDocument.Parse(json);

            // find asset
            string? assetUrl = null, assetName = null;
            foreach (var asset in doc.RootElement.GetProperty("assets").EnumerateArray())
            {
                var name = asset.GetProperty("name").GetString()!;
                if (!matchName(name)) continue;
                assetUrl = asset.GetProperty("browser_download_url").GetString();
                assetName = name;
                break;
            }

            if (assetUrl == null)
            {
                Debug.Log($"No matching asset found at {repoApiUrl} for {fileKey}.");
                return;
            }

            // download bytes
            Debug.Log($"Downloading {fileKey} from {assetName} …");
            var bytes = await _client.GetByteArrayAsync(assetUrl);

            // skip if already up-to-date
            if (File.Exists(fileKey))
            {
                var existing = await File.ReadAllBytesAsync(fileKey);
                if (ComputeHash(existing) == ComputeHash(bytes))
                {
                    Debug.Log($"{fileKey} is already up-to-date.");
                    return;
                }
            }

            if (postProcess != null)
            {
                await postProcess(bytes, assetName!);
            }
            else
            {
                await File.WriteAllBytesAsync(fileKey, bytes);
                Debug.Log($"{fileKey} updated.");
            }
        }

        private static PlatformType GetPlatform()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return PlatformType.Windows;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) return PlatformType.Linux;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) return PlatformType.Mac;
            return PlatformType.Unsupported;
        }

        private static string ComputeHash(byte[] data)
        {
            using var sha256 = SHA256.Create();
            var hash = sha256.ComputeHash(data);
            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }

        private static void UnixMakeExecutable(string path)
        {
            try
            {
                // chmod +x on Unix
                var chmod = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "chmod",
                        Arguments = $"+x \"{path}\"",
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };
                chmod.Start();
                chmod.WaitForExit();
            }
            catch { /* best effort */ }
        }
    }
}
