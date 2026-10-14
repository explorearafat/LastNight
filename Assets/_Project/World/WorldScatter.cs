// Last Light — World/WorldScatter.cs
// Procedural zone+ slope scatter for rocks/trees/grass. Editor-friendly, also runs in build.

using UnityEngine;

namespace LastLight.World
{
    public sealed class WorldScatter : MonoBehaviour
    {
        [Header("Refs (auto-found)")]
        [SerializeField] WorldConfig config;

        [Header("Prefabs (assign Generated FBX prefabs, or leave null for blockout fallback)")]
        [SerializeField] GameObject pineTallPrefab;  // from LL_NatureKit: pine tall
        [SerializeField] GameObject pineShortPrefab;
        [SerializeField] GameObject scrubPrefab;
        [SerializeField] GameObject grassClumpPrefab;
        [SerializeField] GameObject rockPrefab;      // any LL_Rock_01..04
        [SerializeField] GameObject cliffPrefab;

        [Header("Counts (per quality, roughly)")]
        [SerializeField] int pineCount = 90;
        [SerializeField] int scrubCount = 40;
        [SerializeField] int rockExtraCount = 24;
        [SerializeField] int grassCount = 120;

        GameObject _root;

        void Awake() => ScatterIfNeeded();

        void ScatterIfNeeded()
        {
            if (GameObject.Find("WorldScatter_Root") != null) return;
            _root = new GameObject("WorldScatter_Root");
            var cfg = config != null ? config : ScriptableObject.CreateInstance<WorldConfig>();
            // Don't scatter on the islet
            Vector2 isletC = cfg.isletCenter; float isletR = cfg.isletRadius + 8f;
            System.Random rng = new System.Random(1337);
            for (int i=0;i<pineCount;i++)
            {
                Vector2 p = RandInIsland(rng, cfg, isletC, isletR);
                if (InsideZone(p, cfg, "Forest") || InsideZone(p, cfg, "MountainCave")) continue; // keep interior clear for castle/bunker
                var go = Spawn(pineTallPrefab ?? rockPrefab, new Vector3(p.x, 0, p.y), rng);
                if (go != null) go.transform.SetParent(_root.transform, true);
            }
            for (int i=0;i<scrubCount;i++)
            {
                Vector2 p = RandInIsland(rng, cfg, isletC, isletR);
                var go = Spawn(scrubPrefab ?? rockPrefab, new Vector3(p.x, 0, p.y), rng);
                if (go != null) go.transform.SetParent(_root.transform, true);
            }
            for (int i=0;i<rockExtraCount;i++)
            {
                Vector2 p = RandInIsland(rng, cfg, isletC, isletR);
                var go = Spawn(rockPrefab, new Vector3(p.x, 0, p.y), rng);
                if (go != null) go.transform.SetParent(_root.transform, true);
            }
            for (int i=0;i<grassCount;i++)
            {
                Vector2 p = RandInIsland(rng, cfg, isletC, isletR);
                if (InsideZone(p, cfg, "Beach") || InsideZone(p, cfg, "LighthouseIslet")) continue;
                var go = Spawn(grassClumpPrefab ?? rockPrefab, new Vector3(p.x, 0, p.y), rng);
                if (go != null) go.transform.SetParent(_root.transform, true);
            }
            // GPU instancing candidate — enable on shared mats
            if (_root != null)
                foreach (var r in _root.GetComponentsInChildren<Renderer>(true))
                    if (r.sharedMaterial != null) r.sharedMaterial.enableInstancing = true;
        }

        Vector2 RandInIsland(System.Random rng, WorldConfig cfg, Vector2 avoidCenter, float avoidR)
        {
            for (int t=0;t<20;t++)
            {
                float x = (float)rng.NextDouble()*cfg.islandSize - cfg.islandSize*0.5f;
                float z = (float)rng.NextDouble()*cfg.islandSize - cfg.islandSize*0.5f;
                var p = new Vector2(x,z);
                if (Vector2.Distance(p, avoidCenter) < avoidR) continue;
                // keep out of the channel strip?
                if (x > cfg.channelMin.x-20f && x < cfg.channelMax.x+20f && Mathf.Abs(z) < 70f)
                {
                    // channel water — no scatter
                    continue;
                }
                return p;
            }
            return Vector2.zero;
        }

        bool InsideZone(Vector2 p, WorldConfig cfg, string id)
        {
            foreach (var z in cfg.zones) if (z.id==id && Vector2.Distance(p, z.center) < z.radius*0.6f) return true;
            return false;
        }

        GameObject Spawn(GameObject prefab, Vector3 pos, System.Random rng)
        {
            // Ground at y=0 — raycast would be better but static y=0 is mobile-cheap for now
            if (prefab == null)
            {
                var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cube.transform.position = pos; cube.transform.localScale = new Vector3(0.6f,0.6f,0.6f);
                cube.isStatic = true; return cube;
            }
            var go = Instantiate(prefab, pos, Quaternion.Euler(0, (float)rng.NextDouble()*360f, 0));
            go.isStatic = true;
            float s = 0.9f + (float)rng.NextDouble()*0.25f;
            go.transform.localScale = Vector3.one * s;
            return go;
        }
    }
}
