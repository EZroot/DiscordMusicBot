using System;
using System.Collections.Generic;

namespace DiscordMusicBot.Models
{
    [Serializable]
    public class SongAnalyticData
    {
        public int NumberOfPlays { get; set; }
        public SongData SongData { get; set; }
    }

    [Serializable]
    public class UserAnalyticData
    {
        public string UserName { get; set; }
        // Key: SongId, Value: SongAnalyticData
        public Dictionary<string, SongAnalyticData> SongHistory { get; set; } = new Dictionary<string, SongAnalyticData>();
    }

    [Serializable]
    public class AnalyticData
    {
        // Global song analytics (Key: SongId)
        public Dictionary<string, SongAnalyticData> GlobalMostPlayedSongs { get; set; } = new Dictionary<string, SongAnalyticData>();
        // Per-user analytics (Key: UserName)
        public Dictionary<string, UserAnalyticData> UserAnalytics { get; set; } = new Dictionary<string, UserAnalyticData>();
        // List of recent songs
        public List<SongData> RecentSongHistory { get; set; } = new List<SongData>();
    }
}