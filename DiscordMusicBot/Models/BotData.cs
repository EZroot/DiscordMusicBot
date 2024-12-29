namespace DiscordMusicBot.Models
{
    [System.Serializable]
    public struct BotData
    {
        public string ApiKey;
        public string EnvPath;
        public string GuildId; 
        public string[] CustomStatus;
        public bool DebugMode;
    }
}
