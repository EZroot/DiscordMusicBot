using Discord;
using Discord.Audio;
using Discord.WebSocket;
using DiscordMusicBot2.Audio.Interface;
using DiscordMusicBot2.Data;
using DiscordMusicBot2.Events;
using System.Diagnostics;

namespace DiscordMusicBot2.Audio
{
    internal class ServiceAudio : IServiceAudio
    {
        private IAudioClient? m_audioClient;
        private SongData? m_currentPlayingSong = null;
        private SongBuffer m_songBuffer;
        private ProcessPlaybackManager m_playbackManager;

        public List<SongData> SongDataList => m_songBuffer.m_songBuffer.ToList();

        public ServiceAudio()
        {
            Debug.Log("Service audio initialized");
            m_songBuffer = new SongBuffer();
            EventHub.Subscribe<OnSongFinishedEvent>(OnSongFinished);
        }

        private void OnSongFinished(OnSongFinishedEvent @event)
        {
            m_currentPlayingSong = null;
            _ = StartNextSong();
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

            m_songBuffer.AddSongToQueue(new SongData(0, "random", youtubeUrl, "99:99"));

            await StartNextSong();
        }

        public async Task ChangeVolume(float vol)
        {
            await m_playbackManager.SetVolume(vol);
        }

        private async Task StartNextSong()
        {
            if (m_currentPlayingSong == null)
            {
                m_currentPlayingSong = m_songBuffer.GetSongFromQueue();
                if (m_currentPlayingSong != null)
                {
                    Debug.Log($"Playing song: {m_currentPlayingSong.Name}");
                    await m_playbackManager.PlayAsync(m_currentPlayingSong.Url).ConfigureAwait(false);
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
