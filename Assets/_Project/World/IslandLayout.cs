// Last Light — World/IslandLayout.cs — Day 5 archipelago (islet + main island).
// Main island + small lighthouse islet separated by channel water. Zones via blockout.

using UnityEngine;
using System.Collections.Generic;

namespace LastLight.World
{
    public sealed class IslandLayout : MonoBehaviour
    {
        [SerializeField] WorldConfig config;

        [Header("Imported replacements (optional — assign meshes from Assets/Environment/)")]
        [SerializeField] List<ZoneReplacement> replacements = new();

        [Header("Materials (shared — enables SRP batching)")]
        [SerializeField] Material groundMaterial;
        [SerializeField] Material rockMaterial;
        [SerializeField] Material foliageMaterial;
        [SerializeField] Material isletMaterial;

        readonly List<GameObject> _spawned = new();
        GameObject _groundMain, _groundIslet;

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

            // Main island plane
            _groundMain = GameObject.CreatePrimitive(PrimitiveType.Plane);
            _groundMain.name = "Island_Ground";
            _groundMain.transform.position = Vector3.zero;
            _groundMain.transform.localScale = new Vector3(size / 10f, 1f, size / 10f);
            if (groundMaterial != null) _groundMain.GetComponent<Renderer>().sharedMaterial = groundMaterial;
            _groundMain.isStatic = true;

            // Lighthouse islet plane (small, raised a little)
            _groundIslet = GameObject.CreatePrimitive(PrimitiveType.Plane);
            _groundIslet.name = "Islet_Ground";
            _groundIslet.transform.position = new Vector3(cfg.isletCenter.x, 0.15f, cfg.isletCenter.y);
            float s = cfg.isletSize / 10f;
            _groundIslet.transform.localScale = new Vector3(s, 1f, s);
            var m = isletMaterial != null ? isletMaterial : rockMaterial;
            if (m != null) _groundIslet.GetComponent<Renderer>().sharedMaterial = m;
            _groundIslet.isStatic = true;

            if (cfg.useTerrain) BuildTerrain(cfg);

            for (int i = 0; i < cfg.zones.Length; i++)
            {
                var zone = cfg.zones[i];
                var repl = replacements.Find(r => r.zoneId == zone.id);
                if (repl != null && repl.prefab != null)
                {
                    var go = Instantiate(repl.prefab, new Vector3(zone.center.x, 0, zone.center.y), Quaternion.identity, transform);
                    go.name = $"Zone_{zone.id}"; _spawned.Add(go);
                }
                else BuildBlockoutZone(zone, i);
            }

            if (cfg.bakeNavMesh) TryBakeNavMesh();
        }

        void BuildBlockoutZone(ZoneDef zone, int index)
        {
            var go = new GameObject($"Zone_{zone.id}");
            go.transform.position = new Vector3(zone.center.x, 0, zone.center.y);
            _spawned.Add(go);
            var baseCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            baseCube.name = $"Zone_{zone.id}_Base";
            baseCube.transform.SetParent(go.transform, false);
            baseCube.transform.localPosition = new Vector3(0, zone.height * 0.5f, 0);
            baseCube.transform.localScale    = new Vector3(zone.radius * 0.55f, Mathf.Max(0.2f, zone.height), zone.radius * 0.55f);
            if (groundMaterial != null) baseCube.GetComponent<Renderer>().sharedMaterial = groundMaterial;
            baseCube.isStatic = true;

            System.Random rng = new(index * 7919 + 104729);
            int count = zone.id switch { "LighthouseIslet" => 3, "Castle" => 10, "Bunker" => 6, "MountainCave" => 8, "Forest" => 16, "Beach" => 10, "CanneryPier" => 6, "Ruins" => 8, _ => 6 };
            for (int j = 0; j < count; j++)
            {
                float a = (float)rng.NextDouble() * Mathf.PI * 2f;
                float r = (float)rng.NextDouble() * zone.radius * 0.40f;
                Vector3 pos = new Vector3(Mathf.Cos(a) * r, zone.height + 0.3f, Mathf.Sin(a) * r);
                var prim = zone.id == "Forest" ? PrimitiveType.Cylinder : PrimitiveType.Cube;
                var obj = GameObject.CreatePrimitive(prim);
                obj.name = $"Block_{zone.id}_{j:00}";
                obj.transform.SetParent(go.transform, false);
                obj.transform.localPosition = pos;
                float s = 0.8f + (float)rng.NextDouble() * 1.4f;
                float h = zone.id == "Forest" ? s * 3.5f : s * 0.7f;
                obj.transform.localScale = zone.id == "Forest" ? new Vector3(s*0.35f,h,s*0.35f) : new Vector3(s,h,s);
                obj.transform.localRotation = Quaternion.Euler(0, (float)rng.NextDouble()*360f, 0);
                obj.isStatic = true;
                var mat = zone.id == "Forest" ? foliageMaterial : rockMaterial;
                if (mat != null) obj.GetComponent<Renderer>().sharedMaterial = mat;
            }
            if (zone.id == "LighthouseIslet")
            {
                var tower = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                tower.name = "Lighthouse_Tower_Marker";
                tower.transform.SetParent(go.transform, false);
                tower.transform.localPosition = new Vector3(0, 6f, 0);
                tower.transform.localScale = new Vector3(1.4f, 6f, 1.4f); tower.isStatic = true;
            }
            if (zone.id == "Castle")
            {
                // hint of ruined keep: a taller tower marker
                var keep = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                keep.name = "Castle_Keep_Marker";
                keep.transform.SetParent(go.transform, false);
                keep.transform.localPosition = new Vector3(0, 7f, 0);
                keep.transform.localScale = new Vector3(2.0f, 7f, 2.0f); keep.isStatic = true;
            }
        }

        void BuildTerrain(WorldConfig cfg)
        {
            var tData = new TerrainData { size = new Vector3(cfg.islandSize, cfg.terrainHeight, cfg.islandSize), heightmapResolution = 129 };
            var heights = new float[tData.heightmapResolution, tData.heightmapResolution];
            tData.SetHeights(0,0,heights);
            var tGo = Terrain.CreateTerrainGameObject(tData);
            tGo.name = "Island_Terrain";
            tGo.transform.position = new Vector3(-cfg.islandSize*0.5f, 0, -cfg.islandSize*0.5f);
            tGo.isStatic = true;
            tGo.GetComponent<Terrain>().basemapDistance = 400f;
        }

        void TryBakeNavMesh()
        {
            var navType = System.Type.GetType("Unity.AI.Navigation.NavMeshSurface, Unity.AI.Navigation");
            if (navType == null) { Debug.Log("[IslandLayout] NavMeshSurface not found — install AI Navigation package to bake."); return; }
            var surface = gameObject.AddComponent(navType) as Component;
            navType.GetMethod("BuildNavMesh")?.Invoke(surface, null);
        }

        [System.Serializable]
        public struct ZoneReplacement { public string zoneId; public GameObject prefab; }
    }
}
