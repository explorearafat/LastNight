// Last Light — World/WorldBuilder.cs
// Spawns a minimal placeholder island so the game is playable before any art is imported.
// Creates: ground plane, rocks, stairs, lighthouse cylinder + beacon light + structure + spawners.
// All placeholder meshes use Unity primitives and a single shared URP Lit material.

using LastLight.Survival;
using UnityEngine;

namespace LastLight.World
{
    public sealed class WorldBuilder : MonoBehaviour
    {
        [Header("Island")]
        [SerializeField] Vector2 islandSize = new Vector2(80, 80);
        [SerializeField] Material groundMaterial;
        [SerializeField] Material rockMaterial;
        [SerializeField] Material lighthouseMaterial;

        [Header("Gatherables")]
        [SerializeField] int gatherableCount = 14;

        [Header("Refs (auto-found if null)")]
        [SerializeField] Transform playerSpawn;
        [SerializeField] Transform lighthouseRoot;

        void Awake() => BuildIfNeeded();

        void BuildIfNeeded()
        {
            if (GameObject.Find("Island_Ground") != null) return;
            Build();
        }

        void Build()
        {
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Island_Ground";
            ground.transform.position = Vector3.zero;
            ground.transform.localScale = new Vector3(islandSize.x / 10f, 1f, islandSize.y / 10f);
            if (groundMaterial != null) ground.GetComponent<Renderer>().sharedMaterial = groundMaterial;

            // Rocks
            for (int i = 0; i < 10; i++)
            {
                var rock = GameObject.CreatePrimitive(PrimitiveType.Cube);
                rock.name = $"Rock_{i:00}";
                float rx = Random.Range(-islandSize.x * 0.42f, islandSize.x * 0.42f);
                float rz = Random.Range(-islandSize.y * 0.42f, islandSize.y * 0.42f);
                rock.transform.position = new Vector3(rx, 0.5f, rz);
                float s = Random.Range(0.6f, 1.8f);
                rock.transform.localScale = new Vector3(s, s * 0.9f, s);
                rock.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360f), 0);
                if (rockMaterial != null) rock.GetComponent<Renderer>().sharedMaterial = rockMaterial;
            }

            // Stairs demo (a few cubes as slope test for CharacterController)
            for (int i = 0; i < 5; i++)
            {
                var step = GameObject.CreatePrimitive(PrimitiveType.Cube);
                step.name = $"Stair_{i}";
                step.transform.position = new Vector3(4f, 0.15f + i * 0.22f, 6f + i * 0.55f);
                step.transform.localScale = new Vector3(2.2f, 0.2f, 0.55f);
            }

            // Lighthouse tower
            var tower = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            tower.name = "Lighthouse_Tower";
            tower.transform.position = new Vector3(0, 4f, 0);
            tower.transform.localScale = new Vector3(1.2f, 4f, 1.2f);
            if (lighthouseMaterial != null) tower.GetComponent<Renderer>().sharedMaterial = lighthouseMaterial;

            // Lantern head
            var head = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            head.name = "Lighthouse_Lantern";
            head.transform.SetParent(tower.transform, true);
            head.transform.localPosition = new Vector3(0, 1.05f, 0);
            head.transform.localScale = new Vector3(1.05f, 0.15f, 1.05f);

            // Beacon light
            var beaconGo = new GameObject("Beacon_Light");
            beaconGo.transform.SetParent(head.transform, false);
            beaconGo.transform.localPosition = Vector3.up * 0.5f;
            var beacon = beaconGo.AddComponent<Light>();
            beacon.type = LightType.Spot;
            beacon.spotAngle = 42f;
            beacon.intensity = 0f;
            beacon.color = new Color(1f, 0.92f, 0.72f);
            beacon.shadows = LightShadows.None;
            beaconGo.AddComponent<Lighthouse.LighthouseState>();

            // Interior warmth volume
            var warmthGo = new GameObject("WarmthVolume_Interior");
            warmthGo.transform.SetParent(tower.transform, true);
            warmthGo.transform.localPosition = new Vector3(0, -0.4f, 0);
            var warmthCol = warmthGo.AddComponent<BoxCollider>();
            warmthCol.isTrigger = true;
            warmthCol.size = new Vector3(3.2f, 6f, 3.2f);
            warmthGo.AddComponent<WarmthVolume>();

            // Damageable structure (world VFX hooks)
            tower.AddComponent<DamageableStructure>();

