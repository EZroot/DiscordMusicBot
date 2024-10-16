using DiscordMusicBot.Services.Interfaces;
using DiscordMusicBot.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace DiscordMusicBot.Services
{
    internal static class Service
    {
        private static Dictionary<Type, object> _registry = new Dictionary<Type, object>();

        public static void ClearRegistry()
        {
            _registry?.Clear();
        }

        public static void Register<T>() where T : IService
        {
            _registry ??= new Dictionary<Type, object>();

            if (_registry.ContainsKey(typeof(T))) return;

            var types = Assembly.GetExecutingAssembly().GetTypes();
            var potentialService = types.FirstOrDefault(type =>
                (typeof(T)).IsAssignableFrom(type) && type.IsClass && !type.IsAbstract);

            if (potentialService == null)
            {
                throw new InvalidOperationException($"No class found that implements {typeof(T)}");
            }

            var instance = Activator.CreateInstance(potentialService);
            _registry[typeof(T)] = instance;

            Debug.Log($"Registered Service <color=cyan>{typeof(T)}</color> successfully!");
        }

        public static T Get<T>() where T : IService
        {
            if (!_registry.ContainsKey(typeof(T)))
            {
                Register<T>();
            }

            return (T)_registry[typeof(T)];
        }
    }
}
