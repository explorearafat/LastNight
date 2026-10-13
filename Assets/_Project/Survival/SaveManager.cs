// Last Light — Survival/SaveManager.cs
// Auto-save at each Dusk to a single JSON file in Application.persistentDataPath.
// No manual save. Also tracks ship-wreck events persistently.
//
// File: <persistentDataPath>/lastlight_save.json
// Safe to call even when no file exists — it just writes a fresh one.

using UnityEngine;
using System.Collections.Generic;
using System.IO;
using LastLight.Core;

namespace LastLight.Survival
{
    public sealed class SaveManager : MonoBehaviour
    {
        string SavePath => Path.Combine(Application.persistentDataPath, GameDefines.SaveFileName);

        [Header("Debug")]
        [SerializeField] bool logOnSave = true;

        int _shipWrecks;
        readonly List<int> _wreckDays = new();

        // Note collection — persistent across saves (also synced via static for early Awake checks)
        static readonly HashSet<int> _collectedNotes = new();
        public static bool HasCollectedNote(int n) => _collectedNotes.Contains(n);
        public static void CollectNote(int n)
        {
            if (_collectedNotes.Add(n))
            {
                // Persist immediately via instance AutoSave if available
                var inst = FindFirstObjectByType<SaveManager>();
                inst?.AutoSave();
            }
        }



        void Awake()
        {
            // Auto-load on boot so lighthouse structural damage is restored.
            TryLoad();
        }

