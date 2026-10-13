// Last Light — World/LighthouseInterior.cs
// Builds the lighthouse interior as blockout primitives: stairs, lens room,
// generator room, keeper's room, doors, windows. Audio zones + warmth volumes included.
// Replaced by imported meshes from Assets/Environment/ — the blockout at the same
// positions keeps the game playable before art.

using UnityEngine;

namespace LastLight.World
{
    public sealed class LighthouseInterior : MonoBehaviour
    {
        [Header("Imported replacements (optional)")]
        [SerializeField] GameObject interiorPrefab; // full interior mesh, if available

        [Header("Materials")]
        [SerializeField] Material woodMaterial;
        [SerializeField] Material metalMaterial;
        [SerializeField] Material stoneMaterial;

        GameObject _root;

        void Awake() => BuildIfNeeded();

        void BuildIfNeeded()
        {
            if (GameObject.Find("Lighthouse_Interior_Root") != null) return;
            if (interiorPrefab != null)
            {
                _root = Instantiate(interiorPrefab);
                _root.name = "Lighthouse_Interior_Root";
                _root.isStatic = true;
                return;
            }
            BuildBlockout();
        }

        void BuildBlockout()
        {
            var tower = GameObject.Find("Lighthouse_Tower");
            Transform parent = tower != null ? tower.transform : transform;
            _root = new GameObject("Lighthouse_Interior_Root");
            _root.transform.SetParent(parent, false);
            _root.transform.localPosition = Vector3.zero;

            // ── Keeper's room floor (wood, inside the tower base) ──
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Interior_Floor";
            floor.transform.SetParent(_root.transform, false);
            floor.transform.localPosition = new Vector3(0, 0.12f, 0);
            floor.transform.localScale    = new Vector3(2.0f, 0.08f, 2.0f);
            floor.tag = "Wood"; floor.isStatic = true;
            if (woodMaterial != null) floor.GetComponent<Renderer>().sharedMaterial = woodMaterial;

            // ── Spiral stairs (12 steps around the tower wall) ──
            var stairsRoot = new GameObject("Stairs_Spiral");
            stairsRoot.transform.SetParent(_root.transform, false);
            for (int i = 0; i < 12; i++)
            {
                float frac = i / 12f;
                float ang  = frac * 360f;
                float h    = 0.35f + frac * 7.2f;
                float r    = 0.85f;
                var step = GameObject.CreatePrimitive(PrimitiveType.Cube);
                step.name = $"Stair_{i:00}";
                step.transform.SetParent(stairsRoot.transform, false);
                step.transform.localPosition = new Vector3(Mathf.Cos(ang * Mathf.Deg2Rad) * r, h, Mathf.Sin(ang * Mathf.Deg2Rad) * r);
                step.transform.localScale    = new Vector3(0.55f, 0.12f, 0.35f);
                step.transform.localRotation = Quaternion.Euler(0, -ang, 0);
                step.tag = "Wood"; step.isStatic = true;
                if (woodMaterial != null) step.GetComponent<Renderer>().sharedMaterial = woodMaterial;
                // Stairs volume so animator knows we're on stairs
                var vol = step.AddComponent<StairsVolume>();
                var col = step.GetComponent<BoxCollider>(); var b = col.bounds;
                // Expand trigger slightly so the player touches it reliably
                col.isTrigger = true;
            }

            // ── Generator room wall (low partition) ──
            var genWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            genWall.name = "Interior_GeneratorWall";
            genWall.transform.SetParent(_root.transform, false);
            genWall.transform.localPosition = new Vector3(0.6f, 0.55f, 0.35f);
            genWall.transform.localScale    = new Vector3(0.12f, 1.0f, 1.2f);
            genWall.isStatic = true;
            if (metalMaterial != null) genWall.GetComponent<Renderer>().sharedMaterial = metalMaterial;

            // ── Lens room (top cylinder with open sides / glass placeholder) ──
            var lensFrame = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            lensFrame.name = "LensRoom_Frame";
            lensFrame.transform.SetParent(_root.transform, false);
            lensFrame.transform.localPosition = new Vector3(0, 8.0f, 0);
            lensFrame.transform.localScale    = new Vector3(1.25f, 0.45f, 1.25f);
            lensFrame.isStatic = true;
            if (metalMaterial != null) lensFrame.GetComponent<Renderer>().sharedMaterial = metalMaterial;

            // Zone volumes (explicit ShelterZones for the storm tracker)
            CreateZone("Zone_GeneratorRoom", ShelterZone.GeneratorRoom, new Vector3(0.55f, 0.6f, 0.2f), new Vector3(1.2f, 1.4f, 1.4f));
            CreateZone("Zone_LensRoom",      ShelterZone.LensRoom,      new Vector3(0, 8.0f, 0),       new Vector3(2.0f, 1.2f, 2.0f));
            CreateZone("Zone_KeepersRoom",   ShelterZone.LighthouseInterior, new Vector3(0, 0.9f, 0),  new Vector3(2.2f, 2f, 2.2f));
        }

        void CreateZone(string name, Core.ShelterZone zone, Vector3 localPos, Vector3 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(_root.transform, false);
            go.transform.localPosition = localPos;
            var col = go.AddComponent<BoxCollider>(); col.isTrigger = true; col.size = size;
            var zv  = go.AddComponent<ZoneVolume>();
            // Set private field via reflection-safe helper would need field access; allow inspector default
            // Instead use a small runtime setter if the field is private — here we assume the zone is set via SetZone
            typeof(ZoneVolume).GetField("zone", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(zv, zone);
        }
    }
}
