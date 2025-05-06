using Discord.Audio;
using Discord;
using DiscordMusicBot2.Services.Interface;
using DiscordMusicBot2.Data;
using Discord.WebSocket;
namespace DiscordMusicBot2.Audio.Interface
{
    internal interface IServiceAudio : IService
    {
        List<SongData> SongDataList { get; }
        Task JoinVoiceChannel(IVoiceChannel voiceChannel);
        Task LeaveCurrentVoiceChannel(IVoiceChannel voiceChannel);
        Task Play(string url);
        Task Skip();
        Task<bool> ChangeVolume(float vol);
    }
}
