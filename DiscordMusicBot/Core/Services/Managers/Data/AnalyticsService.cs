using DiscordMusicBot.Models;
using DiscordMusicBot.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DiscordMusicBot.Services.Managers.Data
{
    internal class AnalyticsService : IServiceAnalytics
    {
        private AnalyticData _analytics;
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

        public AnalyticData Analytics => _analytics;

        public async Task InitializeAsync()
        {
            try
            {
                _analytics = Service.Get<IServiceDataManager>().LoadAnalytics() ?? new AnalyticData();
            }
            catch (Exception ex)
            {
                Utils.Debug.Log($"<color=red>Error initializing analytics: {ex.Message}</color>");
                _analytics = new AnalyticData();
            }
            await Task.CompletedTask;
        }

        public async Task AddSongAnalyticsAsync(string userName, SongData songData)
        {
            await _semaphore.WaitAsync();
            try
            {
                if (string.IsNullOrEmpty(songData.Id))
                {
                    songData.Id = Guid.NewGuid().ToString();
                }

                // Update per-user analytics.
                if (!_analytics.UserAnalytics.TryGetValue(userName, out var userAnalytics))
                {
                    userAnalytics = new UserAnalyticData { UserName = userName };
                    _analytics.UserAnalytics[userName] = userAnalytics;
                }

                if (userAnalytics.SongHistory.TryGetValue(songData.Id, out var userSongData))
                {
                    userSongData.NumberOfPlays++;
                }
                else
                {
                    userAnalytics.SongHistory[songData.Id] = new SongAnalyticData
                    {
                        SongData = songData,
                        NumberOfPlays = 1
                    };
                }

                // Update global analytics.
                if (_analytics.GlobalMostPlayedSongs.TryGetValue(songData.Id, out var globalSongData))
                {
                    globalSongData.NumberOfPlays++;
                }
                else
                {
                    _analytics.GlobalMostPlayedSongs[songData.Id] = new SongAnalyticData
                    {
                        SongData = songData,
                        NumberOfPlays = 1
                    };
                }

                // Update recent songs list (keeping a max of 5).
                _analytics.RecentSongHistory.Insert(0, songData);
                const int maxRecent = 5;
                if (_analytics.RecentSongHistory.Count > maxRecent)
                {
                    _analytics.RecentSongHistory.RemoveAt(_analytics.RecentSongHistory.Count - 1);
                }

                // Persist changes.
                await Service.Get<IServiceDataManager>().SaveAnalytics(_analytics);
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

        public List<SongAnalyticData> GetTopGlobalSongs(int top = 10)
        {
            return _analytics.GlobalMostPlayedSongs.Values
                            .OrderByDescending(s => s.NumberOfPlays)
                            .Take(top)
                            .ToList();
        }

        public List<SongAnalyticData> GetUserTopSongs(string userName, int top = 10)
        {
            if (_analytics.UserAnalytics.TryGetValue(userName, out var userAnalytics))
            {
                return userAnalytics.SongHistory.Values
                                    .OrderByDescending(s => s.NumberOfPlays)
                                    .Take(top)
                                    .ToList();
            }
            return new List<SongAnalyticData>();
        }

        public List<SongData> GetRecentSongs(int count = 10)
        {
            return _analytics.RecentSongHistory.Take(count).ToList();
        }
    }
}
