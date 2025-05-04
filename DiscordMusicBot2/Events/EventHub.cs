namespace DiscordMusicBot2.Events
{
    public static class EventHub
    {
        private static readonly Dictionary<Type, Delegate> m_eventDelegates = new();

        public static void Subscribe<T>(Action<T> del) where T : struct
        {
            var type = typeof(T);
            if (!m_eventDelegates.ContainsKey(type))
            {
                m_eventDelegates[type] = null;
            }
            m_eventDelegates[type] = (Action<T>)m_eventDelegates[type] + del;
            Debug.Log($"<color=orange>Subscribed to event</color> {type.Name}");
        }

        public static void Unsubscribe<T>(Action<T> del) where T : struct
        {
            var type = typeof(T);
            if (m_eventDelegates.ContainsKey(type))
            {
                m_eventDelegates[type] = (Action<T>)m_eventDelegates[type] - del;
                Debug.Log($"<color=orange>Unsubscribed from event</color> {type.Name}");
            }
        }

        public static void Raise<T>(T eventArg) where T : struct
        {
            var type = typeof(T);
            if (m_eventDelegates.ContainsKey(type))
            {
                var del = (Action<T>)m_eventDelegates[type];
                if (del != null)
                {
                    foreach (var handler in del.GetInvocationList())
                    {
                        try
                        {
                            ((Action<T>)handler).Invoke(eventArg);
                        }
                        catch (Exception ex)
                        {
                            Debug.Log($"Error occurred processing {type.Name} event: {ex}");
                        }
                    }
                    //Debug.Log($"<color=orange>Raised event</color> {type.Name}");
                }
            }
        }
    }
}
