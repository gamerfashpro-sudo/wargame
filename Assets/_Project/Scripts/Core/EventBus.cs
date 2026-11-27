using System;
using System.Collections.Generic;

namespace WG.Core
{
    /// <summary>Простая типобезопасная шина событий: EventBus.Publish(new X()); EventBus.Subscribe&lt;X&gt;(handler).</summary>
    public static class EventBus
    {
        static readonly Dictionary<Type, Delegate> handlers = new();

        public static void Subscribe<T>(Action<T> handler)
        {
            handlers.TryGetValue(typeof(T), out var d);
            handlers[typeof(T)] = Delegate.Combine(d, handler);
        }

        public static void Unsubscribe<T>(Action<T> handler)
        {
            if (!handlers.TryGetValue(typeof(T), out var d)) return;
            d = Delegate.Remove(d, handler);
            if (d == null) handlers.Remove(typeof(T)); else handlers[typeof(T)] = d;
        }

        public static void Publish<T>(T evt)
        {
            if (handlers.TryGetValue(typeof(T), out var d)) (d as Action<T>)?.Invoke(evt);
        }

        public static void Clear() => handlers.Clear();
    }

    // ---- События ядра ----
    public struct SettingsChanged { }
    public struct LanguageChanged { }
    public struct GameSpeedChanged { public int Speed; }
    public struct SelectionChanged { public UnityEngine.GameObject Selected; }
    public struct NotificationRaised { public string Text; public NotificationKind Kind; }
    public enum NotificationKind { Info, Success, Warning, Danger }
}
