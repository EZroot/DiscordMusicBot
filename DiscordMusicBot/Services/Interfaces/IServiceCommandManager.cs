using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordMusicBot.Services.Interfaces
{
    internal interface IServiceCommandManager : IService
    {
        Task ExecuteCommand(SocketSlashCommand slashCommand);
        Task RegisterAllCommands(SocketGuild guild);
    }
}
