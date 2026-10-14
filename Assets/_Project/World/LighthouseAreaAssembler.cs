// Last Light — World/LighthouseAreaAssembler.cs
// Assembles the LIGHTHOUSE AREA prefab set into the scene at Lighthouse zone (origin).
// Replaces/extends WorldBuilder's placeholder tower + adds keepers house, props, fencing,
// rocks, and interactables. Safe to leave in scene permanently — no-ops if already assembled.
//
// Drop on a GameObject "LighthouseArea" in Island.unity, assign prefabs from Generated/.
// Or leave unassigned: it will try Resources/ fallback and otherwise the blockout remains.

using UnityEngine;

namespace LastLight.World
{
    public sealed class LighthouseAreaAssembler : MonoBehaviour
    {
        [Header("Prefabs (from Assets/Environment/Generated/)")]
        [SerializeField] GameObject lighthousePrefab;   // LL_Lighthouse_Tower
        [SerializeField] GameObject keepersHousePrefab; // LL_Keepers_House
        [SerializeField] GameObject generatorPrefab;    // LL_Generator
        [SerializeField] GameObject fuelCanisterPrefab; // LL_Fuel_Canister
        [SerializeField] GameObject lanternPrefab;      // LL_Lantern
        [SerializeField] GameObject rocksSetPrefab;     // LL_Rocks_Set
        [SerializeField] GameObject driftwoodPrefab;    // LL_Driftwood
        [SerializeField] GameObject fencePrefab;        // LL_Fence_4m
        [SerializeField] GameObject dockPrefab;         // LL_Dock_5m
        [SerializeField] GameObject cratesPrefab;       // LL_Crates
        [SerializeField] GameObject barrelPrefab;       // LL_Barrel
        [SerializeField] GameObject anchorPrefab;       // LL_Anchor
        [SerializeField] GameObject buoyPrefab;         // LL_Buoy
        [SerializeField] GameObject chainPilePrefab;    // LL_Chain_Pile
        [SerializeField] GameObject notePaperPrefab;    // LL_Note_Paper

        [Header("Layout (all in metres, relative to lighthouse origin)")]
        [SerializeField] Vector3 houseOffset    = new Vector3(12f, 0f, -5f);
        [SerializeField] Vector3 generatorOffset= new Vector3( 2f, 0f, -2.2f);
        [SerializeField] float   generatorYaw  = 35f;
        [SerializeField] Vector3 rocksJitter   = new Vector3(20f, 0f, 20f);

        GameObject _root;

        void Awake() => AssembleIfNeeded();

