// Last Light — World/DamageableStructure.cs
// Diegetic "damage report" — cracks, leaks and lens state shown through the world.
// Other systems write into this; the lighthouse model / VFX reads from it.
// No menu damage report — the player sees cracks and water, not a health bar.

using UnityEngine;
using System.Collections.Generic;

namespace LastLight.World
{
    public sealed class DamageableStructure : MonoBehaviour
    {
        [Header("Visuals (optional — assign to see damage)")]
        [SerializeField] List<GameObject> crackDecals = new(); // enable progressively with structural damage
        [SerializeField] GameObject waterLeakVFX;
        [SerializeField] GameObject lensCrackVFX;
        [SerializeField] Light      interiorLight;

        [Header("Boarded windows")]
        readonly Dictionary<string, bool> _boardedWindows = new();

        bool _hasWaterLeak;
        bool _hasLensCrack;

        // --- Query ---
        public bool HasWaterLeak  => _hasWaterLeak;
        public bool HasLensCrack  => _hasLensCrack;
        public bool IsBoarded(string id) => _boardedWindows.TryGetValue(id, out bool v) && v;

        public void SetBoarded(string id, bool v)
        {
            _boardedWindows[id] = v;
        }

        public void SetWaterLeak(bool v)
        {
            _hasWaterLeak = v;
            if (waterLeakVFX != null) waterLeakVFX.SetActive(v);
        }

        public void SetLensCrack(bool v)
        {
            _hasLensCrack = v;
            if (lensCrackVFX != null) lensCrackVFX.SetActive(v);
        }

        /// <summary>Show structural damage diegetically: enable crack decals by damage01.</summary>
        public void RefreshStructuralVisuals(float damage01)
        {
            if (crackDecals.Count == 0) return;
            int toShow = Mathf.RoundToInt(damage01 * crackDecals.Count);
            for (int i = 0; i < crackDecals.Count; i++)
                if (crackDecals[i] != null) crackDecals[i].SetActive(i < toShow);
            if (interiorLight != null)
                interiorLight.intensity = Mathf.Lerp(1f, 0.3f, damage01);
        }

        public Dictionary<string, bool> SnapshotBoarded() => new(_boardedWindows);

        public void RestoreBoarded(Dictionary<string, bool> data)
        {
            _boardedWindows.Clear();
            if (data == null) return;
            foreach (var kv in data) _boardedWindows[kv.Key] = kv.Value;
            foreach (var kv in _boardedWindows)
            {
                // Find matching BoardWindowInteractable to sync visual.
                var boards = FindObjectsByType<Lighthouse.BoardWindowInteractable>(FindObjectsSortMode.None);
                foreach (var b in boards) if (b.WindowId == kv.Key) b.RestoreBoarded(kv.Value);
            }
        }
    }
}
