using DiscordMusicBot.Utils;
namespace DiscordMusicBot.Events
{
    public static class EventHub
    {
        private static readonly Dictionary<Type, Delegate> eventDelegates = new Dictionary<Type, Delegate>();

        public static void Subscribe<T>(Action<T> del) where T : struct
        {
            var type = typeof(T);
            if (!eventDelegates.ContainsKey(type))
            {
                eventDelegates[type] = null;
            }
            eventDelegates[type] = (Action<T>)eventDelegates[type] + del;
            Debug.Log($"<color=orange>Subscribed to event</color> {type.Name}");
        }

        public static void Unsubscribe<T>(Action<T> del) where T : struct
        {
            var type = typeof(T);
            if (eventDelegates.ContainsKey(type))
            {
                eventDelegates[type] = (Action<T>)eventDelegates[type] - del;
               Debug.Log($"<color=orange>Unsubscribed from event</color> {type.Name}");
            }
        }

        public static void Raise<T>(T eventArg) where T : struct
        {
            var type = typeof(T);
            if (eventDelegates.ContainsKey(type))
            {
                var del = (Action<T>)eventDelegates[type];
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
