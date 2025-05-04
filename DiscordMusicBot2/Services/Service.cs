using DiscordMusicBot2.Services.Interface;
using System.Reflection;

namespace DiscordMusicBot2.Services
{
    internal static class Service
    {
        private static readonly Dictionary<Type, object> _registry = new();

        /// <summary>Delete every singleton (use only in unit tests).</summary>
        public static void ClearRegistry() => _registry.Clear();

        /// <summary>
        /// Register the concrete implementation that satisfies <typeparamref name="T"/>.
        /// Fails if <typeparamref name="T"/> is not an interface or already registered.
        /// </summary>
        public static void Register<T>() where T : IService
        {
            // Enforce "interface only" rule at runtime
            if (!typeof(T).IsInterface)
                throw new InvalidOperationException(
                    $"Service keys must be interfaces. Use IServiceFoo, not FooService.");

            // Don’t allow double‑registration
            if (_registry.ContainsKey(typeof(T)))
                throw new InvalidOperationException(
                    $"{typeof(T).Name} is already registered.");

            // Find the concrete type that implements T
            var impl = Assembly.GetExecutingAssembly()
                               .GetTypes()
                               .FirstOrDefault(t =>
                                    typeof(T).IsAssignableFrom(t) &&
                                    t is { IsClass: true, IsAbstract: false });

            if (impl == null)
                throw new InvalidOperationException(
                    $"No concrete class found that implements {typeof(T).Name}.");

            // Create and store the singleton
            _registry[typeof(T)] = Activator.CreateInstance(impl)!;

            Debug.Log($"Registered service <color=cyan>{typeof(T).Name}</color> → {impl.Name}");
        }

        /// <summary>
        /// Resolve the singleton implementing <typeparamref name="T"/> (registers lazily if needed).
        /// </summary>
        public static T Get<T>() where T : IService
        {
            if (!typeof(T).IsInterface)
                throw new InvalidOperationException(
                    $"Resolve services by interface only. Use IServiceFoo, not FooService.");

            if (!_registry.TryGetValue(typeof(T), out var instance))
                Register<T>();                     // this will throw on race/double‑register

            return (T)_registry[typeof(T)];
        }
    }
}
