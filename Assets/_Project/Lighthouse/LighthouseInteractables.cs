// Last Light — Lighthouse/LighthouseInteractables.cs
// Placeholder interactables for the lighthouse: refuel, repair generator, repair lens.
// Replace the prompt strings and effects when real items/inventory exist.

using UnityEngine;

namespace LastLight.Lighthouse
{
    public sealed class RefuelInteractable : Core.InteractableBase
    {
        [SerializeField] float fuelAmount = 35f;
        [SerializeField] LighthouseState lighthouse;

        void Awake()
        {
            if (lighthouse == null) lighthouse = FindFirstObjectByType<LighthouseState>();
        }

        public override void Interact(GameObject instigator)
        {
            if (lighthouse == null) return;
            lighthouse.Refuel(fuelAmount);
            // In a real inventory system this would consume a fuel can item.
            Debug.Log($"[Interact] Refueled lighthouse (+{fuelAmount}).");
        }
    }

    public sealed class RepairGeneratorInteractable : Core.InteractableBase
    {
        [SerializeField] float repairAmount = 30f;
        [SerializeField] LighthouseState lighthouse;

        void Awake()
        {
            if (lighthouse == null) lighthouse = FindFirstObjectByType<LighthouseState>();
        }

        public override bool CanInteract => base.CanInteract && lighthouse != null && lighthouse.GeneratorCondition01 < 0.95f;

        public override void Interact(GameObject instigator)
        {
            lighthouse.RepairGenerator(repairAmount);
            Debug.Log($"[Interact] Repaired generator (+{repairAmount}).");
        }
    }

    public sealed class RepairLensInteractable : Core.InteractableBase
    {
        [SerializeField] float repairAmount = 30f;
        [SerializeField] LighthouseState lighthouse;

        void Awake()
        {
            if (lighthouse == null) lighthouse = FindFirstObjectByType<LighthouseState>();
        }

        public override bool CanInteract => base.CanInteract && lighthouse != null && lighthouse.LensCondition01 < 0.95f;

        public override void Interact(GameObject instigator)
        {
            lighthouse.RepairLens(repairAmount);
            Debug.Log($"[Interact] Repaired lens (+{repairAmount}).");
        }
    }
}
