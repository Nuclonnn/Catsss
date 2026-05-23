using UnityEngine;
using System.Collections.Generic;
public abstract class EventChannel<T> : ScriptableObject
{
    readonly HashSet<EventListener<T>> observers = new();

    public void Invoke(T value)
    {
        // Создаем временный список или идем с конца, если бы это был List
        var listenersToInvoke = new List<EventListener<T>>(observers);
        foreach (var observer in listenersToInvoke)
        {
            observer.Raise(value);
        }
    }

    public void Register(EventListener<T> observer){
        observers.Add(observer);
    }

    public void Deregister(EventListener<T> observer){
        observers.Remove(observer);
    }

}

public readonly struct Empty{}

[CreateAssetMenu(menuName = "Events/EventChannel")]
public class EventChannel : EventChannel<Empty>{}