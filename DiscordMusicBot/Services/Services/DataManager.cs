﻿using DiscordMusicBot.Models;
using DiscordMusicBot.Services.Interfaces;
using DiscordMusicBot.Utils;
using Newtonsoft.Json;
using System.Text.Json;

namespace DiscordMusicBot.Services.Services
{
    internal class DataManager : IServiceDataManager
    {
        private const string CONFIG_FILE_PATH = "config.json";
        private const string ANALYTIC_FILE_PATH = "analytics.json";

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
                GlobalCommandUsage = analytics.GlobalCommandUsage,
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
                Console.WriteLine($"Error during serialization: {ex.Message}");
            }
        }

        public BotData LoadConfig()
        {
            if (!File.Exists(CONFIG_FILE_PATH))
            {
                CreateDefaultConfig();
            }

            string json = File.ReadAllText(CONFIG_FILE_PATH);
            var config = JsonConvert.DeserializeObject<BotData>(json);
            var apiKey = config.ApiKey;//Environment.GetEnvironmentVariable(config.EnvPath);
            var guildId = config.GuildId;

            if (!string.IsNullOrEmpty(apiKey)) return new BotData { ApiKey = apiKey, GuildId = guildId };
            return config;
        }

        private void CreateDefaultConfig()
        {
            var defaultConfig = new BotData
            {
                ApiKey = "Replace me",
                EnvPath = "API_KEY",
                GuildId = "Replace me with server id"
            };

            try
            {
                string json = JsonConvert.SerializeObject(defaultConfig, Formatting.Indented);
                File.WriteAllText(CONFIG_FILE_PATH, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during serialization: {ex.Message}");
            }


            Debug.Log("config.json created! Please exit the bot and fill in your discord bot api key and guild id (server id)");
            Console.Read();
            
        }

        private void CreateDefaultAnalytics()
        {
            var defaultAnalytics = new AnalyticData
            {
                GlobalMostPlayedSongs = new List<SongAnlyticData>(),  
                GlobalCommandUsage = new List<CommandAnalyticData>(),  
                UserAnalyticData = new List<UserAnalyticData>(),       
                RecentSongHistory = new SongData[10] //Default song history is 10                    
            };

            try
            {
                string json = JsonConvert.SerializeObject(defaultAnalytics, Formatting.Indented);
                File.WriteAllText(ANALYTIC_FILE_PATH, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during serialization: {ex.Message}");
            }


            Debug.Log("<color=green>analytics.json created!</color>");
        }
    }
}