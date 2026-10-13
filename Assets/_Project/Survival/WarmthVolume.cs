// Last Light — Survival/WarmthVolume.cs
// Trigger volume that marks the player as "near warmth" (inside lighthouse, by a fire, etc.)
// Add this to any trigger collider and it will call PlayerStats.Add/RemoveWarmthSource.

using UnityEngine;

namespace LastLight.Survival
{
    [RequireComponent(typeof(Collider))]
    public sealed class WarmthVolume : MonoBehaviour
    {
        void Reset()
        {
            var col = GetComponent<Collider>();
            col.isTrigger = true;
        }

        void OnTriggerEnter(Collider other)
        {
            if (other.TryGetComponent<PlayerStats>(out var stats)) stats.AddWarmthSource();
        }

        void OnTriggerExit(Collider other)
        {
            if (other.TryGetComponent<PlayerStats>(out var stats)) stats.RemoveWarmthSource();
        }
    }
}
