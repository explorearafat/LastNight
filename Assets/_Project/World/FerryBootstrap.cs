// Last Light — World/FerryBootstrap.cs
// Boat ferry gameplay: require ferrying resources (fuel/food/repair etc.) via BoatInventory
// before dusk. The lighthouse side consumes from the islet; the main island is where
// Gatherables live. Lightweight — sits alongside Inventory / SaveManager.

using UnityEngine;
using LastLight.Core;
using LastLight.Systems;

namespace LastLight.World
{
    public sealed class FerryBootstrap : MonoBehaviour
    {
        [Header("Ferry rule")]
        [Tooltip("How much fuel/food/repair must be ferried to the islet each day to keep the light healthy")]
        [SerializeField] float requiredFerryKgPerDay = 6f;

        [Header("Refs (auto-found)")]
        [SerializeField] BoatInventory boatInv;
        [SerializeField] Survival.Inventory playerInv;

        void Awake()
        {
            Resolve();
        }

        void OnEnable()
        {
            EventBus.Subscribe<DayPhaseChangedEvent>(OnPhase);
            EventBus.Subscribe<BoatCrossingEvent>(OnCrossing);
        }
        void OnDisable()
        {
            EventBus.Unsubscribe<DayPhaseChangedEvent>(OnPhase);
            EventBus.Unsubscribe<BoatCrossingEvent>(OnCrossing);
        }

        void Resolve()
        {
            if (boatInv == null) boatInv = FindFirstObjectByType<BoatInventory>();
            if (playerInv==null) playerInv= FindFirstObjectByType<Survival.Inventory>();
        }

        void OnPhase(DayPhaseChangedEvent e)
        {
            Resolve();
            if (e.NewPhase == DayPhase.Dusk)
            {
                // At dusk, tally what was ferried today — if too little, flag mercy hint
                float ferried = 0f;
                if (boatInv != null)
                {
                    foreach (ItemType t in System.Enum.GetValues(typeof(ItemType)))
                        ferried += boatInv.GetCount(t) * WeightOf(t);
                }
                if (ferried < requiredFerryKgPerDay * 0.35f)
                {
                    Debug.Log($"[Ferry] Light ferry today: {ferried:0.0}kg — lighthouse may struggle tonight.");
                }
            }
        }

        void OnCrossing(BoatCrossingEvent e)
        {
            // Auto-unload to player when landing on the islet (so refuel works without extra UI)
            if (e.Side != "Islet") return;
            if (boatInv == null || playerInv == null) { Resolve(); if (boatInv==null||playerInv==null) return; }
            foreach (ItemType t in System.Enum.GetValues(typeof(ItemType)))
            {
                int have = boatInv.GetCount(t);
                if (have > 0) boatInv.UnloadToPlayer(t, have);
            }
        }

        static float WeightOf(ItemType t) => t switch
        {
            ItemType.Fuel => GameDefines.ItemWeightFuel,
            ItemType.Wood => GameDefines.ItemWeightWood,
            ItemType.Food => GameDefines.ItemWeightFood,
            ItemType.RepairParts => GameDefines.ItemWeightRepairParts,
            _ => 1f
        };
    }
}