        void AssembleIfNeeded()
        {
            if (GameObject.Find("LighthouseArea_Root") != null) return;
            _root = new GameObject("LighthouseArea_Root");

            // 1) Lighthouse — replaces the primitive tower if it exists
            var oldTower = GameObject.Find("Lighthouse_Tower");
            if (lighthousePrefab != null)
            {
                var lh = Instantiate(lighthousePrefab, Vector3.zero, Quaternion.identity, _root.transform);
                lh.name = "Lighthouse_Tower";
                lh.isStatic = true;
                // Remove old blockout tower visuals if they overlap
                if (oldTower != null && oldTower != lh)
                {
                    // Keep its LighthouseState + DamageableStructure by moving them
                    MoveComponentIfExists<LastLight.Lighthouse.LighthouseState>(oldTower, lh);
                    MoveComponentIfExists<DamageableStructure>(oldTower, lh);
                    Destroy(oldTower);
                }
                else if (oldTower == null)
                {
                    lh.AddComponent<LastLight.Lighthouse.LighthouseState>();
                    lh.AddComponent<DamageableStructure>();
                }
                // Ensure warmth volume for interior
                EnsureWarmth(lh, new Vector3(0, 8f, 0), new Vector3(6f, 16f, 6f));
                // Beacon light: find LL_Lens and add a Spot if missing
                EnsureBeaconLight(lh);
                // Stairs volumes already inside FBX — tag them and add StairsVolume
                foreach (Transform t in lh.GetComponentsInChildren<Transform>(true))
                    if (t.name.Contains("Stair")) t.gameObject.AddComponent<StairsVolume>();
            }

            // 2) Keeper's house
            if (keepersHousePrefab != null)
                Spawn(keepersHousePrefab, houseOffset, Quaternion.Euler(0, 25f, 0), "KeepersHouse");

            // 3) Generator
            if (generatorPrefab != null)
            {
                var gen = Spawn(generatorPrefab, generatorOffset, Quaternion.Euler(0, generatorYaw, 0), "Generator");
                // Add refuel/repair interactables to the generator
                if (gen != null)
                {
                    var col = gen.AddComponent<BoxCollider>(); col.isTrigger = true; col.size = new Vector3(2f, 1.5f, 1.5f); col.center = new Vector3(0, 0.6f, 0);
                    gen.AddComponent<LastLight.Lighthouse.RefuelInteractable>();
                    // Repair hitbox slightly offset
                    var repGo = new GameObject("RepairGenerator"); repGo.transform.SetParent(gen.transform, false);
                    repGo.transform.localPosition = new Vector3(0.5f, 0.6f, 0);
                    var repCol = repGo.AddComponent<BoxCollider>(); repCol.isTrigger = true; repCol.size = new Vector3(1f, 1f, 1f);
                    repGo.AddComponent<LastLight.Lighthouse.RepairGeneratorInteractable>();
                }
            }

            // 4) Fuel canisters (2 pickups)
            if (fuelCanisterPrefab != null)
            {
                SpawnWithPickup(fuelCanisterPrefab, generatorOffset + new Vector3(1.4f, 0, 0.6f), Quaternion.Euler(0, -20f, 0), Core.ItemType.Fuel, 1);
                SpawnWithPickup(fuelCanisterPrefab, houseOffset + new Vector3(-2f, 0, 2f), Quaternion.Euler(0, 60f, 0), Core.ItemType.Fuel, 1);
            }

            // 5) Lantern (on a crate near the door)
            if (lanternPrefab != null)
                Spawn(lanternPrefab, new Vector3(0.9f, 0.72f, 1.4f), Quaternion.identity, "Lantern_OnCrate");

            // 6) Rocks — scatter around the lighthouse base + cliff edge
            if (rocksSetPrefab != null)
            {
                Spawn(rocksSetPrefab, new Vector3( 4.5f, 0,  5f), Quaternion.Euler(0,  30f, 0), "Rocks_A");
                Spawn(rocksSetPrefab, new Vector3(-5f,   0, -4f), Quaternion.Euler(0, -15f, 0), "Rocks_B");
            }
            else
            {
                // Fallback: individual rocks
                if (TryLoadPrefab("LL_Rock_01", out var r1)) Spawn(r1, new Vector3( 5f, 0,  4f), Quaternion.Euler(0, 20f, 0), "Rock_01");
                if (TryLoadPrefab("LL_Rock_02", out var r2)) Spawn(r2, new Vector3(-4f, 0,  5f), Quaternion.Euler(0,-30f, 0), "Rock_02");
            }

            // 7) Driftwood near the shore side
            if (driftwoodPrefab != null)
                Spawn(driftwoodPrefab, new Vector3( 6f, 0, -8f), Quaternion.Euler(0, 65f, 0), "Driftwood");

            // 8) Fence (keeper's house perimeter)
            if (fencePrefab != null)
            {
                Spawn(fencePrefab, houseOffset + new Vector3( 4.5f, 0, 0), Quaternion.Euler(0, 90f, 0), "Fence_East");
                Spawn(fencePrefab, houseOffset + new Vector3(-4.5f, 0, 0), Quaternion.Euler(0, 90f, 0), "Fence_West");
            }

            // 9) Dock (shoreline)
            if (dockPrefab != null)
                Spawn(dockPrefab, new Vector3( 2f, 0, -11f), Quaternion.Euler(0, 0, 0), "Dock");

            // 10) Crates & barrels
            if (cratesPrefab != null) Spawn(cratesPrefab, new Vector3(-2.5f, 0, 1.2f), Quaternion.Euler(0, 15f, 0), "Crates");
            if (barrelPrefab != null) Spawn(barrelPrefab, new Vector3(-2.0f, 0, 2.2f), Quaternion.Euler(0, -25f, 0), "Barrel");

            // 11) Rusted props
            if (anchorPrefab != null) Spawn(anchorPrefab, new Vector3( 5.5f, 0, 3.5f), Quaternion.Euler(0, 40f, 0), "Anchor");
            if (buoyPrefab != null)   Spawn(buoyPrefab,   new Vector3( 3.2f, 0, -9f),  Quaternion.Euler(0, 0, 0), "Buoy");
            if (chainPilePrefab != null) Spawn(chainPilePrefab, new Vector3( 1.2f, 0, -10f), Quaternion.Euler(0, 15f, 0), "ChainPile");

            // 12) Note (diegetic)
            if (notePaperPrefab != null)
            {
                var noteGo = Spawn(notePaperPrefab, new Vector3(0.2f, 1.1f, 0.4f), Quaternion.Euler(15f, 20f, 0), "Note_KeepersLog");
                if (noteGo != null)
                {
                    var col2 = noteGo.AddComponent<BoxCollider>(); col2.isTrigger = true; col2.size = new Vector3(0.6f, 0.6f, 0.3f);
                    noteGo.tag = "Untagged";
                    // If NotePickup exists, wire it — otherwise leave as pickup via generic interactable
                    var pickup = noteGo.AddComponent<NotePickup>();
                    // Assign a WorldNoteData if available
                    var notes = Resources.LoadAll<WorldNoteData>("");
                    if (notes.Length > 0) SetNoteData(pickup, notes[0]);
                }
            }

            Debug.Log("[LighthouseArea] Assembled — lighthouse zone dressed. Assign missing prefabs in LighthouseAreaAssembler inspector.");
        }

