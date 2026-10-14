// Last Light — Systems/BoatInventory.cs
// Boat ferry limit: only N kg can be on the boat at once. Resources on the main island
// must be ferried before dusk. Works alongside (not replacing) the player Inventory.

using UnityEngine;
using System.Collections.Generic;
using LastLight.Core;

namespace LastLight.Systems
{
    public sealed class BoatInventory : MonoBehaviour
    {
        [SerializeField] float maxWeight = 18f; // slightly less than player carry so the ferry matters

        readonly Dictionary<ItemType,int> _counts = new();
        float _weight;

        public float MaxWeight => maxWeight;
        public float CurrentWeight => _weight;
        public float Weight01 => maxWeight>0? Mathf.Clamp01(_weight/maxWeight):0f;
        public int TotalItems { get; private set; }

        static float W(ItemType t)=> t switch{ ItemType.Fuel=>GameDefines.ItemWeightFuel, ItemType.Wood=>GameDefines.ItemWeightWood, ItemType.Food=>GameDefines.ItemWeightFood, ItemType.RepairParts=>GameDefines.ItemWeightRepairParts, _=>1f };

        // Move from player inventory to boat
        public int StashFromPlayer(ItemType t, int count)
        {
            if(count<=0) return 0;
            var inv = FindFirstObjectByType<Survival.Inventory>();
            if(inv==null) return 0;
            int have = inv.GetCount(t);
            int want = Mathf.Min(count, have);
            int canFit = Mathf.FloorToInt((maxWeight - _weight)/W(t));
            int mv = Mathf.Min(want, canFit);
            if(mv<=0) return 0;
            inv.TryRemove(t, mv);
            _counts[t]= _counts.TryGetValue(t, out int v)? v+mv : mv;
            _weight+= W(t)*mv; TotalItems+=mv;
            EventBus.Publish(new BoatInventoryChangedEvent(Weight01));
            return mv;
        }

        // Unload at the islet (to lighthouse stash — just give back to player or a LighthouseStash)
        public int UnloadToPlayer(ItemType t, int count)
        {
            if(!_counts.TryGetValue(t,out int have)||have<=0) return 0;
            int mv = Mathf.Min(count, have);
            var inv = FindFirstObjectByType<Survival.Inventory>();
            if(inv!=null)
            {
                int added = inv.TryAdd(t, mv); // respects player weight cap
                if(added<=0) return 0;
                mv = added;
            }
            int left = have - mv;
            if(left<=0) _counts.Remove(t); else _counts[left==have? t:t]=left; // keep key
            if(left<=0) _counts.Remove(t); else _counts[t]=left;
            _weight = Mathf.Max(0, _weight - W(t)*mv);
            TotalItems = Mathf.Max(0, TotalItems - mv);
            EventBus.Publish(new BoatInventoryChangedEvent(Weight01));
            return mv;
        }

        public int GetCount(ItemType t)=> _counts.TryGetValue(t,out int v)? v:0;
        public bool Has(ItemType t,int n=1)=> GetCount(t)>=n;

        public Dictionary<ItemType,int> Snapshot()=> new(_counts);

        public void Clear(){ _counts.Clear(); _weight=0; TotalItems=0; EventBus.Publish(new BoatInventoryChangedEvent(0)); }

        // Sinks with boat? we keep it — mercy will clear at dawn if needed
    }

    public readonly struct BoatInventoryChangedEvent { public readonly float Weight01; public BoatInventoryChangedEvent(float w){ Weight01=w; } }
}
