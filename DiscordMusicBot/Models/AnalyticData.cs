namespace DiscordMusicBot.Models
{
    [System.Serializable]
    public struct AnalyticData
    {
        public List<SongAnlyticData> GlobalMostPlayedSongs;
        public List<UserAnalyticData> UserAnalyticData;
        public SongData[] RecentSongHistory;
    }

    [System.Serializable]
    public struct UserAnalyticData
    {
        public string UserName;
        public List<SongAnlyticData> SongHistory;
    }

    [System.Serializable]
    public struct SongAnlyticData
    {
        public int NumberOfPlays;
        public SongData SongData;
    }

    [System.Serializable]
    public struct CommandAnalyticData
    {
        public int NumberOfUses;
        public string CommandName;
    }
}