        GameObject Spawn(GameObject prefab, Vector3 pos, Quaternion rot, string name)
        {
            if (prefab == null) return null;
            var go = Instantiate(prefab, pos, rot, _root.transform);
            go.name = name;
            go.isStatic = true;
            return go;
        }

        void SpawnWithPickup(GameObject prefab, Vector3 pos, Quaternion rot, Core.ItemType type, int amount)
        {
            var go = Spawn(prefab, pos, rot, $"Pickup_{type}");
            if (go == null) return;
            var col = go.AddComponent<BoxCollider>(); col.isTrigger = true; col.size = new Vector3(0.8f, 0.8f, 0.8f);
            var pickup = go.AddComponent<LastLight.Lighthouse.PickupInteractable>();
            // Set private fields via reflection (itemType / amount)
            var t = pickup.GetType();
            t.GetField("itemType", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(pickup, type);
            t.GetField("amount", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(pickup, amount);
            t.GetField("visualToHide", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(pickup, go);
        }

        bool TryLoadPrefab(string name, out GameObject prefab)
        {
            prefab = Resources.Load<GameObject>($"Environment/Generated/{name}");
            if (prefab == null) prefab = Resources.Load<GameObject>(name);
            return prefab != null;
        }

        void MoveComponentIfExists<T>(GameObject from, GameObject to) where T : Component
        {
            var c = from.GetComponent<T>();
            if (c == null) return;
            // Can't move — copy fields via reflection and destroy old
            var dst = to.GetComponent<T>() ?? to.AddComponent<T>();
            var fields = typeof(T).GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            foreach (var f in fields) if (!f.IsInitOnly) try { f.SetValue(dst, f.GetValue(c)); } catch { }
        }

        void EnsureWarmth(GameObject parent, Vector3 center, Vector3 size)
        {
            if (parent.GetComponentInChildren<LastLight.Survival.WarmthVolume>() != null) return;
            var w = new GameObject("WarmthVolume_Interior");
            w.transform.SetParent(parent.transform, false);
            w.transform.localPosition = center;
            var col = w.AddComponent<BoxCollider>(); col.isTrigger = true; col.size = size;
            w.AddComponent<LastLight.Survival.WarmthVolume>();
        }

        void EnsureBeaconLight(GameObject lighthouse)
        {
            var lens = lighthouse.transform.Find("LL_Lens") ?? lighthouse.transform;
            if (lens.GetComponentInChildren<Light>() != null) return;
            var go = new GameObject("Beacon_Light");
            go.transform.SetParent(lens, false);
            go.transform.localPosition = new Vector3(0, 0.3f, 0);
            var l = go.AddComponent<Light>();
            l.type = LightType.Spot; l.spotAngle = 38f; l.range = 280f; l.intensity = 3.2f;
            l.color = new Color(1f, 0.92f, 0.72f); l.shadows = LightShadows.None;
            // lighthouse state will drive it via beaconLight ref
            var state = lighthouse.GetComponent<LastLight.Lighthouse.LighthouseState>();
            if (state != null)
            {
                var fi = typeof(LastLight.Lighthouse.LighthouseState).GetField("beaconLight",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                fi?.SetValue(state, l);
            }
        }

        void SetNoteData(NotePickup pickup, WorldNoteData data)
        {
            var fi = typeof(NotePickup).GetField("noteData",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            fi?.SetValue(pickup, data);
        }
    }
}
