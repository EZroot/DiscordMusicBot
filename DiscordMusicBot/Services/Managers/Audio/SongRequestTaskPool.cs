using System.Threading.Channels;
using DiscordMusicBot.Models;
using DiscordMusicBot.Services.Interfaces;
using DiscordMusicBot.Utils;

namespace DiscordMusicBot.Services.Managers.Audio
{
    /// <summary>
    /// Streamlining the task pool
    /// So instead of _ = Task.Run(async() => blah) we can just do _songRequestQueue.EnqueueSongAsync(blah)
    /// </summary>
    public class SongRequestTaskPool
    {
        // Create an unbounded channel for SongData items.
        // SingleReader = true means only 1 consumer takes items out at a time.
        private readonly Channel<SongData> _channel = Channel.CreateUnbounded<SongData>(
            new UnboundedChannelOptions
            {
                SingleWriter = false,
                SingleReader = true
            });

        public SongRequestTaskPool()
        {
            // Start a background task to process items from the channel
            _ = Task.Run(ProcessQueueAsync);
        }

        /// <summary>
        /// Enqueues a new song request. This is a producer call.
        /// </summary>
        public async Task EnqueueSongAsync(SongData song)
        {
            // This writes to the channel in a thread-safe manner
            Debug.Log("SRQ: Wrote song to channel");
            await _channel.Writer.WriteAsync(song);
        }

        /// <summary>
        /// Continuously processes the channel from a single consumer task.
        /// </summary>
        private async Task ProcessQueueAsync()
        {
            // Keep looping as long as there are songs coming in
            while (await _channel.Reader.WaitToReadAsync())
            {
                Debug.Log("SRQ: Tiggered! Processing queue!");

                // TryRead until there are no more items
                while (_channel.Reader.TryRead(out var song))
                {
                    Debug.Log($"SRQ: Reading queue.... Found {song.Title}");

                    // Here you call your actual audio manager to play
                    // This ensures one 'PlaySong' at a time in FIFO order
                    try
                    {
                        Debug.Log($"SRQ: Queuing {song.Title}");
                        await Service.Get<IServiceAudioManager>().PlaySong(song);
                    }
                    catch (Exception ex)
                    {
                        Debug.Log($"Error playing song '{song.Title}': {ex.Message}");
                    }
                }
            }
        }
    }
}
