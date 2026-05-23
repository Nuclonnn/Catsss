using System;
using System.Collections.Generic;

namespace Catsss.Core.Services
{
    public static class ServiceLocator
    {
        private static readonly Dictionary<Type, object> Services = new();

        public static void Register<T>(T service)
        {
            if (service == null)
            {
                throw new ArgumentNullException(nameof(service));
            }

            Services[typeof(T)] = service;
        }

        public static void Unregister<T>()
        {
            Services.Remove(typeof(T));
        }

        public static T Get<T>()
        {
            if (!Services.TryGetValue(typeof(T), out object service))
            {
                throw new InvalidOperationException($"Service {typeof(T)} is not registered in ServiceLocator.");
            }

            return (T)service;
        }

        public static bool TryGet<T>(out T service)
        {
            if (Services.TryGetValue(typeof(T), out object value))
            {
                service = (T)value;
                return true;
            }

            service = default;
            return false;
        }

        public static void Clear()
        {
            Services.Clear();
        }
    }
}