            // Boardable windows (two placeholders around the tower)
            for (int w = 0; w < 2; w++)
            {
                var winGo = new GameObject($"Window_{w}");
                winGo.transform.SetParent(tower.transform, true);
                winGo.transform.localPosition = new Vector3((w == 0 ? 1f : -1f) * 1.1f, -0.2f, 0);
                var winCol = winGo.AddComponent<BoxCollider>();
                winCol.isTrigger = true; winCol.size = new Vector3(0.9f, 1.2f, 0.3f);
                var board = winGo.AddComponent<Lighthouse.BoardWindowInteractable>();
                // windowId auto from name if not set
                var boardsVisual = GameObject.CreatePrimitive(PrimitiveType.Cube);
                boardsVisual.name = "BoardsVisual";
                boardsVisual.transform.SetParent(winGo.transform, false);
                boardsVisual.transform.localPosition = Vector3.zero;
                boardsVisual.transform.localScale = new Vector3(1f, 1f, 0.15f);
                boardsVisual.SetActive(false);
                // Wire boardsVisual via reflection-safe field
                var fi = typeof(Lighthouse.BoardWindowInteractable).GetField("boardsVisual",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                fi?.SetValue(board, boardsVisual);
            }

            // Gatherable spawners — Day 5: only on main island (not islet), so the boat crossing matters
            var gatherRoot = new GameObject("Gatherables");
            var types = new[] { Core.ItemType.Wood, Core.ItemType.Food, Core.ItemType.Fuel, Core.ItemType.RepairParts };
            var cfg = FindFirstObjectByType<WorldConfig>();
            Vector2 isletCenter = cfg != null ? cfg.isletCenter : new Vector2(-520f, 0f);
            float isletR = cfg != null ? cfg.isletRadius + 12f : 85f;
            for (int i = 0; i < gatherableCount; i++)
            {
                Core.ItemType t = types[i % types.Length];
                float rx, rz;
                int tries = 0;
                do
                {
                    rx = Random.Range(-islandSize.x * 0.4f, islandSize.x * 0.4f);
                    rz = Random.Range(-islandSize.y * 0.4f, islandSize.y * 0.4f);
                    tries++;
                } while (new Vector2(rx, rz).magnitude < 5f || Vector2.Distance(new Vector2(rx, rz), isletCenter) < isletR);
                GatherableSpawner.CreatePlaceholder(t, new Vector3(rx, 0.25f, rz), gatherRoot.transform);
            }

            // Loose pickups inside the lighthouse
            var pickupRoot = new GameObject("Pickups_Interior");
            pickupRoot.transform.SetParent(tower.transform, true);
            pickupRoot.transform.localPosition = new Vector3(0.6f, -3.2f, 0);
            CreatePickup(Core.ItemType.Fuel, 1, new Vector3(0.6f, 0.3f, 0.4f), pickupRoot.transform);
            CreatePickup(Core.ItemType.RepairParts, 1, new Vector3(-0.5f, 0.3f, 0.6f), pickupRoot.transform);

            // Generator console (refuel + repair interactables near the tower base)
            var genGo = new GameObject("Generator_Console");
            genGo.transform.SetParent(tower.transform, true);
            genGo.transform.localPosition = new Vector3(0.8f, -3.0f, 0.5f);
            var genCol = genGo.AddComponent<BoxCollider>(); genCol.isTrigger = true; genCol.size = new Vector3(0.8f, 0.8f, 0.6f);
            genGo.AddComponent<Lighthouse.RefuelInteractable>();
            var repGenGo = new GameObject("Repair_Generator");
            repGenGo.transform.SetParent(genGo.transform, true);
            repGenGo.transform.localPosition = new Vector3(0.5f, 0, 0);
            var repCol = repGenGo.AddComponent<BoxCollider>(); repCol.isTrigger = true; repCol.size = new Vector3(0.6f, 0.6f, 0.5f);
            repGenGo.AddComponent<Lighthouse.RepairGeneratorInteractable>();

            // Lens room (repair lens)
            var lensGo = new GameObject("Lens_Console");
            lensGo.transform.SetParent(head.transform, true);
            lensGo.transform.localPosition = new Vector3(0, 0.4f, 0);
            var lensCol = lensGo.AddComponent<BoxCollider>(); lensCol.isTrigger = true; lensCol.size = new Vector3(0.7f, 0.7f, 0.7f);
            lensGo.AddComponent<Lighthouse.RepairLensInteractable>();
        }

        static void CreatePickup(Core.ItemType type, int amount, Vector3 localPos, Transform parent)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = $"Pickup_{type}";
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = new Vector3(0.35f, 0.35f, 0.35f);
            go.AddComponent<Lighthouse.PickupInteractable>();
            // Wire via fields
            var comp = go.GetComponent<Lighthouse.PickupInteractable>();
            var t1 = typeof(Lighthouse.PickupInteractable).GetField("itemType",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var t2 = typeof(Lighthouse.PickupInteractable).GetField("amount",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var t3 = typeof(Lighthouse.PickupInteractable).GetField("visualToHide",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            t1?.SetValue(comp, type); t2?.SetValue(comp, amount); t3?.SetValue(comp, go);
        }

        public Vector3 GetPlayerSpawnPosition()
        {
            if (playerSpawn != null) return playerSpawn.position;
            return new Vector3(6f, 1.2f, -8f);
        }
    }
}
