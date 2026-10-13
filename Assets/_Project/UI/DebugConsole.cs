// Last Light — UI/DebugConsole.cs
// Tiny on-screen console for forcing time, weather, inventory, and storm.
// Toggle with BackQuote (`). Commands are parsed case-insensitively.

using UnityEngine;
using System.Collections.Generic;

namespace LastLight.UI
{
    public sealed class DebugConsole : MonoBehaviour
    {
        [SerializeField] KeyCode toggleKey = KeyCode.BackQuote;
        [SerializeField] int maxLogLines = 10;

        bool _open;
        string _input = "";
        readonly List<string> _log = new();
        Vector2 _scroll;
        GUIStyle _bgStyle, _inputStyle, _logStyle;
        bool _stylesBuilt;

        void Update() { if (Input.GetKeyDown(toggleKey)) _open = !_open; }

        void OnGUI()
        {
            if (!_open) return;
            BuildStyles();
            float w = Mathf.Min(560, Screen.width - 20);
            float h = 280f;
            float x = 10, y = Screen.height - h - 10;
            GUI.Box(new Rect(x, y, w, h), "", _bgStyle);
            float logH = h - 36;
            GUILayout.BeginArea(new Rect(x + 8, y + 6, w - 16, logH));
            _scroll = GUILayout.BeginScrollView(_scroll);
            foreach (var line in _log) GUILayout.Label(line, _logStyle);
            GUILayout.EndScrollView();
            GUILayout.EndArea();
            GUI.SetNextControlName("ConsoleInput");
            _input = GUI.TextField(new Rect(x + 8, y + h - 28, w - 90, 22), _input, _inputStyle);
            if (GUI.Button(new Rect(x + w - 78, y + h - 28, 70, 22), "Run")) Submit();
            if (Event.current.isKey && Event.current.keyCode == KeyCode.Return) { Submit(); Event.current.Use(); }
            if (_open) GUI.FocusControl("ConsoleInput");
        }

        void Submit()
        {
            string cmd = _input.Trim();
            if (cmd.Length == 0) return;
            _input = ""; Log($"> {cmd}"); Execute(cmd);
        }

        void Log(string msg)
        {
            _log.Add(msg);
            while (_log.Count > maxLogLines) _log.RemoveAt(0);
            Debug.Log($"[Console] {msg}");
        }

