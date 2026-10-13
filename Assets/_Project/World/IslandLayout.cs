// Last Light — World/IslandLayout.cs
// Builds the ~1 km² island with 5 zones as blockout primitives.
// If meshes are imported into Assets/Environment/, they replace the blockout at the same positions.
// No Terrain by default (single mesh plane is cheaper on mobile) — flip WorldConfig.useTerrain to opt in.

using UnityEngine;
using System.Collections.Generic;

namespace LastLight.World
{
    public sealed class IslandLayout : MonoBehaviour
    {
        [SerializeField] WorldConfig config;

        [Header("Imported replacements (optional — assign meshes from Assets/Environment/)")]
        [Tooltip("If assigned, these replace blockout at matching zone ids. Key = zone id.")]
        [SerializeField] List<ZoneReplacement> replacements = new();

        [Header("Materials (shared — enables SRP batching)")]
        [SerializeField] Material groundMaterial;
        [SerializeField] Material rockMaterial;
        [SerializeField] Material foliageMaterial;

        // Keep roots so we can destroy on scene reload without duplicating.
        readonly List<GameObject> _spawned = new();
        GameObject _ground;

        void Awake() => BuildIfNeeded();

        void BuildIfNeeded()
        {
            if (GameObject.Find("Island_Ground") != null) return;
            Build();
        }

        void Build()
        {
            var cfg = config != null ? config : ScriptableObject.CreateInstance<WorldConfig>();
            float size = cfg.islandSize;

            // Ground plane (cheap mobile ground — Terrain is opt-in via config).
            _ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            _ground.name = "Island_Ground";
            _ground.transform.position = Vector3.zero;
            _ground.transform.localScale = new Vector3(size / 10f, 1f, size / 10f);
            if (groundMaterial != null) _ground.GetComponent<Renderer>().sharedMaterial = groundMaterial;

            // If Terrain requested, create one instead of the plane mesh.
            if (cfg.useTerrain)
                BuildTerrain(cfg);

            // 5 zones as blockout
            for (int i = 0; i < cfg.zones.Length; i++)
            {
                var zone = cfg.zones[i];
                var repl = replacements.Find(r => r.zoneId == zone.id);
                if (repl != null && repl.prefab != null)
                {
                    var go = Instantiate(repl.prefab, new Vector3(zone.center.x, 0, zone.center.y), Quaternion.identity, transform);
                    go.name = $"Zone_{zone.id}";
                    _spawned.Add(go);
                }
                else BuildBlockoutZone(zone, i);
            }

            // NavMesh: only if requested (simple paths are cheaper).
            if (cfg.bakeNavMesh) TryBakeNavMesh();
        }

        void BuildBlockoutZone(ZoneDef zone, int index)
        {
            var go = new GameObject($"Zone_{zone.id}");
            go.transform.position = new Vector3(zone.center.x, 0, zone.center.y);
            _spawned.Add(go);

            // Raised base to hint elevation differences (beach flat, cliff high).
            var baseCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            baseCube.name = $"Zone_{zone.id}_Base";
            baseCube.transform.SetParent(go.transform, false);
            baseCube.transform.localPosition = new Vector3(0, zone.height * 0.5f, 0);
            baseCube.transform.localScale    = new Vector3(zone.radius * 0.55f, Mathf.Max(0.2f, zone.height), zone.radius * 0.55f);
            if (groundMaterial != null) baseCube.GetComponent<Renderer>().sharedMaterial = groundMaterial;
            baseCube.isStatic = true;

            // A few landmark primitives per zone so each zone is recognisable before art.
            System.Random rng = new(index * 7919 + 104729);
            int count = zone.id switch { "Lighthouse" => 4, "Forest" => 18, "Beach" => 10, "Cannery" => 6, "CliffCave" => 8, _ => 6 };
            for (int j = 0; j < count; j++)
            {
                float a = (float)rng.NextDouble() * Mathf.PI * 2f;
                float r = (float)rng.NextDouble() * zone.radius * 0.42f;
                Vector3 pos = new Vector3(Mathf.Cos(a) * r, zone.height + 0.3f, Mathf.Sin(a) * r);
                var prim = zone.id == "Forest" ? PrimitiveType.Cylinder : PrimitiveType.Cube;
                var obj = GameObject.CreatePrimitive(prim);
                obj.name = $"Block_{zone.id}_{j:00}";
                obj.transform.SetParent(go.transform, false);
                obj.transform.localPosition = pos;
                float s = 0.8f + (float)rng.NextDouble() * 1.4f;
                float h = zone.id == "Forest" ? s * 3.5f : s * 0.7f;
                obj.transform.localScale = zone.id == "Forest"
                    ? new Vector3(s * 0.35f, h, s * 0.35f)
                    : new Vector3(s, h, s);
                obj.transform.localRotation = Quaternion.Euler(0, (float)rng.NextDouble() * 360f, 0);
                obj.isStatic = true;
                // GPU instancing candidate — share material in a real setup.
                var mat = zone.id == "Forest" ? foliageMaterial : rockMaterial;
                if (mat != null) obj.GetComponent<Renderer>().sharedMaterial = mat;
            }

            // Lighthouse zone additionally spawns the tower blockout here (WorldBuilder keeps doing the detailed tower).
            if (zone.id == "Lighthouse")
            {
                var tower = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                tower.name = "Lighthouse_Tower_Marker";
                tower.transform.SetParent(go.transform, false);
                tower.transform.localPosition = new Vector3(0, 6f, 0);
                tower.transform.localScale    = new Vector3(1.4f, 6f, 1.4f);
                tower.isStatic = true;
            }
        }

        void BuildTerrain(WorldConfig cfg)
        {
            // Create a minimal TerrainData so light probes / occlusion have a heightfield to work with.
            var tData = new TerrainData
            {
                size = new Vector3(cfg.islandSize, cfg.terrainHeight, cfg.islandSize),
                heightmapResolution = 129, // power-of-two+1; 129 is cheap on mobile
            };
            // Flat heightfield — sculpt in the editor after first import.
            var heights = new float[tData.heightmapResolution, tData.heightmapResolution];
            tData.SetHeights(0, 0, heights);
            var tGo = Terrain.CreateTerrainGameObject(tData);
            tGo.name = "Island_Terrain";
            tGo.transform.position = new Vector3(-cfg.islandSize * 0.5f, 0, -cfg.islandSize * 0.5f);
            tGo.isStatic = true;
            // ASTC-compressed splat textures: assign after importing your terrain textures.
            tGo.GetComponent<Terrain>().basemapDistance = 400f;
        }

        void TryBakeNavMesh()
        {
            // Lightweight nav: only bake if Unity.AI.Navigation package is present.
            // We try via reflection so the project compiles without it.
            var navType = System.Type.GetType("Unity.AI.Navigation.NavMeshSurface, Unity.AI.Navigation");
            if (navType == null) { Debug.Log("[IslandLayout] NavMeshSurface not found — install AI Navigation package to bake."); return; }
            var surface = gameObject.AddComponent(navType) as Component;
            navType.GetMethod("BuildNavMesh")?.Invoke(surface, null);
        }

        [System.Serializable]
        public struct ZoneReplacement
        {
            public string     zoneId;
            public GameObject prefab;
        }
    }
}
