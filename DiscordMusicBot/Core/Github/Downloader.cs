using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;
using DiscordMusicBot.Utils;

namespace DiscordMusicBot.Core.Github;

public class Downloader
{
    private enum PlatformType
    {
        Windows,
        Linux,
        Mac,
        Unsupported
    }

    public async Task DownloadAsync()
    {

        using var client = new HttpClient();
        client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("MyApp", "1.0"));

        // Fetch latest release info
        var releaseUrl = "https://api.github.com/repos/yt-dlp/yt-dlp/releases/latest";
        var releaseJson = await client.GetStringAsync(releaseUrl);
        using var doc = JsonDocument.Parse(releaseJson);

        // Find the asset with "yt-dlp.exe" in its name
        string assetUrl = null;
        PlatformType platform = PlatformType.Windows;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            platform = PlatformType.Windows;
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            platform = PlatformType.Linux;
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            platform = PlatformType.Mac;
        else
            platform = PlatformType.Unsupported;

        foreach (var asset in doc.RootElement.GetProperty("assets").EnumerateArray())
        {
            if (platform == PlatformType.Windows)
            {
                if (asset.GetProperty("name").GetString().Contains("yt-dlp.exe"))
                {
                    assetUrl = asset.GetProperty("browser_download_url").GetString();
                    break;
                }
            }
            else if (platform == PlatformType.Linux)
            {
                if (asset.GetProperty("name").GetString().Contains("yt-dlp"))
                {
                    assetUrl = asset.GetProperty("browser_download_url").GetString();
                    break;
                }
            }
            else
            {
                Debug.Log("Platform not supported");
            }
        }

        if (assetUrl == null)
        {
            Debug.Log("Asset not found.");
            return;
        }
        
        Debug.Log("Checking for new version...");
        var fileBytes = await client.GetByteArrayAsync(assetUrl);

        string fileName = platform == PlatformType.Windows ? "yt-dlp.exe" : "yt-dlp";

        if (File.Exists(fileName))
        {
            var currentBytes = await File.ReadAllBytesAsync(fileName);
            if (ComputeHash(currentBytes) == ComputeHash(fileBytes))
            {
                Debug.Log("yt-dlp is up-to-date.");
                return;
            }
        }
        
        if (platform == PlatformType.Linux)
        {
            await File.WriteAllBytesAsync("yt-dlp", fileBytes);
            Debug.Log("yt-dlp updated! >:)");
        }
        else if (platform == PlatformType.Windows)
        {
            await File.WriteAllBytesAsync("yt-dlp.exe", fileBytes);
            Debug.Log("yt-dlp updated! >:)");
        }

        Debug.Log("Download complete.");
    }
    
    string ComputeHash(byte[] data)
    {
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(data);
        return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
    }
}