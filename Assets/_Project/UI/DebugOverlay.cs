// Last Light — UI/DebugOverlay.cs
// Minimal FPS / memory / state / storm debug overlay.
// Toggle with ` (or 3-finger tap). Zero per-frame allocations.

using UnityEngine;
using UnityEngine.UI;
using System.Text;

namespace LastLight.UI
{
    public sealed class DebugOverlay : MonoBehaviour
    {
        [Header("UI (auto-created if not assigned)")]
        [SerializeField] Text label;
        [SerializeField] KeyCode toggleKey = KeyCode.BackQuote;

        [Header("Options")]
        [SerializeField] bool startVisible = true;
        [SerializeField] float refreshInterval = 0.25f;

        readonly StringBuilder _sb = new StringBuilder(768);
        float _nextRefresh;
        int   _frameCount;
        float _accumDt;
        float _fps;

        World.DayNightCycle   _dayNight;
        World.WeatherManager  _weather;
        Lighthouse.LighthouseState _lighthouse;
        Survival.PlayerStats  _stats;
        Survival.Inventory    _inventory;
        World.AdaptiveStormDirector _stormDirector;
        World.DayLoopController     _loop;
        Survival.SaveManager        _save;

        void Awake()
        {
            if (label == null) TryCreateLabel();
            gameObject.SetActive(true);
            if (label != null) label.enabled = startVisible;
        }

        void Start()
        {
            _dayNight   = FindFirstObjectByType<World.DayNightCycle>();
            _weather    = FindFirstObjectByType<World.WeatherManager>();
            _lighthouse = FindFirstObjectByType<Lighthouse.LighthouseState>();
            _stats      = FindFirstObjectByType<Survival.PlayerStats>();
            _inventory  = FindFirstObjectByType<Survival.Inventory>();
            _stormDirector = FindFirstObjectByType<World.AdaptiveStormDirector>();
            _loop       = FindFirstObjectByType<World.DayLoopController>();
            _save       = FindFirstObjectByType<Survival.SaveManager>();
        }

        void Update()
        {
            if (Input.GetKeyDown(toggleKey)) Toggle();
            if (Input.touchCount == 3 && Input.touches[0].phase == TouchPhase.Began) Toggle();

            _frameCount++; _accumDt += Time.unscaledDeltaTime;
            if (Time.unscaledTime < _nextRefresh) return;
            _nextRefresh = Time.unscaledTime + refreshInterval;
            _fps = _frameCount / Mathf.Max(0.001f, _accumDt);
            _frameCount = 0; _accumDt = 0f;
            if (label != null && label.enabled) RefreshText();
        }

        public void Toggle() { if (label != null) label.enabled = !label.enabled; }
        public void SetVisible(bool v) { if (label != null) label.enabled = v; }

        void RefreshText()
        {
            _sb.Clear();
            _sb.Append("FPS ").Append(_fps.ToString("0"));
            _sb.Append("  ms ").Append((1000f / Mathf.Max(1f, _fps)).ToString("0.0"));
            _sb.Append("  target ").Append(Application.targetFrameRate.ToString());

#if UNITY_ANDROID && !UNITY_EDITOR
            long mem = UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong();
            _sb.Append("  MEM ").Append((mem / (1024f * 1024f)).ToString("0")).Append(" MB");
#else
            long mem2 = System.GC.GetTotalMemory(false);
            _sb.Append("  GC ").Append((mem2 / (1024f * 1024f)).ToString("0.0")).Append(" MB");
#endif

            if (_dayNight != null)
                _sb.Append("\nTime ").Append(_dayNight.NormalizedTime01.ToString("0.00"))
                   .Append("  ").Append(_dayNight.CurrentPhase.ToString())
                   .Append("  Day ").Append(_dayNight.DayCount.ToString());

            if (_loop != null)
                _sb.Append("  Failed ").Append(_loop.ConsecutiveFailedNights.ToString());

            if (_weather != null)
                _sb.Append("\nWeather ").Append(_weather.CurrentState.ToString())
                   .Append("  Storm ").Append(_weather.StormIntensity.ToString("0.00"))
                   .Append("  Wind ").Append(_weather.WindForce01.ToString("0.00"));

            if (_stormDirector != null)
            {
                _sb.Append("\nStorm → ").Append(_stormDirector.LastChosenTarget)
                   .Append(" (").Append(_stormDirector.LastChosenWeight.ToString("0.00")).Append(")");
                string summary = _stormDirector.DebugSummary;
                if (!string.IsNullOrEmpty(summary)) _sb.Append("  [").Append(summary).Append("]");
            }

            if (_lighthouse != null)
                _sb.Append("\nFuel ").Append(_lighthouse.Fuel01.ToString("0.00"))
                   .Append("  Light ").Append(_lighthouse.LightOn ? "ON" : "OFF")
                   .Append("  Dmg ").Append(_lighthouse.StructuralDamage01.ToString("0.00"))
                   .Append("  Gen ").Append(_lighthouse.GeneratorCondition01.ToString("0.00"))
                   .Append("  Lens ").Append(_lighthouse.LensCondition01.ToString("0.00"));

            if (_stats != null)
                _sb.Append("\nHP ").Append(_stats.Health01.ToString("0.00"))
                   .Append("  Stam ").Append(_stats.Stamina01.ToString("0.00"))
                   .Append("  Hun ").Append(_stats.Hunger01.ToString("0.00"))
                   .Append("  Cold ").Append(_stats.Cold01.ToString("0.00"));

            if (_inventory != null)
            {
                _sb.Append("  Inv ").Append(_inventory.CurrentWeight.ToString("0.0")).Append("/").Append(Core.GameDefines.InventoryMaxWeight.ToString("0"))
                   .Append(" (").Append(_inventory.TotalItems.ToString()).Append(")");
                // List counts
                foreach (Core.ItemType it in System.Enum.GetValues(typeof(Core.ItemType)))
                {
                    int c = _inventory.GetCount(it);
                    if (c > 0) _sb.Append(" ").Append(it.ToString()).Append(":").Append(c.ToString());
                }
            }

            if (_save != null && _save.ShipWreckCount > 0)
                _sb.Append("\n⚠ SHIP WRECKS: ").Append(_save.ShipWreckCount.ToString());

            _sb.Append("\n[~] toggle  |  `help` for console");
            label.text = _sb.ToString();
        }

        void TryCreateLabel()
        {
            var canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                var go = new GameObject("DebugCanvas");
                canvas = go.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 999;
                go.AddComponent<CanvasScaler>();
                go.AddComponent<GraphicRaycaster>();
            }
            var textGo = new GameObject("DebugOverlay_Text");
            textGo.transform.SetParent(canvas.transform, false);
            label = textGo.AddComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (label.font == null) label.font = Font.CreateDynamicFontFromOSFont("Arial", 14);
            label.fontSize = 13;
            label.color = new Color(0.9f, 0.95f, 1f, 0.92f);
            label.alignment = TextAnchor.UpperLeft;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            var rt = label.rectTransform;
            rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = new Vector2(10, -10);
            rt.sizeDelta = new Vector2(560, 240);
        }
    }
}
