using DiscordMusicBot.Services;
using DiscordMusicBot.Services.Interfaces;

public class Program
{
    public static async Task Main()
    {
        await Service.Get<IServiceBotManager>().Initialize();
    }
}