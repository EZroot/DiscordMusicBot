using Discord;
using Discord.WebSocket;

namespace DiscordMusicBot.Commands.Interfaces
{
    internal interface IDiscordCommand
    {
        string CommandName { get; }
        SlashCommandBuilder Register();
        Task ExecuteAsync(SocketSlashCommand options);
    }
}
