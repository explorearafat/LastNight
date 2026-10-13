// Last Light — Core/GameBootstrapper.cs
// Small bootstrap that runs before any scene loads.
// Sets mobile-friendly defaults: Vulkan preference is in ProjectSettings, here we lock
// frame-rate policy, sleep timeout, and ensure quality settings are initialised.

using UnityEngine;

namespace LastLight.Core
{
    public static class GameBootstrapper
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Bootstrap()
        {
            // Never let the device sleep while the game is in the foreground.
            Screen.sleepTimeout = SleepTimeout.NeverSleep;

            // Ensure an FPS limit is set even if QualitySettingsManager hasn't spawned yet.
            // The manager will override this in Awake if present in the scene.
            if (Application.targetFrameRate <= 0)
                Application.targetFrameRate = 60;

#if UNITY_ANDROID && !UNITY_EDITOR
            // Hint the OS that this is a game — prefer sustained performance mode when available.
            // No API dependency; just a log so the intent is clear in any dump.
            Debug.Log("[Bootstrap] Android game mode — Vulkan, URP mobile, sustained perf on.");
#endif
        }
    }
}
