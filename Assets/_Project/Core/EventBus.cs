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
            var snapshot = list.ToArray();
            foreach (var d in snapshot) ((Action<T>)d).Invoke(evt);
        }

        public static void Clear() => _map.Clear();
    }

    // --- Event payloads ---

    public readonly struct LighthouseChangedEvent
    {
        public readonly float Fuel01, StructuralDamage01;
        public readonly bool  LightOn;
        public LighthouseChangedEvent(float fuel01, float damage01, bool lightOn)
        { Fuel01 = fuel01; StructuralDamage01 = damage01; LightOn = lightOn; }
    }

    public readonly struct WeatherChangedEvent
    {
        public readonly WeatherState From, To;
        public readonly float StormIntensity01;
        public WeatherChangedEvent(WeatherState from, WeatherState to, float intensity)
        { From = from; To = to; StormIntensity01 = intensity; }
    }

    public readonly struct DayPhaseChangedEvent
    {
        public readonly DayPhase NewPhase;
        public readonly float    NormalizedTime01;
        public DayPhaseChangedEvent(DayPhase phase, float t) { NewPhase = phase; NormalizedTime01 = t; }
    }

    public readonly struct PlayerVitalEvent
    {
        public readonly string Vital;
        public readonly float  Value01;
        public PlayerVitalEvent(string vital, float v) { Vital = vital; Value01 = v; }
    }

    public readonly struct ItemPickedEvent
    {
        public readonly ItemType Type;
        public readonly int Count;
        public ItemPickedEvent(ItemType type, int count) { Type = type; Count = count; }
    }

    public readonly struct InventoryChangedEvent
    {
        public readonly float Weight01;
        public InventoryChangedEvent(float w01) { Weight01 = w01; }
    }

    public readonly struct LoopPhaseChangedEvent
    {
        public readonly DayPhase From, To;
        public LoopPhaseChangedEvent(DayPhase from, DayPhase to) { From = from; To = to; }
    }

    public readonly struct ShipWreckEvent
    {
        public readonly int DayNumber;
        public ShipWreckEvent(int day) { DayNumber = day; }
    }

    public readonly struct DamageReportEvent
    {
        public readonly float NewStructuralDamage01;
        public readonly bool  HadLightFailure;
        public readonly bool  HadWaterLeak;
        public readonly bool  HadLensFailure;
        public DamageReportEvent(float dmg, bool light, bool leak, bool lens)
        { NewStructuralDamage01 = dmg; HadLightFailure = light; HadWaterLeak = leak; HadLensFailure = lens; }
    }

    public readonly struct StormTargetChosenEvent
    {
        public readonly string TargetName;
        public readonly float  Weight;
        public StormTargetChosenEvent(string name, float weight) { TargetName = name; Weight = weight; }
    }
}
