using DiscordMusicBot.Models;
using System.Collections.Generic;

namespace DiscordMusicBot.Services.Managers.Audio
{
    internal class AudioQueuer
    {
        // Lock object to guard access to the queue and current song
        private readonly object _queueLock = new object();

        // Internal queue of songs waiting to be played
        private readonly Queue<SongData> _songQueue = new Queue<SongData>();

        // Keep track of the current song playing
        private SongData? _currentPlayingSong;

        // Provide a thread-safe way to check how many songs are queued
        public int SongCount
        {
            get
            {
                lock (_queueLock)
                {
                    return _songQueue.Count;
                }
            }
        }

        public SongData[] SongQueue
        {
            get
            {
                lock (_queueLock)
                {
                    var result = new SongData[_songQueue.Count + 1];

                    // Put the current song at index 0 (if it’s non-null)
                    if (_currentPlayingSong != null)
                        result[0] = new SongData() { 
                            Id = _currentPlayingSong.Value.Id,
                            Title = _currentPlayingSong.Value.Title,
                            Url = _currentPlayingSong.Value.Url,
                            Length = _currentPlayingSong.Value.Length };

                    // Copy the queue songs starting at index 1
                    _songQueue.ToArray().CopyTo(result, 1);
                    return result;
                }
            }
        }

        /// <summary>
        /// Adds a new song to the queue in a thread-safe manner.
        /// </summary>
        public void Enqueue(SongData song)
        {
            lock (_queueLock)
            {
                _songQueue.Enqueue(song);
            }
        }

        /// <summary>
        /// Dequeues the next song if available, returning null if empty.
        /// Thread-safe as well.
        /// </summary>
        public SongData? Dequeue()
        {
            lock (_queueLock)
            {
                if (_songQueue.Count == 0)
                    return null;

                return _songQueue.Dequeue();
            }
        }

        /// <summary>
        /// Peeks at the next song in the queue without removing it.
        /// </summary>
        public SongData? PeekNext()
        {
            lock (_queueLock)
            {
                if (_songQueue.Count == 0)
                    return null;

                return _songQueue.Peek();
            }
        }

        /// <summary>
        /// Access or modify the currently playing song in a thread-safe manner.
        /// </summary>
        public SongData? CurrentPlayingSong
        {
            get
            {
                lock (_queueLock)
                {
                    return _currentPlayingSong;
                }
            }
            set
            {
                lock (_queueLock)
                {
                    _currentPlayingSong = value;
                }
            }
        }
    }
}
