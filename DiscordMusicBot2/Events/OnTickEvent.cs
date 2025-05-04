namespace DiscordMusicBot2.Events
{
    internal struct OnTickEvent
    {
        public double Tick;
        public OnTickEvent(double tick) { Tick = tick; }
    }
}