        void Execute(string cmd)
        {
            var parts = cmd.Split(' ');
            string verb = parts[0].ToLowerInvariant();

            switch (verb)
            {
                case "help":
                    Log("time <0-1|dawn|day|dusk|night>");
                    Log("weather <clear|rain|fog|storm> [intensity]  storm <0-1>");
                    Log("fuel <amt>  damage <0-1>  heal  food <amt>");
                    Log("inv <fuel|wood|food|parts> [count]  invclear");
                    Log("save  load  deletesave  wreck  stormlearn");
                    Log("quality <low|med|high>  fps");
                    break;

                case "time":
                    if (parts.Length < 2) { Log("usage: time <0-1|dawn|day|dusk|night>"); break; }
                    var dnc = FindFirstObjectByType<World.DayNightCycle>();
                    if (dnc == null) { Log("No DayNightCycle."); break; }
                    float t;
                    switch (parts[1].ToLowerInvariant())
                    {
                        case "dawn": t = 0.25f; break; case "day": t = 0.50f; break;
                        case "dusk": t = 0.75f; break; case "night": t = 0.00f; break;
                        default: if (!float.TryParse(parts[1], out t)) { Log("Invalid time."); break; } break;
                    }
                    dnc.SetTime01(t); Log($"Time → {t:0.00}"); break;

                case "weather":
                    if (parts.Length < 2) { Log("usage: weather <clear|rain|fog|storm> [intensity]"); break; }
                    var wm = FindFirstObjectByType<World.WeatherManager>();
                    if (wm == null) { Log("No WeatherManager."); break; }
                    if (!System.Enum.TryParse<Core.WeatherState>(parts[1], true, out var ws)) { Log("Unknown weather."); break; }
                    float intensity = 0.7f; if (parts.Length >= 3) float.TryParse(parts[2], out intensity);
                    if (ws == Core.WeatherState.Storm) wm.SetWeather(ws, Mathf.Clamp01(intensity)); else wm.SetWeather(ws);
                    Log($"Weather → {ws} ({intensity:0.00})"); break;

                case "storm":
                    { var w2 = FindFirstObjectByType<World.WeatherManager>(); if (w2 == null) { Log("No WeatherManager."); break; }
                      float v = 0.7f; if (parts.Length >= 2) float.TryParse(parts[1], out v);
                      w2.SetWeather(Core.WeatherState.Storm, Mathf.Clamp01(v)); Log($"Storm → {v:0.00}"); break; }

                case "fuel":
                    { var lh = FindFirstObjectByType<Lighthouse.LighthouseState>(); if (lh == null) { Log("No LighthouseState."); break; }
                      float v = 30f; if (parts.Length >= 2) float.TryParse(parts[1], out v); lh.Refuel(v); Log($"Fuel +{v:0.0}"); break; }

                case "damage":
                    { var lh2 = FindFirstObjectByType<Lighthouse.LighthouseState>(); if (lh2 == null) { Log("No LighthouseState."); break; }
                      float v = 0.1f; if (parts.Length >= 2) float.TryParse(parts[1], out v); lh2.AddStructuralDamage(Mathf.Clamp01(v)); Log($"Damage +{v:0.00}"); break; }

                case "heal":
                    { var st = FindFirstObjectByType<Survival.PlayerStats>(); if (st == null) { Log("No PlayerStats."); break; }
                      st.RestoreHealth(100f); st.RestoreHunger(100f); Log("Player healed."); break; }

                case "food":
                    { var st2 = FindFirstObjectByType<Survival.PlayerStats>(); if (st2 == null) { Log("No PlayerStats."); break; }
                      float v = 30f; if (parts.Length >= 2) float.TryParse(parts[1], out v); st2.RestoreHunger(v); Log($"Hunger +{v:0.0}"); break; }

                case "inv":
                    { var inv = FindFirstObjectByType<Survival.Inventory>(); if (inv == null) { Log("No Inventory."); break; }
                      if (parts.Length < 2) { Log("usage: inv <fuel|wood|food|parts> [count]"); break; }
                      if (!System.Enum.TryParse<Core.ItemType>(parts[1], true, out var it)) { Log("Unknown item."); break; }
                      int c = 3; if (parts.Length >= 3) int.TryParse(parts[2], out c);
                      int added = inv.TryAdd(it, c); Log($"{it} +{added}/{c}"); break; }

                case "invclear":
                    { var inv2 = FindFirstObjectByType<Survival.Inventory>(); inv2?.Clear(); Log("Inventory cleared."); break; }

                case "save":
                    FindFirstObjectByType<Survival.SaveManager>()?.AutoSave(); Log("Saved."); break;

                case "load":
                    { bool ok = FindFirstObjectByType<Survival.SaveManager>()?.TryLoad() ?? false; Log(ok ? "Loaded." : "No save."); break; }

                case "deletesave":
                    FindFirstObjectByType<Survival.SaveManager>()?.DeleteSave(); Log("Save deleted."); break;

                case "wreck":
                    FindFirstObjectByType<World.DayLoopController>()?.GetType().GetMethod("TriggerShipWreck",
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                        ?.Invoke(FindFirstObjectByType<World.DayLoopController>(), null);
                    Log("Ship wreck triggered."); break;

                case "stormlearn":
                    { var sd = FindFirstObjectByType<World.AdaptiveStormDirector>();
                      if (sd == null) { Log("No AdaptiveStormDirector."); break; }
                      Log($"Target: {sd.LastChosenTarget} ({sd.LastChosenWeight:0.00})");
                      Log($"Weights: {sd.DebugSummary}"); break; }

                case "quality":
                    { var qm = FindFirstObjectByType<Core.QualitySettingsManager>(); if (qm == null) { Log("No QualitySettingsManager."); break; }
                      string q = parts.Length >= 2 ? parts[1].ToLowerInvariant() : "";
                      Core.QualityPreset preset = q == "low" ? Core.QualityPreset.Low : q == "high" ? Core.QualityPreset.High : Core.QualityPreset.Medium;
                      qm.Apply(preset); Log($"Quality → {preset}"); break; }

                case "fps":
                    Log($"FPS {1f / Mathf.Max(0.001f, Time.unscaledDeltaTime):0}  target {Application.targetFrameRate}"); break;

                default: Log($"Unknown: {verb}  (try 'help')"); break;
            }
        }

        void BuildStyles()
        {
            if (_stylesBuilt) return; _stylesBuilt = true;
            _bgStyle    = new GUIStyle(GUI.skin.box) { normal = { background = MakeTex(new Color(0.08f, 0.09f, 0.12f, 0.92f)) } };
            _inputStyle = new GUIStyle(GUI.skin.textField) { fontSize = 13 };
            _logStyle   = new GUIStyle(GUI.skin.label) { fontSize = 12, normal = { textColor = new Color(0.88f, 0.92f, 1f) } };
        }

        static Texture2D MakeTex(Color c)
        {
            var t = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            t.SetPixel(0, 0, c); t.Apply(); return t;
        }
    }
}
