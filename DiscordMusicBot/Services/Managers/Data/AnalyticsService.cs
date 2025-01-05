using DiscordMusicBot.Models;
using DiscordMusicBot.Services.Interfaces;
using System.Collections.Concurrent;
using System.Threading;

namespace DiscordMusicBot.Services.Managers.Data
{
    internal class AnalyticsService : IServiceAnalytics
    {
        private AnalyticData _analyticData;
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

        public AnalyticData AnalyticData => _analyticData;

        public async Task InitializeAsync()
        {
            try
            {
                _analyticData = Service.Get<IServiceDataManager>().LoadAnalytics() ?? new AnalyticData();
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                Utils.Debug.Log($"<color=red>Error during initialization: {ex.Message}</color>");
                _analyticData = new AnalyticData();
            }
        }

        public async Task AddSongAnalytics(string userName, SongData songData)
        {
            await _semaphore.WaitAsync();
            try
            {
                // Ensure songData has a unique Id
                if (string.IsNullOrEmpty(songData.Id))
                {
                    songData.Id = Guid.NewGuid().ToString();
                }

                // Update User Analytics
                if (!_analyticData.UserAnalyticData.TryGetValue(userName, out var userAnalytics))
                {
                    userAnalytics = new UserAnalyticData { UserName = userName };
                    _analyticData.UserAnalyticData[userName] = userAnalytics;
                }

                if (userAnalytics.SongHistory.TryGetValue(songData.Id, out var userSongData))
                {
                    userSongData.NumberOfPlays += 1;
                }
                else
                {
                    userAnalytics.SongHistory[songData.Id] = new SongAnlyticData
                    {
                        SongData = songData,
                        NumberOfPlays = 1
                    };
                }

                // Update Global Analytics
                if (_analyticData.GlobalMostPlayedSongs.TryGetValue(songData.Id, out var globalSongData))
                {
                    globalSongData.NumberOfPlays += 1;
                }
                else
                {
                    _analyticData.GlobalMostPlayedSongs[songData.Id] = new SongAnlyticData
                    {
                        SongData = songData,
                        NumberOfPlays = 1
                    };
                }

                // Update Recent Song History
                _analyticData.RecentSongHistory.Insert(0, songData);
                if (_analyticData.RecentSongHistory.Count > 100) // Assuming a limit of 100 recent songs
                {
                    _analyticData.RecentSongHistory.RemoveAt(_analyticData.RecentSongHistory.Count - 1);
                }
                // Save Analytics
                await Service.Get<IServiceDataManager>().SaveAnalytics(_analyticData);
            }
            catch (Exception ex)
            {
                Utils.Debug.Log($"<color=red>Error adding song analytics: {ex.Message}</color>");
            }
            finally
            {
                _semaphore.Release();
            }
        }

        // Additional methods to retrieve analytics can be added here
        public List<SongAnlyticData> GetTopGlobalSongs(int top = 10)
        {
            return _analyticData.GlobalMostPlayedSongs
                                .Values
                                .OrderByDescending(s => s.NumberOfPlays)
                                .Take(top)
                                .ToList();
        }

        public List<SongAnlyticData> GetUserTopSongs(string userName, int top = 10)
        {
            if (_analyticData.UserAnalyticData.TryGetValue(userName, out var userAnalytics))
            {
                return userAnalytics.SongHistory
                                    .Values
                                    .OrderByDescending(s => s.NumberOfPlays)
                                    .Take(top)
                                    .ToList();
            }
            return new List<SongAnlyticData>();
        }

        public List<SongData> GetRecentSongs(int count = 10)
        {
            return _analyticData.RecentSongHistory.Take(count).ToList();
        }
    }
}
