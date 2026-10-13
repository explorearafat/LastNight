// Last Light — World/WorldConfig.cs
// Data-driven island + quality settings. A single ScriptableObject drives both the
// blockout layout and the mobile quality tiers so designers tune numbers without code.

using UnityEngine;
using LastLight.Core;

namespace LastLight.World
{
    [CreateAssetMenu(menuName = "LastLight/World Config", fileName = "WorldConfig")]
    public sealed class WorldConfig : ScriptableObject
    {
        [Header("Island")]
        [Tooltip("~1 km² — Unity Terrain size X,Z in metres. Mesh mode uses a flat plane of this size.")]
        public float islandSize = 1000f;

        [Tooltip("True = Unity Terrain with heightmap; false = single mesh plane (cheaper).")]
        public bool useTerrain = false;

        [Tooltip("If useTerrain, terrain height in metres.")]
        public float terrainHeight = 60f;

        [Header("World Layout (5 zones as blockout primitives — replaced by imported meshes)")]
        public ZoneDef[] zones = DefaultZones();

        [Header("Quality Tiers (applied by WorldQualitySettings)")]
        public WorldQualityTier low  = WorldQualityTier.DefaultLow();
        public WorldQualityTier med  = WorldQualityTier.DefaultMed();
        public WorldQualityTier high = WorldQualityTier.DefaultHigh();

        [Header("Gatherables")]
        public int gatherableCount = 18;

        [Header("NavMesh")]
        public bool bakeNavMesh = false; // leave off until you need pathfinding; simple paths are cheaper

        static ZoneDef[] DefaultZones() => new[]
        {
            new ZoneDef { id = "Lighthouse", center = new Vector2(  0,   0), radius = 90f,  height = 2f  },
            new ZoneDef { id = "Beach",      center = new Vector2(  0,-280), radius = 180f, height = 0.2f },
            new ZoneDef { id = "Forest",     center = new Vector2( 280, 60), radius = 220f, height = 1.5f },
            new ZoneDef { id = "Cannery",    center = new Vector2(-260,  40), radius = 120f, height = 0.6f },
            new ZoneDef { id = "CliffCave",  center = new Vector2(  80, 320), radius = 160f, height = 22f },
        };

        public WorldQualityTier TierFor(QualityPreset p) => p switch
        {
            QualityPreset.Low  => low,
            QualityPreset.High => high,
            _ => med,
        };
    }

    [System.Serializable]
    public struct ZoneDef
    {
        public string  id;
        public Vector2 center; // island-local XZ
        public float   radius;
        public float   height; // blockout height for this zone
    }

    [System.Serializable]
    public struct WorldQualityTier
    {
        [Header("LOD / Culling")]
        [Range(0.2f, 2f)] public float lodBias;
        public float shadowDistance;
        public float terrainPixelError;
        public float terrainBasemapDistance;
        public float detailDistance;
        public float treeDistance;
        public float fogDensityMultiplier;

        [Header("Shadows / Lights")]
        public bool  realtimeShadows;
        public int   pixelLightCount;
        public int   maxDrawCalls;

        public static WorldQualityTier DefaultLow()  => new() { lodBias = 0.6f, shadowDistance = 25f,  terrainPixelError = 120f, terrainBasemapDistance = 200f, detailDistance = 40f,  treeDistance = 350f,  fogDensityMultiplier = 1.3f, realtimeShadows = false, pixelLightCount = 1, maxDrawCalls = GameDefines.MaxDrawCallsLow  };
        public static WorldQualityTier DefaultMed()  => new() { lodBias = 1.0f, shadowDistance = 60f,  terrainPixelError = 60f,  terrainBasemapDistance = 500f, detailDistance = 80f,   treeDistance = 900f,  fogDensityMultiplier = 1.0f, realtimeShadows = true,  pixelLightCount = 2, maxDrawCalls = GameDefines.MaxDrawCallsMed  };
        public static WorldQualityTier DefaultHigh() => new() { lodBias = 1.4f, shadowDistance = 120f, terrainPixelError = 30f,  terrainBasemapDistance = 1000f,detailDistance = 150f,  treeDistance = 1500f, fogDensityMultiplier = 1.0f, realtimeShadows = true,  pixelLightCount = 3, maxDrawCalls = GameDefines.MaxDrawCallsHigh };
    }
}
