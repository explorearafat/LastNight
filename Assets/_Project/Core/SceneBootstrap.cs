// Last Light — Core/SceneBootstrap.cs
// Drop one of these in the Island scene. It spawns every manager + the player
// if they are not already present, so the scene is playable before you place prefabs.
// Safe to leave in the scene permanently — every Ensure* no-ops when its target exists.

using UnityEngine;

namespace LastLight.Core
{
    public sealed class SceneBootstrap : MonoBehaviour
    {
        [Header("Prefabs (optional — falls back to primitives)")]
        [SerializeField] GameObject playerPrefab;
        [SerializeField] GameObject charactersRootPrefab;

        [Header("World Config (optional — assign a WorldConfig asset)")]
        [SerializeField] World.WorldConfig worldConfig;

        void Awake()
        {
            EnsureManager<LastLight.World.DayNightCycle>("DayNight");
            EnsureManager<LastLight.World.WeatherManager>("Weather");
            EnsureManager<LastLight.World.LightingManager>("Lighting");
            EnsureManager<LastLight.World.WeatherVFXManager>("WeatherVFX");
            EnsureManager<LastLight.World.OceanController>("Ocean");
            EnsureManager<LastLight.Audio.AudioManager>("AudioManager");
            EnsureManager<QualitySettingsManager>("Quality");
            EnsureManager<LastLight.World.WorldQualitySettings>("WorldQuality");
            EnsureManager<LastLight.Survival.SaveManager>("SaveManager");
            EnsureManager<LastLight.World.DayLoopController>("DayLoop");
            EnsureManager<LastLight.World.AdaptiveStormDirector>("AdaptiveStorm");
            EnsureManager<LastLight.UI.DebugOverlay>("DebugOverlay");
            EnsureManager<LastLight.UI.DebugConsole>("DebugConsole");
            EnsureManager<LastLight.UI.NotePresenter>("NotePresenter");

            // Wire WorldConfig into quality/world if assigned
            if (worldConfig != null)
            {
                var wq = FindFirstObjectByType<LastLight.World.WorldQualitySettings>();
                if (wq != null)
                {
                    var fi = typeof(LastLight.World.WorldQualitySettings).GetField("config",
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    fi?.SetValue(wq, worldConfig);
                }
                var layout = FindFirstObjectByType<LastLight.World.IslandLayout>();
                if (layout != null)
                {
                    var fi2 = typeof(LastLight.World.IslandLayout).GetField("config",
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    fi2?.SetValue(layout, worldConfig);
                }
            }

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
            if (FindFirstObjectByType<LastLight.World.IslandLayout>() == null
             && FindFirstObjectByType<LastLight.World.WorldBuilder>() == null)
            {
                var go = new GameObject("IslandLayout");
                go.AddComponent<LastLight.World.IslandLayout>();
            }
            // Legacy WorldBuilder still covers the lighthouse tower detail + gatherables + interiors
            if (FindFirstObjectByType<LastLight.World.WorldBuilder>() == null)
            {
                var go = new GameObject("WorldBuilder");
                go.AddComponent<LastLight.World.WorldBuilder>();
            }
            if (FindFirstObjectByType<LastLight.World.LighthouseInterior>() == null)
            {
                var go = new GameObject("LighthouseInterior");
                go.AddComponent<LastLight.World.LighthouseInterior>();
            }
            if (FindFirstObjectByType<LastLight.World.DamageableStructure>() == null)
            {
                // WorldBuilder creates one on the tower; this is a fallback only
                var go = new GameObject("DamageableStructure");
                go.AddComponent<LastLight.World.DamageableStructure>();
            }
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
            else if (charactersRootPrefab != null)
            {
                player = Instantiate(charactersRootPrefab);
                player.name = "Player (Elias)";
                EnsurePlayerComponents(player);
            }
            else
            {
                player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                player.name = "Player (Elias) — Placeholder";
                player.transform.position = new Vector3(6f, 1.2f, -8f);
                Destroy(player.GetComponent<CapsuleCollider>());
                var cc = player.AddComponent<CharacterController>();
                cc.height = 1.8f; cc.radius = 0.32f; cc.center = new Vector3(0, 0.9f, 0);
                cc.slopeLimit = GameDefines.PlayerSlopeLimitDeg;
                cc.stepOffset = GameDefines.PlayerStepHeight;
                EnsurePlayerComponents(player);

                var camGo = new GameObject("Main Camera");
                camGo.transform.SetParent(player.transform, false);
                camGo.transform.localPosition = new Vector3(0, 1.6f, -3.2f);
                var cam = camGo.AddComponent<Camera>();
                cam.fieldOfView = 58f;
                camGo.AddComponent<AudioListener>();
            }
        }

        static void EnsurePlayerComponents(GameObject player)
        {
            if (player.GetComponent<CharacterController>() == null)
            {
                var cc = player.AddComponent<CharacterController>();
                cc.height = 1.8f; cc.radius = 0.32f; cc.center = new Vector3(0, 0.9f, 0);
                cc.slopeLimit = GameDefines.PlayerSlopeLimitDeg;
                cc.stepOffset = GameDefines.PlayerStepHeight;
            }
            if (player.GetComponent<LastLight.Player.PlayerInputActions>() == null)
                player.AddComponent<LastLight.Player.PlayerInputActions>();
            if (player.GetComponent<LastLight.Player.PlayerController>() == null)
                player.AddComponent<LastLight.Player.PlayerController>();
            if (player.GetComponent<LastLight.Player.PlayerVisuals>() == null)
                player.AddComponent<LastLight.Player.PlayerVisuals>();
            if (player.GetComponent<LastLight.Player.CharacterSetup>() == null)
                player.AddComponent<LastLight.Player.CharacterSetup>();
            if (player.GetComponent<LastLight.Audio.FootstepController>() == null)
                player.AddComponent<LastLight.Audio.FootstepController>();
            if (player.GetComponent<LastLight.Survival.PlayerStats>() == null)
                player.AddComponent<LastLight.Survival.PlayerStats>();
            if (player.GetComponent<LastLight.Survival.Inventory>() == null)
                player.AddComponent<LastLight.Survival.Inventory>();
            if (player.GetComponent<LastLight.Player.ThirdPersonCamera>() == null)
                player.AddComponent<LastLight.Player.ThirdPersonCamera>();

            bool hasTouch = player.GetComponentInChildren<LastLight.Player.TouchInput>() != null;
            if (!hasTouch)
            {
                var touchGo = new GameObject("TouchInput");
                touchGo.transform.SetParent(player.transform, false);
                touchGo.AddComponent<LastLight.Player.TouchInput>();
            }
        }
    }
}
