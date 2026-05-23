using System;
using System.Collections.Generic;
using UnityEngine;

namespace Catsss.Core.Events
{
    public abstract class EventChannel<T> : ScriptableObject
    {
        private readonly HashSet<EventListener<T>> _listeners = new();

        public event Action<T> Raised;

        public void Invoke(T value)
        {
            Raised?.Invoke(value);
            var listenersSnapshot = new List<EventListener<T>>(_listeners);

            foreach (EventListener<T> listener in listenersSnapshot)
            {
                if (listener != null)
                {
                    listener.Raise(value);
                }
            }
        }

        public void Register(EventListener<T> listener)
        {
            _listeners.Add(listener);
        }

        public void Deregister(EventListener<T> listener)
        {
            _listeners.Remove(listener);
        }
    }
}
