using Discord.Audio;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordMusicBot.Services.Interfaces
{
    internal interface IServiceFFmpeg : IService
    {
        bool IsSongPlaying { get; }
        Process CreateStream(string url);
        Task StreamToDiscord(IAudioClient client, string url);
        bool ForceClose();
        Task SetVolume(float newVolume);

    }
}
