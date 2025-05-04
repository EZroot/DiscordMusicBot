using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordMusicBot2.Data
{
    public class ConfigData
    {
        public readonly string BotToken;
        public readonly string GuildId;

        public ConfigData(string botToken, string guildId)
        {
            BotToken = botToken;
            GuildId = guildId;
        }
    }
}
