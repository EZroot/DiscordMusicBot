
using System.Timers;
using DiscordMusicBot.Events;
using DiscordMusicBot.Events.Events;

namespace DiscordMusicBot.Utils
{
    public class BotTimer
    {
        private System.Timers.Timer _timer;
        public BotTimer(double interval = 15000)
        {
            _timer = new System.Timers.Timer(interval);
            _timer.Elapsed += OnTimedEvent; 
            _timer.AutoReset = true; 
            _timer.Enabled = true; 
        }

        private void OnTimedEvent(Object source, ElapsedEventArgs e)
        {
            Utils.Debug.Log("Timer event called!");
            EventHub.Raise(new EvOnTimerLoop());
        }
    }
}