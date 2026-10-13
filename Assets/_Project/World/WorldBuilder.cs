// Last Light — World/WorldBuilder.cs
// Spawns a minimal placeholder island so the game is playable before any art is imported.
// Creates: ground plane, a few rocks, a lighthouse cylinder + beacon light, and spawn point.
// All placeholder meshes use Unity primitives and a single shared URP Lit material.

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

        [Header("Refs (auto-found if null)")]
        [SerializeField] Transform playerSpawn;
        [SerializeField] Transform lighthouseRoot;

        // Keep refs so we can clean up on re-enter play mode without duplicates
        GameObject _ground, _lighthouseObj;

        void Awake() => BuildIfNeeded();

        void BuildIfNeeded()
        {
            // Don't rebuild if the scene already has an Island marker
            if (GameObject.Find("Island_Ground") != null) return;
            Build();
        }

        void Build()
        {
            _ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            _ground.name = "Island_Ground";
            _ground.transform.position = Vector3.zero;
            _ground.transform.localScale = new Vector3(islandSize.x / 10f, 1f, islandSize.y / 10f);
            if (groundMaterial != null) _ground.GetComponent<Renderer>().sharedMaterial = groundMaterial;

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

            // Lighthouse
            _lighthouseObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            _lighthouseObj.name = "Lighthouse_Tower";
            _lighthouseObj.transform.position = new Vector3(0, 4f, 0);
            _lighthouseObj.transform.localScale = new Vector3(1.2f, 4f, 1.2f);
            if (lighthouseMaterial != null) _lighthouseObj.GetComponent<Renderer>().sharedMaterial = lighthouseMaterial;

            // Lantern head
            var head = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            head.name = "Lighthouse_Lantern";
            head.transform.SetParent(_lighthouseObj.transform, true);
            head.transform.localPosition = new Vector3(0, 1.05f, 0);
            head.transform.localScale = new Vector3(1.05f, 0.15f, 1.05f);

            // Beacon light
            var beaconGo = new GameObject("Beacon_Light");
            beaconGo.transform.SetParent(head.transform, false);
            beaconGo.transform.localPosition = Vector3.up * 0.5f;
            var beacon = beaconGo.AddComponent<Light>();
            beacon.type = LightType.Spot;
            beacon.spotAngle = 42f;
            beacon.intensity = 0f; // LighthouseState will drive it
            beacon.color = new Color(1f, 0.92f, 0.72f);
            beacon.shadows = LightShadows.None; // save GPU until player nears
            beaconGo.AddComponent<LastLight.Lighthouse.LighthouseState>(); // attach directly to the beacon for Day-1 wiring
        }

        // Expose spawn
        public Vector3 GetPlayerSpawnPosition()
        {
            if (playerSpawn != null) return playerSpawn.position;
            return new Vector3(6f, 1.2f, -8f);
        }
    }
}
