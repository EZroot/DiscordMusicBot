using Discord.Audio;
using Discord.WebSocket;
using Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordMusicBot.Services.Interfaces
{
    internal interface IServiceAudioManager : IService
    {
        int SongCount { get; }
        Task PlaySong(SocketSlashCommand command);
        Task PlaySong(string title, string url);
        Task PlayNextSong(IAudioClient client);
        Task SongQueue(SocketSlashCommand command);
        Task SkipSong(SocketSlashCommand command);
        Task ChangeVolume(SocketSlashCommand command);
        Task CheckAndJoinVoice(SocketSlashCommand command);
    }
}
