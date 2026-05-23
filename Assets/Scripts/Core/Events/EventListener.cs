using UnityEngine;
using UnityEngine.Events;

namespace Catsss.Core.Events
{
    public abstract class EventListener<T> : MonoBehaviour
    {
        [SerializeField] private EventChannel<T> eventChannel;
        [SerializeField] private UnityEvent<T> response;

        protected virtual void OnEnable()
        {
            if (eventChannel != null)
            {
                eventChannel.Register(this);
            }
        }

        protected virtual void OnDisable()
        {
            if (eventChannel != null)
            {
                eventChannel.Deregister(this);
            }
        }

        public void Raise(T value)
        {
            response?.Invoke(value);
        }
    }
}
