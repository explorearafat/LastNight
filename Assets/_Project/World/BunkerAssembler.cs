// Last Light — World/BunkerAssembler.cs
// Half-flooded WWII bunker — concrete + brick, overgrown, on the main island.

using UnityEngine;

namespace LastLight.World
{
    public sealed class BunkerAssembler : MonoBehaviour
    {
        [SerializeField] GameObject brickKitPrefab; // LL_BrickKit
        [SerializeField] Vector3 bunkerCenter = new Vector3(-140f, 0.05f, 90f);

        GameObject _root;

        void Awake() => AssembleIfNeeded();

        void AssembleIfNeeded()
        {
            if (GameObject.Find("Bunker_Root") != null) return;
            _root = new GameObject("Bunker_Root");
            _root.transform.position = bunkerCenter;

            if (brickKitPrefab == null)
            {
                var m = GameObject.CreatePrimitive(PrimitiveType.Cube);
                m.name = "Bunker_Marker"; m.transform.SetParent(_root.transform, false);
                m.transform.localScale = new Vector3(14f, 2.2f, 10f); m.transform.localPosition = new Vector3(0,0.8f,0);
                m.isStatic = true; return;
            }
            // Squat bunker: low walls, flat concrete floor, slit windows, a single entry arch
            Place("LL_Brick_Floor_4x4", new Vector3(0,0,0), 0, scale: new Vector3(3.2f,1,2.2f));
            Place("LL_Brick_Wall_Straight", new Vector3(0,0, 4.2f), 0);
            Place("LL_Brick_Wall_Broken",   new Vector3(-6f,0, 4.2f), 0);
            Place("LL_Brick_Wall_Straight", new Vector3(6f,0,  4.2f), 0);
            Place("LL_Brick_Wall_Window",   new Vector3(7.5f,0, 0), 90);
            Place("LL_Brick_Wall_Straight", new Vector3(7.5f,0,-3.2f), 90);
            Place("LL_Brick_Wall_Arch",     new Vector3(0,0, -4.2f), 0);
            Place("LL_Brick_Wall_Straight", new Vector3(-7.5f,0, 0), 90);
            Place("LL_Brick_Wall_Broken",   new Vector3(-7.5f,0,-3.2f), 90);

            MossWetnessDriver.PaintVertexMoss(_root);
            foreach (var r in _root.GetComponentsInChildren<Renderer>(true)) r.gameObject.isStatic = true;
            // flood plane (thin water at y=0.12 so it looks half-flooded)
            var flood = GameObject.CreatePrimitive(PrimitiveType.Plane);
            flood.name = "Bunker_Flood"; flood.transform.SetParent(_root.transform, false);
            flood.transform.localPosition = new Vector3(0, 0.12f, 0);
            flood.transform.localScale = new Vector3(1.4f,1,1f);
            var rnd = flood.GetComponent<Renderer>();
            if (rnd.sharedMaterial != null) rnd.sharedMaterial.color = new Color(0.08f,0.18f,0.22f,0.72f);
        }

        void Place(string name, Vector3 pos, float yaw, Vector3 scale = default)
        {
            if (brickKitPrefab==null) return;
            var src = FindKit(brickKitPrefab, name);
            if (src==null) { Debug.LogWarning($"[Bunker] {name} missing"); return; }
            var go = Instantiate(src, _root.transform);
            go.name=name; go.transform.localPosition=pos; go.transform.localRotation=Quaternion.Euler(0,yaw,0);
            if (scale!=default(Vector3)) go.transform.localScale = scale;
        }
        static GameObject FindKit(GameObject kit, string name)
        {
            foreach(Transform t in kit.GetComponentsInChildren<Transform>(true)) if(t.name==name) return t.gameObject;
            foreach(Transform t in kit.GetComponentsInChildren<Transform>(true)) if(t.name.StartsWith(name)) return t.gameObject;
            return null;
        }
    }
}
