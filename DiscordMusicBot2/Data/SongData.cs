using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordMusicBot2.Data
{
    internal class SongData
    {
        public int ID { get; }
        public string Name { get; }
        public string Url { get; }
        public string Duration { get; }

        public SongData(int id, string name, string url, string duration) 
        {
            ID = id;
            Name = name;
            Url = url;
            Duration = duration;
        }
    }
}