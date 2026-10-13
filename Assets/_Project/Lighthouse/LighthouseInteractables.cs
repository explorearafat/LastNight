// Last Light — Lighthouse/LighthouseInteractables.cs
// Concrete interactables for the lighthouse. Each is a trigger-sized MonoBehaviour
// placed on a child of the lighthouse prefab (generator console, lens stair, window frame, …).

using UnityEngine;
using LastLight.Core;
using LastLight.Survival;

namespace LastLight.Lighthouse
{
    // ── Pick up a loose item lying on the floor / shelf ──

    public sealed class PickupInteractable : InteractableBase
    {
        [SerializeField] ItemType itemType = ItemType.Fuel;
        [SerializeField] int      amount   = 1;
        [SerializeField] GameObject visualToHide;

        void Reset() => SetPrompt(itemType == ItemType.Fuel ? "Pick up fuel" : "Pick up item");

        public override void Interact(GameObject instigator)
        {
            var inv = instigator.GetComponentInParent<Inventory>();
            if (inv == null) inv = FindFirstObjectByType<Inventory>();
            if (inv == null) return;

            int added = inv.TryAdd(itemType, amount);
            if (added == 0) { Debug.Log("[Pickup] Inventory full."); return; }
            FindFirstObjectByType<World.AdaptiveStormDirector>()?.RecordGather(itemType);

            if (visualToHide != null) visualToHide.SetActive(false);
            else gameObject.SetActive(false);
            Debug.Log($"[Pickup] {itemType} x{added}");
        }
    }

    // ── Refuel the generator (consumes Fuel from inventory) ──

    public sealed class RefuelInteractable : InteractableBase
    {
        [SerializeField] float fuelPerUnit = 28f;
        [SerializeField] int   fuelCost    = 1;

        void Reset() => SetPrompt("Refuel generator");

        public override bool CanInteract => base.CanInteract
            && FindFirstObjectByType<LighthouseState>() is { } lh && lh.Fuel01 < 0.98f
            && FindFirstObjectByType<Inventory>() is { } inv && inv.Has(ItemType.Fuel, fuelCost);

        public override void Interact(GameObject instigator)
        {
            var inv = FindFirstObjectByType<Inventory>();
            var lh  = FindFirstObjectByType<LighthouseState>();
            if (inv == null || lh == null) return;
            if (inv.TryRemove(ItemType.Fuel, fuelCost) == 0) return;
            lh.Refuel(fuelPerUnit);
            instigator.GetComponentInParent<Player.PlayerVisuals>()?.TriggerInteract();
            FindFirstObjectByType<World.AdaptiveStormDirector>()?.RecordUse(ShelterZone.GeneratorRoom);
        }
    }

    // ── Repair lens (consumes RepairParts) ──

    public sealed class RepairLensInteractable : InteractableBase
    {
        [SerializeField] float repairAmount = 30f;
        [SerializeField] int   partsCost    = 1;

        void Reset() => SetPrompt("Repair lens");

        public override bool CanInteract => base.CanInteract
            && FindFirstObjectByType<LighthouseState>() is { } lh && lh.LensCondition01 < 0.96f
            && FindFirstObjectByType<Inventory>() is { } inv && inv.Has(ItemType.RepairParts, partsCost);

        public override void Interact(GameObject instigator)
        {
            var inv = FindFirstObjectByType<Inventory>();
            var lh  = FindFirstObjectByType<LighthouseState>();
            if (inv == null || lh == null) return;
            if (inv.TryRemove(ItemType.RepairParts, partsCost) == 0) return;
            lh.RepairLens(repairAmount);
            instigator.GetComponentInParent<Player.PlayerVisuals>()?.TriggerInteract();
            FindFirstObjectByType<World.AdaptiveStormDirector>()?.RecordUse(ShelterZone.LensRoom);
        }
    }

    // ── Repair generator health (consumes RepairParts) ──

    public sealed class RepairGeneratorInteractable : InteractableBase
    {
        [SerializeField] float repairAmount = 30f;
        [SerializeField] int   partsCost    = 1;

        void Reset() => SetPrompt("Repair generator");

        public override bool CanInteract => base.CanInteract
            && FindFirstObjectByType<LighthouseState>() is { } lh && lh.GeneratorCondition01 < 0.96f
            && FindFirstObjectByType<Inventory>() is { } inv && inv.Has(ItemType.RepairParts, partsCost);

        public override void Interact(GameObject instigator)
        {
            var inv = FindFirstObjectByType<Inventory>();
            var lh  = FindFirstObjectByType<LighthouseState>();
            if (inv == null || lh == null) return;
            if (inv.TryRemove(ItemType.RepairParts, partsCost) == 0) return;
            lh.RepairGenerator(repairAmount);
            instigator.GetComponentInParent<Player.PlayerVisuals>()?.TriggerInteract();
            FindFirstObjectByType<World.AdaptiveStormDirector>()?.RecordUse(ShelterZone.GeneratorRoom);
        }
    }

    // ── Board up a window (consumes Wood) — blocks a storm's "window" attack ──

    public sealed class BoardWindowInteractable : InteractableBase
    {
        [SerializeField] int       woodCost  = 2;
        [SerializeField] GameObject boardsVisual; // enable to show boards
        [SerializeField] string    windowId = "W1";

        bool _boarded;

        void Reset() => SetPrompt("Board up window");

        public override bool CanInteract => base.CanInteract && !_boarded
            && FindFirstObjectByType<Inventory>() is { } inv && inv.Has(ItemType.Wood, woodCost);

        public override void Interact(GameObject instigator)
        {
            if (_boarded) return;
            var inv = FindFirstObjectByType<Inventory>();
            if (inv == null || inv.TryRemove(ItemType.Wood, woodCost) == 0) return;
            _boarded = true;
            if (boardsVisual != null) boardsVisual.SetActive(true);
            instigator.GetComponentInParent<Player.PlayerVisuals>()?.TriggerInteract();
            FindFirstObjectByType<World.DamageableStructure>()?.SetBoarded(windowId, true);
            FindFirstObjectByType<World.AdaptiveStormDirector>()?.RecordUse(ShelterZone.LighthouseInterior);
            Debug.Log($"[Board] Window {windowId} boarded.");
        }

        public bool IsBoarded => _boarded;
        public string WindowId => windowId;
        public void RestoreBoarded(bool v)
        {
            _boarded = v;
            if (boardsVisual != null) boardsVisual.SetActive(v);
        }
    }

    // ── Eat food (consumes Food from inventory) ──

    public sealed class EatFoodInteractable : InteractableBase
    {
        [SerializeField] float hungerRestore = 28f;
        void Reset() => SetPrompt("Eat");

        public override bool CanInteract => base.CanInteract
            && FindFirstObjectByType<Inventory>() is { } inv && inv.Has(ItemType.Food, 1);

        public override void Interact(GameObject instigator)
        {
            var inv = FindFirstObjectByType<Inventory>();
            if (inv == null || inv.TryRemove(ItemType.Food, 1) == 0) return;
            var stats = FindFirstObjectByType<PlayerStats>();
            stats?.RestoreHunger(hungerRestore);
            instigator.GetComponentInParent<Player.PlayerVisuals>()?.TriggerInteract();
        }
    }
}
