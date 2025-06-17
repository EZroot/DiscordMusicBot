namespace DiscordMusicBot2.Events
{
    public static class EventHub
    {
        // We’ll keep a list of async handlers per event type
        private static readonly Dictionary<Type, List<Delegate>> _handlers = new();

        public static void Subscribe<T>(Func<T, Task> handler) where T : struct
        {
            var type = typeof(T);
            if (!_handlers.TryGetValue(type, out var list))
            {
                list = new List<Delegate>();
                _handlers[type] = list;
            }
            list.Add(handler);
            Debug.Log($"<color=orange>Subscribed to async event</color> {type.Name}");
        }

        public static void Unsubscribe<T>(Func<T, Task> handler) where T : struct
        {
            var type = typeof(T);
            if (_handlers.TryGetValue(type, out var list))
            {
                list.Remove(handler);
                Debug.Log($"<color=orange>Unsubscribed from async event</color> {type.Name}");
                if (list.Count == 0) _handlers.Remove(type);
            }
        }

        // Now returns a Task so callers can await it
        public static async Task RaiseAsync<T>(T eventArg) where T : struct
        {
            var type = typeof(T);
            if (!_handlers.TryGetValue(type, out var list))
                return;

            // Copy to avoid mutation during invocation
            var handlers = list.Cast<Func<T, Task>>().ToArray();
            foreach (var handler in handlers)
            {
                try
                {
                    await handler(eventArg).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Debug.Log($"Error in async handler for {type.Name}: {ex}");
                }
            }
        }
    }
}
