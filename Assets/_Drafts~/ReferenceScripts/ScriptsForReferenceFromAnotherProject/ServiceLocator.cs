using System;
using System.Collections.Generic;

public static class ServiceLocator
{
    private static readonly Dictionary<Type, object> _services = new Dictionary<Type, object>();

    public static void Register<T>(T service) => _services[typeof(T)] = service;
    public static void Unregister<T>() => _services.Remove(typeof(T));
    public static T Get<T>() => (T)_services[typeof(T)];
}

