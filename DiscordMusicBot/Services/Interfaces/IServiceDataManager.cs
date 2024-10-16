using DiscordMusicBot.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordMusicBot.Services.Interfaces
{
    internal interface IServiceDataManager : IService
    {
        BotData LoadConfig();
        AnalyticData LoadAnalytics();
        Task SaveAnalytics(AnalyticData data);

    }
}
