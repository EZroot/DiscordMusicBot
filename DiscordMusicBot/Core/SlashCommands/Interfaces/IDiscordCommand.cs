using Discord;
using Discord.WebSocket;

namespace DiscordMusicBot.SlashCommands.Interfaces
{
    internal interface IDiscordCommand
    {
        string CommandName { get; }
        SlashCommandBuilder Register();
        Task ExecuteAsync(SocketSlashCommand options);
    }
}
