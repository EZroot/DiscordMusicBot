using DiscordMusicBot.Core;
using DiscordMusicBot.Core.Github;
using DiscordMusicBot.Utils;

public class Program
{
    public static async Task Main()
    {
        var downloader = new Downloader();
        await downloader.DownloadAsync();
        Debug.Log("Starting discord music bot...");
        await Task.Delay(500);
        var bot = new DiscordBot();
        await bot.Initialize();
    }
}