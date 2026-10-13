// Last Light — Core/InteractionSystem.cs
// Minimal interactable contract + helpers.
// The player sphere-scans for IInteractable; TouchInput shows a single context button.
// Keep it interface-only so world objects don't need a common base beyond this file.

using UnityEngine;

namespace LastLight.Core
{
    /// <summary>Anything the player can interact with from nearby.</summary>
    public interface IInteractable
    {
        string  Prompt          { get; }
        bool    CanInteract     { get; }
        void    Interact(GameObject instigator);
        Vector3 InteractionPoint { get; }
    }

    /// <summary>Convenience base for quick placeholder interactables.</summary>
    public abstract class InteractableBase : MonoBehaviour, IInteractable
    {
        [SerializeField] string prompt = "Interact";
        [SerializeField] float  maxDistance = 2f;

        public string  Prompt          => prompt;
        public Vector3 InteractionPoint => transform.position;
        public float   MaxDistance     => maxDistance;

        public virtual bool CanInteract => Vector3.Distance(
            FindFirstObjectByType<LastLight.Player.PlayerController>()?.transform.position ?? transform.position,
            InteractionPoint) <= maxDistance;

        public abstract void Interact(GameObject instigator);

        protected void SetPrompt(string p) => prompt = p;
    }

    /// <summary>Finds the best IInteractable near a point (single OverlapSphere).</summary>
    public static class InteractionScanner
    {
        static readonly Collider[] _hits = new Collider[16];

        public static IInteractable FindBest(Vector3 origin, float radius, LayerMask mask)
        {
            int count = Physics.OverlapSphereNonAlloc(origin, radius, _hits, mask, QueryTriggerInteraction.Collide);
            IInteractable best = null;
            float bestDist = float.MaxValue;
            for (int i = 0; i < count; i++)
            {
                var ia = _hits[i].GetComponentInParent<IInteractable>();
                if (ia == null || !ia.CanInteract) continue;
                float d = Vector3.Distance(origin, ia.InteractionPoint);
                if (d < bestDist) { bestDist = d; best = ia; }
            }
            return best;
        }
    }
}
