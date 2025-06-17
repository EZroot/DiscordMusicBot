using DiscordMusicBot2.Events;
using DiscordMusicBot2.Timer.Interface;
using System.Diagnostics;
using System.Linq.Expressions;

namespace DiscordMusicBot2.Timer
{
    internal class ServiceTick : IServiceTick
    {
        private bool m_isRunning;
        private long m_millisecondsPerTick = 1000;
        private long m_prevElapsedMilliseconds;
        private ulong m_totalTickCount;

        public ServiceTick() 
        {
            Start();
        }

        /// <summary>
        /// Creates a looping timer that will raise OnTickEvent on every loop cycle
        /// </summary>
        public void Start()
        {
            m_isRunning = true;
            Task.Run(() => UpdateLoop());
        }

        private void UpdateLoop()
        {
            Stopwatch sw = Stopwatch.StartNew();
            while(m_isRunning)
            {
                if((sw.ElapsedMilliseconds - m_prevElapsedMilliseconds) > m_millisecondsPerTick)
                {
                    m_prevElapsedMilliseconds = sw.ElapsedMilliseconds;
                    m_totalTickCount++;
                    _ = EventHub.RaiseAsync(new OnTickEvent(m_totalTickCount));
                }
            }
            sw.Stop();
        }
    }
}
