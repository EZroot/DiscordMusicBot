using Discord;
using Discord.Audio;
using Discord.WebSocket;
using DiscordMusicBot2.Audio.Interface;
using DiscordMusicBot2.Data;
using DiscordMusicBot2.Events;
using DiscordMusicBot2.Helpers;
using System.Diagnostics;

namespace DiscordMusicBot2.Audio
{
    internal class ServiceAudio : IServiceAudio
    {
        private const int MINUTE_THRESHOLD_TO_PLAY_LIVE = 8; //8 minutes
        private IAudioClient? m_audioClient;
        private SongData? m_currentPlayingSong = null;
        private SongBuffer m_songBuffer;
        private ProcessPlaybackManager m_playbackManager;

        public List<SongData> SongDataList => m_songBuffer.m_songBufferQueue.ToList();

        public ServiceAudio()
        {
            Debug.Log("Service audio initialized");
            m_songBuffer = new SongBuffer();
            EventHub.Subscribe<OnSongFinishedEvent>(OnSongFinished);
        }

        private void OnSongFinished(OnSongFinishedEvent @event)
        {
            _ = Skip();
            //m_currentPlayingSong = null;
            //_ = StartNextSong();
        }

        /// <summary>
        /// Join discord voice channel and setup Audio Client Control
        /// </summary>
        /// <returns></returns>
        public async Task JoinVoiceChannel(IVoiceChannel targetChannel)
        {
            if (m_audioClient == null)
            {
                m_audioClient = await targetChannel.ConnectAsync(selfDeaf: true).ConfigureAwait(false);
                m_playbackManager = new ProcessPlaybackManager(m_audioClient);
            }
            else
            {
                Debug.Log("<color=red>Audio client is already connected. Cannot join another channel.</color>");
            }
        }

        /// <summary>
        /// Leave a discord voice channel
        /// </summary>
        /// <returns></returns>
        public async Task LeaveCurrentVoiceChannel(IVoiceChannel voiceChannel)
        {
            await voiceChannel.DisconnectAsync().ConfigureAwait(false);

            if (m_audioClient != null)
            {
                try
                {
                    await m_audioClient.StopAsync().ConfigureAwait(false);
                }
                finally
                {
                    m_audioClient.Dispose();
                    m_audioClient = null;
                }
            }
        }

        /// <summary>
        /// Play a song directly from youtube URL
        /// </summary>
        /// <returns></returns>
        public async Task Play(string youtubeUrl)
        {
            if (m_audioClient?.ConnectionState != ConnectionState.Connected)
            {
                Debug.Log("<color=red>No live voice connection.</color>");
                return;
            }

            var songDetails = await YoutubeHelper.GetVideoDetails(youtubeUrl);
            var title = songDetails[0];
            var length = songDetails[1];

            m_songBuffer.AddSongToQueue(new SongData(0, title, youtubeUrl, length));

            await StartNextSong();
        }

        public async Task<bool> ChangeVolume(float vol)
        {
            if(m_playbackManager == null)
            {
                Debug.Log("<color=red>Playback manager is not initialized.</color>");
                return false;
            }
            await m_playbackManager.SetVolume(vol);
            return true;
        }

        private async Task StartNextSong()
        {
            Debug.Log($"Start Next Song Called: Count - {m_songBuffer.m_songBufferQueue.Count} - CurrentPlayingSongNull {m_currentPlayingSong == null}");
            if(m_currentPlayingSong != null)
            {
                Debug.Log($"WHAT THE FUCK - current playing song is {m_currentPlayingSong.Name}");
            }

            if (m_currentPlayingSong == null)
            {
                Debug.Log("Trying to start next song");

                m_currentPlayingSong = m_songBuffer.GetSongFromQueue();

                if (m_currentPlayingSong != null)
                {
                    Debug.Log($"Playing song: {m_currentPlayingSong.Name}");
                    
                    var parsedDuration = YoutubeHelper.ParseDuration(m_currentPlayingSong.Duration);
                    var aboveMinuteThreshold = false;
                    if(parsedDuration != null)
                        aboveMinuteThreshold = parsedDuration.Value.Minutes > MINUTE_THRESHOLD_TO_PLAY_LIVE;

                    //Live streams
                    //if (string.IsNullOrEmpty(m_currentPlayingSong.Duration) || aboveMinuteThreshold)
                    //{
                    //    Debug.Log("<color=cyan>Live streaming the song...");
                    //    if(m_currentPlayingSong.Url.Contains("youtube") || m_currentPlayingSong.Url.Contains("youtu.be"))
                    //        await m_playbackManager.PlayLiveYoutubeAsync(m_currentPlayingSong.Url, true).ConfigureAwait(false);
                    //    else
                    //        await m_playbackManager.PlayLiveAsync(m_currentPlayingSong.Url).ConfigureAwait(false);
                    //}
                    ////Normal songs
                    //else
                    //{
                        Debug.Log("<color=cyan>Predownloading song...");
                        //await m_playbackManager.PlayAsync(m_currentPlayingSong.Url).ConfigureAwait(false);
                        await m_playbackManager.PlayAsync(m_currentPlayingSong.Url).ConfigureAwait(false);
                    //}
                }
                else
                {
                    Debug.Log("<color=red>Failed to get song from queue.</color>");
                }
            }
            else
            {
                Debug.Log("<color=red>Already playing a song. Cannot play another one.</color>");
            }
        }

        /// <summary>
        /// Skip the song currently playing
        /// </summary>
        /// <returns></returns>
        public async Task Skip()
        {
            m_currentPlayingSong = null;
            _ = StartNextSong();
        }

        /// <summary>
        /// Display the queue of the current song list
        /// </summary>
        /// <returns></returns>
        public async Task Queue()
        {

        }

        /// <summary>
        /// Shuffle the song queue
        /// </summary>
        /// <returns></returns>
        public async Task Shuffle()
        {

        }

        /// <summary>
        /// Search youtube and return a list of songs based on max search result
        /// </summary>
        /// <returns></returns>
        public async Task Search()
        {

        }
    }
}
