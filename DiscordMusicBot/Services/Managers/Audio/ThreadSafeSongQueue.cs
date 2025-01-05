using DiscordMusicBot.Models;

namespace DiscordMusicBot.Services.Managers.Audio
{
    public class ThreadSafeSongQueue
    {
        private static readonly Random _rng = new Random();
        private readonly object _queueLock = new object();

        private readonly Queue<SongData> _songQueue = new Queue<SongData>();

        private SongData? _currentPlayingSong;

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

        public SongData[] SongQueueArray
        {
            get
            {
                lock (_queueLock)
                {
                    var result = new SongData[_songQueue.Count + 1];

                    // Put the current song at index 0 (if it’s non-null)
                    if (_currentPlayingSong != null)
                        result[0] = new SongData()
                        {
                            Id = _currentPlayingSong.Value.Id,
                            Title = _currentPlayingSong.Value.Title,
                            Url = _currentPlayingSong.Value.Url,
                            Length = _currentPlayingSong.Value.Length
                        };

                    // Copy the queue songs starting at index 1
                    _songQueue.ToArray().CopyTo(result, 1);
                    return result;
                }
            }
        }

        public void Enqueue(SongData song)
        {
            Utils.Debug.Log("AQ: Waiting for queue lock...");
            lock (_queueLock)
            {
                Utils.Debug.Log($"AQ: Added to queue: {song.Title} {song.Url}");
                _songQueue.Enqueue(song);
            }
        }

        public SongData? Dequeue()
        {
            lock (_queueLock)
            {
                if (_songQueue.Count == 0)
                    return null;

                return _songQueue.Dequeue();
            }
        }

        public SongData? PeekNext()
        {
            lock (_queueLock)
            {
                if (_songQueue.Count == 0)
                    return null;

                return _songQueue.Peek();
            }
        }

        public async Task ShuffleQueue()
        {
            lock (_queueLock)
            {
                var list = new List<SongData>(_songQueue);
                _songQueue.Clear();

                //fisher-yates shuffle 
                for (int i = list.Count - 1; i > 0; i--)
                {
                    int swapIndex = _rng.Next(i + 1);
                    (list[i], list[swapIndex]) = (list[swapIndex], list[i]);
                }

                foreach (var item in list)
                    _songQueue.Enqueue(item);
            }

            await Task.CompletedTask;
        }
    }
}
