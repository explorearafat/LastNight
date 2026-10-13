// Last Light — Core/SceneBootstrap.cs
// One GameObject to drop in the Island scene that spawns all managers and the player
// if they are not already present. Lets the scene work even before you manually
// place prefabs. Safe to leave in the scene — it no-ops when managers already exist.

using UnityEngine;

namespace LastLight.Core
{
    public sealed class SceneBootstrap : MonoBehaviour
    {
        [Header("Prefabs (optional — falls back to primitives)")]
        [SerializeField] GameObject playerPrefab;

        void Awake()
        {
            EnsureManager<LastLight.World.DayNightCycle>("DayNight");
            EnsureManager<LastLight.World.WeatherManager>("Weather");
            EnsureManager<QualitySettingsManager>("Quality");
            EnsureManager<LastLight.UI.DebugOverlay>("DebugOverlay");
            EnsureManager<LastLight.UI.DebugConsole>("DebugConsole");

            EnsureWorld();
            EnsurePlayer();
        }

        void EnsureManager<T>(string name) where T : Component
        {
            if (FindFirstObjectByType<T>() != null) return;
            var go = new GameObject(name);
            go.AddComponent<T>();
        }

        void EnsureWorld()
        {
            if (FindFirstObjectByType<LastLight.World.WorldBuilder>() != null) return;
            var go = new GameObject("WorldBuilder");
            go.AddComponent<LastLight.World.WorldBuilder>();
        }

        void EnsurePlayer()
        {
            if (FindFirstObjectByType<LastLight.Player.PlayerController>() != null) return;

            GameObject player;
            if (playerPrefab != null)
            {
                player = Instantiate(playerPrefab);
                player.name = "Player (Elias)";
            }
            else
            {
                player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                player.name = "Player (Elias) — Placeholder";
                player.transform.position = new Vector3(6f, 1.2f, -8f);
                // Remove capsule collider — CharacterController is the collider
                Destroy(player.GetComponent<CapsuleCollider>());

                var cc = player.AddComponent<CharacterController>();
                cc.height = 1.8f; cc.radius = 0.32f; cc.center = new Vector3(0, 0.9f, 0);
                cc.slopeLimit = GameDefines.PlayerSlopeLimitDeg;
                cc.stepOffset = GameDefines.PlayerStepHeight;

                player.AddComponent<LastLight.Player.PlayerInputActions>();
                player.AddComponent<LastLight.Player.PlayerController>();
                player.AddComponent<LastLight.Player.PlayerVisuals>();
                player.AddComponent<LastLight.Survival.PlayerStats>();

                // Camera rig as child
                var camGo = new GameObject("Main Camera");
                camGo.transform.SetParent(player.transform, false);
                camGo.transform.localPosition = new Vector3(0, 1.6f, -3.2f);
                var cam = camGo.AddComponent<Camera>();
                cam.fieldOfView = 58f;
                camGo.AddComponent<AudioListener>();
                player.AddComponent<LastLight.Player.ThirdPersonCamera>();

                // Touch input lives on a separate canvas object
                var touchGo = new GameObject("TouchInput");
                touchGo.transform.SetParent(player.transform, false);
                touchGo.AddComponent<LastLight.Player.TouchInput>();
            }
        }
    }
}
