using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 타입 기반 publish/subscribe 이벤트 허브입니다.
/// </summary>
public class EventBus
{
    private static readonly Lazy<EventBus> _instance = new Lazy<EventBus>(() => new EventBus());
    public static EventBus Instance => _instance.Value;

    private readonly Dictionary<Type, Delegate> _events = new Dictionary<Type, Delegate>();

    private EventBus() { }

    public void Subscribe<T>(Action<T> onEvent)
    {
        Type eventType = typeof(T);
        if (_events.TryGetValue(eventType, out var existingDelegate))
        {
            _events[eventType] = Delegate.Combine(existingDelegate, onEvent);
        }
        else
        {
            _events[eventType] = onEvent;
        }
    }

    public void Unsubscribe<T>(Action<T> onEvent)
    {
        Type eventType = typeof(T);
        if (_events.TryGetValue(eventType, out var existingDelegate))
        {
            var newDelegate = Delegate.Remove(existingDelegate, onEvent);
            if (newDelegate == null) _events.Remove(eventType);
            else _events[eventType] = newDelegate;
        }
    }

    public void Publish<T>(T eventMessage)
    {
        Type eventType = typeof(T);
        if (_events.TryGetValue(eventType, out var existingDelegate))
        {
            try
            {
                (existingDelegate as Action<T>)?.Invoke(eventMessage);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[EventBus] '{eventType.Name}' 실행 중 에러 발생:\n{ex}");
            }
        }
    }
}