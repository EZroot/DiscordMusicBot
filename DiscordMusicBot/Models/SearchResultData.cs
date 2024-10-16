using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordMusicBot.Models
{
    [System.Serializable]
    public struct SearchResultData
    {
        public string Title;
        public string Url;
        public string Duration;
    }
}
