using Discord;
using Discord.WebSocket;
using DiscordMusicBot2;
using DiscordMusicBot2.Bot.Interface;
using DiscordMusicBot2.FileManagement;
using DiscordMusicBot2.Github;
using DiscordMusicBot2.Services;

public class Program
{

    static async Task Main()
    {
        // get path of the current executable
        var path = AppContext.BaseDirectory;
        var fileName = "config.ini";
        var fullPath = Path.Combine(path, fileName);
        var config = FileLoader.LoadConfig(fullPath);
        if (config == null)
        {
            Debug.Log("ERROR: Config failed to load. Exiting");
            await Task.Delay(2000);
            return;
        }
        Debug.Log($"<color=green>Config loaded:</color> {fullPath}");

        Downloader downloader = new Downloader();
        await downloader.DownloadAsync();

        await Service.Get<IServiceBot>().StartDiscordBot(config.BotToken, config.GuildId);

        // keep the process alive
        await Task.Delay(-1);
    }
}