// Last Light — Core/EventBus.cs
// Tiny allocation-free event bus for cross-system communication without hard references.

using System;
using System.Collections.Generic;

namespace LastLight.Core
{
    /// <summary>Simple typed pub/sub. Call <see cref="Clear"/> on scene unload in tests.</summary>
    public static class EventBus
    {
        static readonly Dictionary<Type, List<Delegate>> _map = new();

        public static void Subscribe<T>(Action<T> handler)
        {
            var t = typeof(T);
            if (!_map.TryGetValue(t, out var list)) _map[t] = list = new List<Delegate>();
            list.Add(handler);
        }

        public static void Unsubscribe<T>(Action<T> handler)
        {
            if (_map.TryGetValue(typeof(T), out var list)) list.Remove(handler);
        }

        public static void Publish<T>(T evt)
        {
            if (!_map.TryGetValue(typeof(T), out var list)) return;
            // Snapshot to allow handlers to subscribe/unsubscribe during dispatch.
            var snapshot = list.ToArray();
            foreach (var d in snapshot) ((Action<T>)d).Invoke(evt);
        }

        public static void Clear() => _map.Clear();
    }

    // --- Event payloads ---

    /// <summary>Fired when fuel, damage or light state changes.</summary>
    public readonly struct LighthouseChangedEvent
    {
        public readonly float Fuel01;
        public readonly float StructuralDamage01; // 0 = pristine, 1 = ruined
        public readonly bool  LightOn;
        public LighthouseChangedEvent(float fuel01, float damage01, bool lightOn)
        { Fuel01 = fuel01; StructuralDamage01 = damage01; LightOn = lightOn; }
    }

    /// <summary>Fired when weather transitions.</summary>
    public readonly struct WeatherChangedEvent
    {
        public readonly WeatherState From, To;
        public readonly float StormIntensity01;
        public WeatherChangedEvent(WeatherState from, WeatherState to, float intensity)
        { From = from; To = to; StormIntensity01 = intensity; }
    }

    /// <summary>Fired when day phase rolls over.</summary>
    public readonly struct DayPhaseChangedEvent
    {
        public readonly DayPhase NewPhase;
        public readonly float    NormalizedTime01; // 0 = midnight, 0.5 = noon
        public DayPhaseChangedEvent(DayPhase phase, float t) { NewPhase = phase; NormalizedTime01 = t; }
    }

    /// <summary>Fired from PlayerStats when a vital crosses a threshold the presentation layer cares about.</summary>
    public readonly struct PlayerVitalEvent
    {
        public readonly string Vital; // "Health" | "Stamina" | "Hunger" | "Cold"
        public readonly float  Value01;
        public PlayerVitalEvent(string vital, float v) { Vital = vital; Value01 = v; }
    }
}