        public void AutoSave()
        {
            var data = BuildSaveData();
            try
            {
                string dir = Path.GetDirectoryName(SavePath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
                File.WriteAllText(SavePath, JsonUtility.ToJson(data, true));
                if (logOnSave) Debug.Log($"[Save] Auto-saved at Dusk — Day {data.dayNumber} → {SavePath}");
            }
            catch (System.Exception ex) { Debug.LogWarning($"[Save] Failed to write {SavePath}: {ex.Message}"); }
        }

        public bool TryLoad()
        {
            if (!File.Exists(SavePath)) return false;
            try
            {
                string json = File.ReadAllText(SavePath);
                var data = JsonUtility.FromJson<SaveFile>(json);
                ApplySaveData(data);
                Debug.Log($"[Save] Loaded Day {data.dayNumber} from {SavePath}");
                return true;
            }
            catch (System.Exception ex) { Debug.LogWarning($"[Save] Corrupt save — starting fresh. {ex.Message}"); return false; }
        }

        public void DeleteSave()
        {
            if (File.Exists(SavePath)) File.Delete(SavePath);
            PlayerPrefs.DeleteKey("LL_Lighthouse"); // legacy key — no longer used, but clean up
            Debug.Log("[Save] Save deleted.");
        }

        public void RecordShipWreck(int day)
        {
            _shipWrecks++;
            _wreckDays.Add(day);
            // Write immediately — don't wait for Dusk.
            AutoSave();
        }

        public int ShipWreckCount => _shipWrecks;

        SaveFile BuildSaveData()
        {
            var dnc    = FindFirstObjectByType<World.DayNightCycle>();
            var weather= FindFirstObjectByType<World.WeatherManager>();
            var loop   = FindFirstObjectByType<World.DayLoopController>();
            var lh     = FindFirstObjectByType<Lighthouse.LighthouseState>();
            var stats  = FindFirstObjectByType<PlayerStats>();
            var inv    = FindFirstObjectByType<Inventory>();
            var structure = FindFirstObjectByType<World.DamageableStructure>();
            var qm     = FindFirstObjectByType<QualitySettingsManager>();

            var data = new SaveFile
            {
                version      = 3,
                savedAtUtc   = System.DateTime.UtcNow.ToString("o"),
                normalizedTime01 = dnc != null ? dnc.NormalizedTime01 : 0.35f,
                weatherState   = weather != null ? weather.CurrentState : WeatherState.Clear,
                stormIntensity = weather != null ? weather.StormIntensity : 0f,
                dayNumber      = loop != null ? loop.DayNumber : 1,
                failedNights   = loop != null ? loop.ConsecutiveFailedNights : 0,
                fuel           = lh != null ? lh.Fuel01 * GameDefines.LighthouseMaxFuel : 65f,
                generatorCondition = lh != null ? lh.GeneratorCondition01 : 1f,
                lensCondition      = lh != null ? lh.LensCondition01 : 1f,
                structuralDamage   = lh != null ? lh.StructuralDamage01 : 0f,
                health         = stats != null ? stats.Health01 * 100f : 100f,
                stamina        = stats != null ? stats.Stamina01 * 100f : 100f,
                hunger         = stats != null ? stats.Hunger01 * 100f : 80f,
                cold           = stats != null ? stats.Cold01 * 100f : 0f,
                qualityPreset  = qm != null ? (int)qm.Current : 1,
                shipWrecks     = _shipWrecks,
                wreckDaysCsv   = string.Join(",", _wreckDays),
                collectedNotesCsv = string.Join(",", _collectedNotes),
            };

            // Inventory
            if (inv != null)
            {
                var snap = inv.Snapshot();
                var invList = new List<InvEntry>();
                foreach (var kv in snap) invList.Add(new InvEntry { item = kv.Key.ToString(), count = kv.Value });
                data.inventory = invList.ToArray();
            }

            // Boarded windows
            if (structure != null)
            {
                var boarded = structure.SnapshotBoarded();
                var bw = new List<BoardedEntry>();
                foreach (var kv in boarded) bw.Add(new BoardedEntry { id = kv.Key, boarded = kv.Value });
                data.boardedWindows = bw.ToArray();
                data.hasWaterLeak = structure.HasWaterLeak;
                data.hasLensCrack = structure.HasLensCrack;
            }

            return data;
        }

        void ApplySaveData(SaveFile data)
        {
            var dnc    = FindFirstObjectByType<World.DayNightCycle>();
            var weather= FindFirstObjectByType<World.WeatherManager>();
            var loop   = FindFirstObjectByType<World.DayLoopController>();
            var lh     = FindFirstObjectByType<Lighthouse.LighthouseState>();
            var stats  = FindFirstObjectByType<PlayerStats>();
            var inv    = FindFirstObjectByType<Inventory>();
            var structure = FindFirstObjectByType<World.DamageableStructure>();
            var qm     = FindFirstObjectByType<QualitySettingsManager>();

            if (dnc != null) dnc.SetTime01(data.normalizedTime01);
            if (weather != null) weather.SnapWeather(data.weatherState, data.stormIntensity);
            if (loop != null) loop.RestoreLoopState(data.dayNumber, data.failedNights);
            if (lh != null) lh.RestoreFromSave(data.fuel, data.generatorCondition, data.lensCondition, data.structuralDamage);
            if (stats != null) stats.RestoreFromSave(data.health, data.stamina, data.hunger, data.cold);
            if (qm != null) qm.Apply((QualityPreset)Mathf.Clamp(data.qualityPreset, 0, 2));

            if (inv != null && data.inventory != null)
            {
                var dict = new Dictionary<ItemType, int>();
                foreach (var e in data.inventory)
                    if (System.Enum.TryParse<ItemType>(e.item, out var t)) dict[t] = e.count;
                inv.Restore(dict);
            }

            if (structure != null && data.boardedWindows != null)
            {
                var dict = new Dictionary<string, bool>();
                foreach (var e in data.boardedWindows) dict[e.id] = e.boarded;
                structure.RestoreBoarded(dict);
                structure.SetWaterLeak(data.hasWaterLeak);
                structure.SetLensCrack(data.hasLensCrack);
            }

            _shipWrecks = data.shipWrecks;
            _wreckDays.Clear();
            if (!string.IsNullOrEmpty(data.wreckDaysCsv))
                foreach (var s in data.wreckDaysCsv.Split(',')) if (int.TryParse(s, out int d)) _wreckDays.Add(d);
            _collectedNotes.Clear();
            if (!string.IsNullOrEmpty(data.collectedNotesCsv))
                foreach (var s in data.collectedNotesCsv.Split(',')) if (int.TryParse(s, out int n)) _collectedNotes.Add(n);
        }

        [System.Serializable]
        class SaveFile
        {
            public int version;
            public string savedAtUtc;
            public float normalizedTime01;
            public WeatherState weatherState;
            public float stormIntensity;
            public int   dayNumber, failedNights;
            public float fuel, generatorCondition, lensCondition, structuralDamage;
            public float health, stamina, hunger, cold;
            public int   qualityPreset;
            public InvEntry[] inventory;
            public BoardedEntry[] boardedWindows;
            public bool hasWaterLeak, hasLensCrack;
            public int shipWrecks;
            public string wreckDaysCsv;
            public string collectedNotesCsv;
        }

        [System.Serializable]
        class InvEntry { public string item; public int count; }

        [System.Serializable]
        class BoardedEntry { public string id; public bool boarded; }
    }
}
