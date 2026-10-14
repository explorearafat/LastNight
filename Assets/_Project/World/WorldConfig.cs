// Last Light — World/WorldConfig.cs — Day 5 archipelago layout.
// Lighthouse on a small islet; main island holds the ruins and resources.

using UnityEngine;
using LastLight.Core;

namespace LastLight.World
{
    [CreateAssetMenu(menuName = "LastLight/World Config", fileName = "WorldConfig")]
    public sealed class WorldConfig : ScriptableObject
    {
        [Header("Island")]
        [Tooltip("Main island X,Z in metres. Single mesh plane unless useTerrain.")]
        public float islandSize = 750f;

        [Tooltip("Small lighthouse islet — separate landmass west of the main island.")]
        public float isletSize = 160f;

        [Tooltip("Gap of deep shark water between islet and main island (metres).")]
        public float channelWidth = 260f;

        [Tooltip("True = Unity Terrain with heightmap; false = mesh plane (cheaper).")]
        public bool useTerrain = false;

        [Tooltip("If useTerrain, terrain height in metres.")]
        public float terrainHeight = 55f;

        [Header("World Layout (islet + main island zones — blockout primitives)")]
        public ZoneDef[] zones = DefaultZones();

        [Header("Islet (lighthouse base)")]
        public Vector2 isletCenter = new Vector2(-520f, 0f);
        public float   isletRadius = 75f;

        [Header("Dock positions (world XZ)")]
        public Vector2 mainIslandDock  = new Vector2(-210f, -6f);
        public Vector2 isletDock       = new Vector2(-445f, -6f);

        [Header("Quality Tiers (applied by WorldQualitySettings)")]
        public WorldQualityTier low  = WorldQualityTier.DefaultLow();
        public WorldQualityTier med  = WorldQualityTier.DefaultMed();
        public WorldQualityTier high = WorldQualityTier.DefaultHigh();

        [Header("Gatherables (now only on main island)")]
        public int gatherableCount = 18;

        [Header("NavMesh")]
        public bool bakeNavMesh = false;

        static ZoneDef[] DefaultZones() => new[]
        {
            // Islet — lighthouse
            new ZoneDef { id = "LighthouseIslet", center = new Vector2(-520f, 0f), radius = 75f, height = 2.5f  },
            // Main island — sandy beach shoreline + interior
            new ZoneDef { id = "Beach",       center = new Vector2(-40f,-260f), radius = 190f, height = 0.2f },
            new ZoneDef { id = "Forest",      center = new Vector2( 160f, 40f), radius = 210f, height = 1.5f },
            new ZoneDef { id = "Castle",      center = new Vector2(  40f, 140f), radius = 130f, height = 8f   },
            new ZoneDef { id = "Bunker",      center = new Vector2(-140f,  90f), radius = 110f, height = 1.2f },
            new ZoneDef { id = "CanneryPier", center = new Vector2(-180f,-140f), radius = 120f, height = 0.6f },
            new ZoneDef { id = "MountainCave",center = new Vector2( 110f, 280f), radius = 155f, height = 24f  },
            new ZoneDef { id = "Ruins",       center = new Vector2( -20f, -40f), radius = 100f, height = 1.0f },
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
        public Vector2 center;
        public float   radius;
        public float   height;
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
