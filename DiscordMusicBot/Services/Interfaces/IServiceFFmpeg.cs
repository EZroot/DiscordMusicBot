using Discord.Audio;
using System.Diagnostics;

namespace DiscordMusicBot.Services.Interfaces
{
    internal interface IServiceFFmpeg : IService
    {
        bool IsSongPlaying { get; }
        Task StreamToDiscord(IAudioClient client, string url);
        bool ForceClose();
        Task SetVolume(float newVolume);

    }
}
