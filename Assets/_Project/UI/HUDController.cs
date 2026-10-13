// Last Light — UI/HUDController.cs
// Minimal HUD: no bars. Shows survival state through world/character feedback.
// This script owns the tiny optional prompts (e.g. low-fuel warning as a diegetic light flicker hint)
// and the interact prompt already handled by TouchInput. Everything else is in PlayerVisuals.
// A single subtle vignette + low-fuel text is the only persistent HUD element, and it's hidden
// by default — DebugOverlay is the real status display.

using UnityEngine;
using UnityEngine.UI;

namespace LastLight.UI
{
    public sealed class HUDController : MonoBehaviour
    {
        [Header("Optional — assign if you add a vignette Image / warning Text")]
        [SerializeField] Image vignette;
        [SerializeField] Text  warningLabel;

        [Header("Thresholds")]
        [SerializeField] float lowFuelThreshold = 0.18f;
        [SerializeField] float criticalColdThreshold = 0.75f;

        Lighthouse.LighthouseState _lighthouse;
        Survival.PlayerStats       _stats;

        void Start()
        {
            _lighthouse = FindFirstObjectByType<Lighthouse.LighthouseState>();
            _stats      = FindFirstObjectByType<Survival.PlayerStats>();
            if (warningLabel != null) warningLabel.enabled = false;
        }

        void Update()
        {
            // Vignette intensity based on cold + low fuel.
            if (vignette != null)
            {
                float coldVignette = _stats != null ? Mathf.InverseLerp(criticalColdThreshold, 1f, _stats.Cold01) : 0f;
                float fuelVignette = _lighthouse != null ? Mathf.InverseLerp(lowFuelThreshold, 0f, _lighthouse.Fuel01) : 0f;
                float alpha = Mathf.Clamp01(Mathf.Max(coldVignette * 0.55f, fuelVignette * 0.35f));
                var c = vignette.color; c.a = alpha; vignette.color = c;
            }

            if (warningLabel != null && _lighthouse != null)
            {
                bool show = _lighthouse.Fuel01 < lowFuelThreshold && _lighthouse.LightOn;
                warningLabel.enabled = show;
                if (show) warningLabel.text = "Fuel low — light failing…";
            }
        }
    }
}
