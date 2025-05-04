using DiscordMusicBot2.Data;

namespace DiscordMusicBot2.Audio
{
    internal class SongBuffer
    {
        public Queue<SongData> m_songBuffer = new();

        public SongBuffer() 
        {

        }

        public void AddSongToQueue(SongData songData)
        {
            Debug.Log($"Enqueueing song data: {songData.ID} {songData.Name} {songData.Duration}");
            m_songBuffer.Enqueue(songData);
        }

        public SongData? GetSongFromQueue()
        {
            if (m_songBuffer.TryDequeue(out var songData))
            {
                return songData;
            }
            Debug.Log("<color=red>Failed to Dequeue Song Data - No songs available. Returning Null.");
            return null;
        }
    }
}
