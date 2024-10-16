using Discord.Audio;
using Discord.WebSocket;
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
        Task LeaveVoice(SocketSlashCommand command);
    }
}
