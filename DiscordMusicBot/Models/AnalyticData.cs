namespace DiscordMusicBot.Models
{
    [System.Serializable]
    public class AnalyticData
    {
        public List<SongAnlyticData> GlobalMostPlayedSongs;
        public List<UserAnalyticData> UserAnalyticData;
        public SongData[] RecentSongHistory;
    }

    [System.Serializable]
    public class UserAnalyticData
    {
        public string UserName;
        public List<SongAnlyticData> SongHistory = new List<SongAnlyticData>();
    }


    [System.Serializable]
    public class SongAnlyticData
    {
        public int NumberOfPlays;
        public SongData SongData;
    }

    [System.Serializable]
    public class CommandAnalyticData
    {
        public int NumberOfUses;
        public string CommandName;
    }
}
