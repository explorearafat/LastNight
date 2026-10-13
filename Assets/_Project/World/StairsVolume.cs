// Last Light — World/StairsVolume.cs
// Small trigger placed on stair meshes. While the player is inside, PlayerVisuals
// lerps Stairs01 to 1 so the Animator cross-fades to a stair locomotion blend.

using UnityEngine;

namespace LastLight.World
{
    [RequireComponent(typeof(Collider))]
    public sealed class StairsVolume : MonoBehaviour
    {
        [SerializeField] float lerpSpeed = 6f;

        void Reset() => GetComponent<Collider>().isTrigger = true;

        void OnTriggerStay(Collider other)
        {
            if (other.TryGetComponent<Player.PlayerVisuals>(out var v))
                v.SetStairs01(Mathf.MoveTowards(v != null ? 1f : 0f, 1f, Time.deltaTime * lerpSpeed));
        }

        // We do the exit lerp in PlayerVisuals itself (it decays to 0 when not told otherwise).
    }
}
