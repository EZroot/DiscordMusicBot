using DiscordMusicBot.Models;
using DiscordMusicBot.Services.Interfaces;
using DiscordMusicBot.Utils;
using Newtonsoft.Json;

namespace DiscordMusicBot.Services.Managers.Data
{
    internal class ConfigurationService : IServiceDataManager
    {
        private const string CONFIG_FILE_PATH = "config.json";
        private const string ANALYTIC_FILE_PATH = "analytics.json";

        private BotData? _botDataCache = null;

        public AnalyticData LoadAnalytics()
        {
            if (!File.Exists(ANALYTIC_FILE_PATH))
            {
                CreateDefaultAnalytics();
            }

            string json = File.ReadAllText(ANALYTIC_FILE_PATH);
            var analytics = JsonConvert.DeserializeObject<AnalyticData>(json);
            var result = new AnalyticData
            {
                GlobalMostPlayedSongs = analytics.GlobalMostPlayedSongs,
                UserAnalyticData = analytics.UserAnalyticData,
                RecentSongHistory = analytics.RecentSongHistory
            };
            return result;
        }

        public async Task SaveAnalytics(AnalyticData data)
        {
            try
            {
                string json = JsonConvert.SerializeObject(data, Formatting.Indented);
                await File.WriteAllTextAsync(ANALYTIC_FILE_PATH, json);
                //Debug.Log($"Saved: {json}");
            }
            catch (Exception ex)
            {
                Debug.Log($"Error during serialization: {ex.Message}");
            }
        }

        public BotData LoadConfig()
        {
            if (_botDataCache != null)
            {
                return (BotData)_botDataCache;
            }

            if (!File.Exists(CONFIG_FILE_PATH))
            {
                CreateDefaultConfig();
            }

            string json = File.ReadAllText(CONFIG_FILE_PATH);
            var config = JsonConvert.DeserializeObject<BotData>(json);
            var apiKey = config.ApiKey;//Environment.GetEnvironmentVariable(config.EnvPath);
            var guildId = config.GuildId;
            var motto = config.CustomStatus;
            var searchResultBtnEmojis = config.SearchResultButtonEmojis;
            var searchCmdDetailedEmbed = config.SearchCmdDetailedEmbed;
            var searchCmdButtonShortmode = config.SearchCmdButtonShortmode;

            var debugMode = config.DebugMode;

            if (!string.IsNullOrEmpty(apiKey))
            {
                var botData = new BotData
                {
                    ApiKey = apiKey,
                    GuildId = guildId,
                    CustomStatus = motto,
                    SearchResultButtonEmojis = searchResultBtnEmojis,
                    SearchCmdButtonShortmode = searchCmdButtonShortmode,
                    SearchCmdDetailedEmbed = searchCmdDetailedEmbed,
                    DebugMode = debugMode
                };

                _botDataCache = botData;
                return botData;
            }

            _botDataCache = config;

            return config;
        }

        private void CreateDefaultConfig()
        {
            var motto = new string[] { "Just chillaxin...", "Thinking about life..", "Pondering the universe", "Waiting for AI Overlords." };
            var defaultSearchResultButtonEmojis = new Dictionary<int, string>
            {
                { 0,"\u0030\uFE0F\u20E3" },
                { 1,"\u0031\uFE0F\u20E3" },
                { 2,"\u0032\uFE0F\u20E3" },
                { 3,"\u0033\uFE0F\u20E3" },
                { 4,"\u0034\uFE0F\u20E3" },
                { 5,"\u0035\uFE0F\u20E3" },
                { 6,"\u0036\uFE0F\u20E3" },
                { 7,"\u0037\uFE0F\u20E3" },
                { 8,"\u0038\uFE0F\u20E3" },
                { 9,"\u0039\uFE0F\u20E3" },
                { 10,"\u0031\uFE0F\u20E3\u0030\uFE0F\u20E3 " }
            };

            var defaultConfig = new BotData
            {
                ApiKey = "Replace me",
                EnvPath = "API_KEY",
                GuildId = "Replace me with server id",
                CustomStatus = motto,
                SearchResultButtonEmojis = defaultSearchResultButtonEmojis,
                SearchCmdDetailedEmbed = false,
                SearchCmdButtonShortmode = false,
                DebugMode = false
            };

            try
            {
                string json = JsonConvert.SerializeObject(defaultConfig, Formatting.Indented);
                File.WriteAllText(CONFIG_FILE_PATH, json);
            }
            catch (Exception ex)
            {
                Debug.Log($"Error during serialization: {ex.Message}");
            }

            Debug.Log("<color=green>config.json</color> created! <color=white>Please exit the bot and fill in your discord bot api key and guild id (server id).");
            Debug.Log("GuildId -> Right click your discord server 'Copy Server Id'");
            Debug.Log("ApiKey -> Sign in to https://discord.com/developers,  go to 'Bot' tab, Reset & Copy <color=white>Token");
            Debug.Log("<color=white>Example:");
            Debug.Log("\t\tApiKey	'MjY5NzcY5NMjY5NzcYMjY5Nzc.MjY5Nzc.MjY5NzcjY5NzcMjY5Nzc-ozWf2JDfLVtKGUK3rXQz'");
            Debug.Log("\t\tGuildId	'308708637679812608'");
            Console.Read();

        }

        private void CreateDefaultAnalytics()
        {
            var defaultAnalytics = new AnalyticData
            {
                // GlobalMostPlayedSongs = new List<SongAnlyticData>(),  
                // UserAnalyticData = new List<UserAnalyticData>(),       
                // RecentSongHistory = new SongData[10] //Default song history is 10                    
            };

            try
            {
                string json = JsonConvert.SerializeObject(defaultAnalytics, Formatting.Indented);
                File.WriteAllText(ANALYTIC_FILE_PATH, json);
            }
            catch (Exception ex)
            {
                Debug.Log($"Error during serialization: {ex.Message}");
            }


            Debug.Log("<color=green>analytics.json created!</color>");
        }
    }
}
