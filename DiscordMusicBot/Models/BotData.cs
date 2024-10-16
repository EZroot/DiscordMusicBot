using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordMusicBot.Models
{
    [System.Serializable]
    public struct BotData
    {
        public string ApiKey;
        public string EnvPath;
        public string GuildId; 
    }
}
