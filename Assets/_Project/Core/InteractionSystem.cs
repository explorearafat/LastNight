// Last Light — Core/InteractionSystem.cs
// Minimal interactable contract. The player raycasts / sphere-checks for IInteractable
// and TouchInput shows the button. Keep it interface-only so world objects don't
// need to inherit from a base class.

using UnityEngine;

namespace LastLight.Core
{
    /// <summary>Anything the player can interact with from nearby.</summary>
    public interface IInteractable
    {
        /// <summary>Short label shown on the interact button, e.g. "Refuel generator".</summary>
        string Prompt { get; }

        /// <summary>True if interaction is currently allowed (in range, requirement met, …).</summary>
        bool CanInteract { get; }

        /// <summary>Called when the player confirms interaction.</summary>
        void Interact(GameObject instigator);

        /// <summary>World position used for distance checks (defaults to transform.position).</summary>
        Vector3 InteractionPoint { get; }
    }

    /// <summary>Convenience base for quick placeholder interactables.</summary>
    public abstract class InteractableBase : MonoBehaviour, IInteractable
    {
        [SerializeField] string prompt = "Interact";
        [SerializeField] float  maxDistance = 2f;

        public string Prompt => prompt;
        public Vector3 InteractionPoint => transform.position;
        public float MaxDistance => maxDistance;

        public virtual bool CanInteract => Vector3.Distance(
            FindFirstObjectByType<LastLight.Player.PlayerController>()?.transform.position ?? transform.position,
            InteractionPoint) <= maxDistance;

        public abstract void Interact(GameObject instigator);
    }
}
