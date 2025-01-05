using DiscordMusicBot.Core;

public class Program
{
    public static async Task Main()
    {
        // await Service.Get<IServiceBotManager>().Initialize();
        var bot = new DiscordBot();
        await bot.Initialize();
    }
}