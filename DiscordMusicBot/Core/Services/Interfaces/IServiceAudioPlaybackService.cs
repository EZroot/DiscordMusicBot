using Discord.Audio;
using Discord.WebSocket;
using DiscordMusicBot.Models;
namespace DiscordMusicBot.Services.Interfaces
{
    internal interface IServiceAudioPlaybackService : IService
    {
        int SongCount { get; }
        Task ClearSong();
        Task<SongData?> PlaySong(string user, string url);
        Task QueueSongToPlay(SongData song);
        Task PlayNextSong();
        Task GetCurrentSongQueue(SocketSlashCommand command);
        Task ShuffleQueue(SocketSlashCommand command);
        Task SkipSong(SocketSlashCommand command);
        Task SkipSongRaw();
        Task ChangeVolume(SocketSlashCommand command);
        Task JoinVoice(SocketSlashCommand command);
        Task LeaveVoiceRaw();
        Task LeaveVoice(SocketSlashCommand command);
    }
}
