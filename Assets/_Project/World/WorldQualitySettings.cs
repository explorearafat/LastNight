// Last Light — World/WorldQualitySettings.cs
// Applies the WorldConfig tier for the active QualityPreset to terrain/LOD/shadows.

using UnityEngine;
using LastLight.Core;

namespace LastLight.World
{
    public sealed class WorldQualitySettings : MonoBehaviour
    {
        [SerializeField] WorldConfig config;

        QualityPreset _lastPreset;

        void Start() => Apply();

        void Update()
        {
            // Re-apply on quality change (cheap — one branch per frame).
            var current = FindFirstObjectByType<QualitySettingsManager>() is { } qm ? qm.Current : QualityPreset.Medium;
            if (current != _lastPreset) Apply();
        }

        public void Apply()
        {
            var qm   = FindFirstObjectByType<QualitySettingsManager>();
            var tier = (config != null ? config : ScriptableObject.CreateInstance<WorldConfig>()).TierFor(qm != null ? qm.Current : QualityPreset.Medium);
            _lastPreset = qm != null ? qm.Current : QualityPreset.Medium;

            QualitySettings.lodBias          = tier.lodBias;
            QualitySettings.shadowDistance   = tier.shadowDistance;
            QualitySettings.pixelLightCount  = tier.pixelLightCount;
            QualitySettings.shadows          = tier.realtimeShadows ? ShadowQuality.HardOnly : ShadowQuality.Disable;

            // Terrain specifics — only if a Terrain exists (mesh mode skips terrain).
            var terrain = Terrain.activeTerrain;
            if (terrain != null)
            {
                terrain.heightmapPixelError   = tier.terrainPixelError;
                terrain.basemapDistance       = tier.terrainBasemapDistance;
                terrain.detailObjectDistance  = tier.detailDistance;
                terrain.treeDistance          = tier.treeDistance;
            }

            RenderSettings.fogDensity *= 1f; // base fog set by WeatherManager; tier scales via multiplier on next change

            Debug.Log($"[WorldQuality] Applied {tier.lodBias:0.0} lodBias, shadow {tier.shadowDistance}m, drawCall budget {tier.maxDrawCalls} — preset {_lastPreset}");
        }
    }
}
