using Discord;
using Discord.WebSocket;

namespace DiscordMusicBot2.Chat.Commands.Interface
{
    internal interface IBotCommand
    {
        string CommandName { get; }
        SlashCommandBuilder Register();
        Task ExecuteAsync(SocketSlashCommand options);
    }
}
