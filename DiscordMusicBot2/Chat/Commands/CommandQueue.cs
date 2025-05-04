using Discord;
using Discord.WebSocket;
using DiscordMusicBot2.Audio.Interface;
using DiscordMusicBot2.Chat.Commands.Interface;
using DiscordMusicBot2.Services;

namespace DiscordMusicBot2.Chat.Commands
{
    internal class CommandQueue : IBotCommand
    {
        public string CommandName => "queue";
        public SlashCommandBuilder Register()
        {
            return new SlashCommandBuilder()
                .WithName(CommandName)
                .WithDescription("Display a queue of songs");
        }

        public async Task ExecuteAsync(SocketSlashCommand options)
        {
            var songList = Service.Get<IServiceAudio>().SongDataList;
            var songTitles = songList.Select(song => song.Name).ToList();
            await options.RespondAsync("Songs in queue: " + string.Join(", ", songTitles), ephemeral: true);
        }
    }
}