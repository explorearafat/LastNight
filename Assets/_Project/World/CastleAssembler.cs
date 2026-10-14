// Last Light — World/CastleAssembler.cs
// Assembles the collapsed brick castle/fort on the main island using the LL_BrickKit FBX.
// Brick kit parts are looked up by child name. No scene setup beyond the prefab.

using UnityEngine;

namespace LastLight.World
{
    public sealed class CastleAssembler : MonoBehaviour
    {
        [SerializeField] GameObject brickKitPrefab; // LL_BrickKit
        [SerializeField] Vector3 castleCenter = new Vector3(40f, 0.15f, 140f);
        [SerializeField] Vector2 footprint = new Vector2(28f, 22f);

        GameObject _root;

        void Awake() => AssembleIfNeeded();

        void AssembleIfNeeded()
        {
            if (GameObject.Find("Castle_Root") != null) return;
            _root = new GameObject("Castle_Root");
            _root.transform.position = castleCenter;

            if (brickKitPrefab == null)
            {
                // Fallback blockout keep
                var keep = GameObject.CreatePrimitive(PrimitiveType.Cube);
                keep.name = "Castle_Keep_Marker";
                keep.transform.SetParent(_root.transform, false);
                keep.transform.localPosition = new Vector3(0, 3.5f, 0);
                keep.transform.localScale = new Vector3(10f, 7f, 10f);
                keep.isStatic = true; return;
            }

            // Build a rough ruined rectangle 28x22 out of straight wall segments
            // Four sides, with intentional gaps (collapsed sections), an arch gate, and two window walls
            Transform KitPart(string name) => FindKitPart(brickKitPrefab, name)?.transform;

            // Outer walls — place every ~4m
            PlaceWall("LL_Brick_Wall_Straight", new Vector3(-12f,0, footprint.y*0.5f-0.2f), 0);
            PlaceWall("LL_Brick_Wall_Broken",  new Vector3(-4f, 0, footprint.y*0.5f-0.2f), 0);
            // gate (arch) on south
            PlaceWall("LL_Brick_Wall_Arch",    new Vector3( 4f, 0, footprint.y*0.5f-0.2f), 0);
            PlaceWall("LL_Brick_Wall_Straight",new Vector3( 12f,0, footprint.y*0.5f-0.2f), 0);

            PlaceWall("LL_Brick_Wall_Straight",new Vector3(footprint.x*0.5f-0.2f,0, 6f), 90);
            PlaceWall("LL_Brick_Wall_Window",  new Vector3(footprint.x*0.5f-0.2f,0,-2f), 90);
            PlaceWall("LL_Brick_Wall_Broken",  new Vector3(footprint.x*0.5f-0.2f,0,-8f), 90);

            PlaceWall("LL_Brick_Wall_Straight",new Vector3(12f,0,-footprint.y*0.5f+0.2f), 0);
            PlaceWall("LL_Brick_Wall_Low",     new Vector3(4f, 0,-footprint.y*0.5f+0.2f), 0);
            PlaceWall("LL_Brick_Wall_Straight",new Vector3(-4f,0,-footprint.y*0.5f+0.2f), 0);
            PlaceWall("LL_Brick_Wall_Straight",new Vector3(-12f,0,-footprint.y*0.5f+0.2f),0);

            PlaceWall("LL_Brick_Wall_Straight",new Vector3(-footprint.x*0.5f+0.2f,0, 6f), 90);
            PlaceWall("LL_Brick_Wall_Door",    new Vector3(-footprint.x*0.5f+0.2f,0,-2f), 90);
            PlaceWall("LL_Brick_Wall_Straight",new Vector3(-footprint.x*0.5f+0.2f,0,-8f), 90);

            // Inner keep wall
            PlaceWall("LL_Brick_Wall_Straight",new Vector3(0,0, 4f), 0);
            // Pillars at corners
            PlaceWall("LL_Brick_Pillar", new Vector3(-10f,0, 8f), 0);
            PlaceWall("LL_Brick_Pillar", new Vector3( 10f,0, 8f), 0);
            PlaceWall("LL_Brick_Pillar", new Vector3(-10f,0,-8f), 0);
            PlaceWall("LL_Brick_Pillar", new Vector3( 10f,0,-8f), 0);

            // Rubble piles in gaps
            PlaceWall("LL_Brick_Rubble", new Vector3(0,0, footprint.y*0.5f+1.2f), 0);
            PlaceWall("LL_Brick_Rubble", new Vector3(footprint.x*0.5f+1.0f,0, 0), 0);

            MossWetnessDriver.PaintVertexMoss(_root);
            // isStatic for batching
            foreach (var r in _root.GetComponentsInChildren<Renderer>(true)) r.gameObject.isStatic = true;
        }

        void PlaceWall(string kitChildName, Vector3 localPos, float yawDeg)
        {
            if (brickKitPrefab == null) return;
            var src = FindKitPart(brickKitPrefab, kitChildName);
            if (src == null) { Debug.LogWarning($"[Castle] kit part {kitChildName} not found"); return; }
            var go = Instantiate(src, _root.transform);
            go.name = kitChildName;
            go.transform.localPosition = localPos;
            go.transform.localRotation = Quaternion.Euler(0, yawDeg, 0);
            // strip isStatic for now so MossWetnessDriver can write vertex colors before batching
        }

        static GameObject FindKitPart(GameObject kit, string name)
        {
            foreach (Transform t in kit.GetComponentsInChildren<Transform>(true))
                if (t.name == name) return t.gameObject;
            // second try: prefix match
            foreach (Transform t in kit.GetComponentsInChildren<Transform>(true))
                if (t.name.StartsWith(name)) return t.gameObject;
            return null;
        }
    }
}
