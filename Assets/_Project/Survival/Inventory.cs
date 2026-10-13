// Last Light — Survival/Inventory.cs
// World-based inventory — no big menu. Four item types, weight affects movement.
// Single-slot HUD hint (which item to drop/use) is driven by UI via events.

using UnityEngine;
using LastLight.Core;
using System.Collections.Generic;

namespace LastLight.Survival
{
    public sealed class Inventory : MonoBehaviour
    {
        [Header("Capacity")]
        [SerializeField] float maxWeight = GameDefines.InventoryMaxWeight;

        // Internal counts per type.
        readonly Dictionary<ItemType, int> _counts = new();

        public float CurrentWeight { get; private set; }
        public float Weight01 => maxWeight > 0f ? Mathf.Clamp01(CurrentWeight / maxWeight) : 0f;
        public int TotalItems { get; private set; }

        // Weight per item type — mirrors GameDefines.
        static float WeightFor(ItemType t) => t switch
        {
            ItemType.Fuel        => GameDefines.ItemWeightFuel,
            ItemType.Wood        => GameDefines.ItemWeightWood,
            ItemType.Food        => GameDefines.ItemWeightFood,
            ItemType.RepairParts => GameDefines.ItemWeightRepairParts,
            _ => 1f
        };

        public int GetCount(ItemType t) => _counts.TryGetValue(t, out int c) ? c : 0;

        /// <summary>Try to add items; returns how many actually fit.</summary>
        public int TryAdd(ItemType type, int count)
        {
            float w = WeightFor(type);
            int canFit = Mathf.FloorToInt((maxWeight - CurrentWeight) / w);
            int added  = Mathf.Clamp(count, 0, canFit);
            if (added <= 0) return 0;

            _counts[type] = GetCount(type) + added;
            CurrentWeight += w * added;
            TotalItems    += added;
            EventBus.Publish(new InventoryChangedEvent(Weight01));
            EventBus.Publish(new ItemPickedEvent(type, added));
            return added;
        }

        /// <summary>Remove items; returns how many actually removed.</summary>
        public int TryRemove(ItemType type, int count)
        {
            int have = GetCount(type);
            int removed = Mathf.Clamp(count, 0, have);
            if (removed <= 0) return 0;

            int left = have - removed;
            if (left == 0) _counts.Remove(type);
            else _counts[type] = left;

            CurrentWeight = Mathf.Max(0f, CurrentWeight - WeightFor(type) * removed);
            TotalItems    = Mathf.Max(0, TotalItems - removed);
            EventBus.Publish(new InventoryChangedEvent(Weight01));
            return removed;
        }

        public bool Has(ItemType type, int count = 1) => GetCount(type) >= count;

        public void Clear()
        {
            _counts.Clear();
            CurrentWeight = 0f;
            TotalItems = 0;
            EventBus.Publish(new InventoryChangedEvent(0f));
        }

        // --- Serialization (called by SaveManager) ---
        public Dictionary<ItemType, int> Snapshot() => new(_counts);

        public void Restore(Dictionary<ItemType, int> data)
        {
            _counts.Clear();
            CurrentWeight = 0f; TotalItems = 0;
            if (data == null) return;
            foreach (var kv in data)
            {
                _counts[kv.Key] = kv.Value;
                CurrentWeight += WeightFor(kv.Key) * kv.Value;
                TotalItems    += kv.Value;
            }
            EventBus.Publish(new InventoryChangedEvent(Weight01));
        }
    }
}
