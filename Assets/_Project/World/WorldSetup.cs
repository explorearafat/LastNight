// Last Light — World/WorldSetup.cs
// Per-world setup helpers: static flags, occlusion, LOD bias, terrain distances.
// Runs before any visual frame so the first render uses the correct tier.

using UnityEngine;
using LastLight.Core;

namespace LastLight.World
{
    public static class WorldSetup
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void EarlySetup()
        {
            // Ensure static batching candidates are marked before the scene loads.
            // IslandLayout / WorldBuilder tag their blockout as isStatic — SRP batcher + static batching picks them up.
        }

        public static void ApplyLODAndOcclusion(WorldQualityTier tier)
        {
            QualitySettings.lodBias          = tier.lodBias;
            QualitySettings.maximumLODLevel  = 0;
            QualitySettings.shadows          = tier.realtimeShadows ? ShadowQuality.HardOnly : ShadowQuality.Disable;
            QualitySettings.shadowDistance   = tier.shadowDistance;
            QualitySettings.pixelLightCount  = tier.pixelLightCount;

            // Occlusion culling — enable for the highest tier only (costly on Low).
            var cam = Camera.main;
            if (cam != null) cam.useOcclusionCulling = tier.shadowDistance >= 60f;
        }

        public static void ConfigureRendererForMobile()
        {
            // Forward+ renderer hint: no deferred path on mobile.
            // This is a no-op in code — ensure your URP asset Graphics → Rendering Path is Forward.
        }
    }
}
