using System.Diagnostics;
using DiscordMusicBot.Models;
using DiscordMusicBot.Services.Interfaces;

namespace DiscordMusicBot.Services.Managers
{
    internal class AnalyticsManager : IServiceAnalytics
    {
        private AnalyticData _analyticData;
        public AnalyticData AnalyticData => _analyticData;

        public async Task Initialize()
        {
            try
            {
                _analyticData = Service.Get<IServiceDataManager>().LoadAnalytics();
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                Utils.Debug.Log($"<color=red>Error during initialization: {ex.Message}");
            }
        }

        public async Task AddSongAnalytics(string userName, SongData songData)
        {
            try
            {
                var userAnalytics = _analyticData.UserAnalyticData.FirstOrDefault(u => u.UserName == userName);
                if (userAnalytics != null && userAnalytics.UserName != null)
                {
                    userAnalytics.SongHistory.Add(new SongAnlyticData { SongData = songData, NumberOfPlays = 1 });
                    userAnalytics.SongHistory = userAnalytics.SongHistory.OrderBy(s => s.SongData.Title).ToList();
                }
                else
                {
                    _analyticData.UserAnalyticData.Add(new UserAnalyticData
                    {
                        UserName = userName,
                        SongHistory = new List<SongAnlyticData> { new SongAnlyticData { SongData = songData, NumberOfPlays = 1 } }
                    });

                    var newUserAnalytics = _analyticData.UserAnalyticData.First(u => u.UserName == userName);
                    _analyticData.GlobalMostPlayedSongs = _analyticData.GlobalMostPlayedSongs.OrderByDescending(s => s.NumberOfPlays).ToList();
                }
                var globalSongData = _analyticData.GlobalMostPlayedSongs.FirstOrDefault(s => s.SongData.Title.Equals(songData.Title));
                if (globalSongData == null || string.IsNullOrEmpty(globalSongData.SongData.Title))
                {
                    _analyticData.GlobalMostPlayedSongs.Add(new SongAnlyticData { SongData = songData, NumberOfPlays = 1 });
                }
                else
                {
                    globalSongData.NumberOfPlays++;
                }

                _analyticData.GlobalMostPlayedSongs = _analyticData.GlobalMostPlayedSongs.OrderBy(s => s.NumberOfPlays).ToList();

                for (int i = _analyticData.RecentSongHistory.Length - 1; i > 0; i--)
                {
                    _analyticData.RecentSongHistory[i] = _analyticData.RecentSongHistory[i - 1]; 
                }
                _analyticData.RecentSongHistory[0] = songData;

                await Service.Get<IServiceDataManager>().SaveAnalytics(_analyticData);
            }
            catch (Exception ex)
            {
                // Handle or log the exception as needed
                Utils.Debug.Log($"<color=red>Error adding song analytics: {ex.Message}</color>");
            }
        }
    }
}
