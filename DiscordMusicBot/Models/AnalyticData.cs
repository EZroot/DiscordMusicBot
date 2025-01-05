namespace DiscordMusicBot.Models
{
    [System.Serializable]
    public class SongAnlyticData
    {
        public int NumberOfPlays { get; set; }
        public SongData SongData { get; set; }
    }
    [System.Serializable]
    public class UserAnalyticData
    {
        public string UserName { get; set; }
        // Key: SongId, Value: SongAnlyticData
        public Dictionary<string, SongAnlyticData> SongHistory { get; set; } = new Dictionary<string, SongAnlyticData>();
    }
    [System.Serializable]
    public class AnalyticData
    {
        // Key: SongId, Value: SongAnlyticData
        public Dictionary<string, SongAnlyticData> GlobalMostPlayedSongs { get; set; } = new Dictionary<string, SongAnlyticData>();
        // Key: UserName, Value: UserAnalyticData
        public Dictionary<string, UserAnalyticData> UserAnalyticData { get; set; } = new Dictionary<string, UserAnalyticData>();
        public List<SongData> RecentSongHistory { get; set; } = new List<SongData>();
    }

}
