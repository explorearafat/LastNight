// Last Light — World/ZoneVolume.cs
// Explicit trigger volumes for ShelterZone detection.
// Place these in your real level meshes to give AdaptiveStormDirector a precise read
// instead of the distance heuristic it falls back to.

using UnityEngine;
using LastLight.Core;

namespace LastLight.World
{
    [RequireComponent(typeof(Collider))]
    public sealed class ZoneVolume : MonoBehaviour
    {
        [SerializeField] ShelterZone zone = ShelterZone.LighthouseInterior;
        public ShelterZone Zone => zone;

        void Reset()
        {
            var col = GetComponent<Collider>();
            col.isTrigger = true;
        }
    }

    /// <summary>Helper to resolve the current ShelterZone from the player's position.</summary>
    public static class ZoneResolver
    {
        // NonAlloc cache for zone volume queries.
        static readonly Collider[] _hits = new Collider[8];

        public static ShelterZone FromPosition(Vector3 pos)
        {
            int count = Physics.OverlapSphereNonAlloc(pos, 0.6f, _hits, ~0, QueryTriggerInteraction.Collide);
            for (int i = 0; i < count; i++)
            {
                var zv = _hits[i].GetComponent<ZoneVolume>();
                if (zv != null) return zv.Zone;
            }
            // Fallback heuristic (same as AdaptiveStormDirector's old ResolveZone).
            float distFromTower = new Vector2(pos.x, pos.z).magnitude;
            if (distFromTower < 4.5f) return ShelterZone.LighthouseInterior;
            if (pos.y > 3f) return ShelterZone.LensRoom;
            if (distFromTower < 7f) return ShelterZone.GeneratorRoom;
            if (pos.z > 10f) return ShelterZone.Cliffs;
            if (pos.x < -8f) return ShelterZone.Forest;
            if (pos.z < -6f) return ShelterZone.Beach;
            if (pos.x < -180f) return ShelterZone.Cannery;
            if (pos.y > 18f) return ShelterZone.Cave;
            return ShelterZone.None;
        }
    }
}
