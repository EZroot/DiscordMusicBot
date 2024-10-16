using DiscordMusicBot.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordMusicBot.Services.Interfaces
{
    internal interface IServiceAnalytics : IService
    {
        Task Initialize();
        Task AddSongAnalytics(string userName, SongData songData);
    }
}
