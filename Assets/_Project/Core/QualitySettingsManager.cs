// Last Light — Core/QualitySettingsManager.cs
// Runtime quality switcher for Android. Applies a preset in one call and
// persists the choice in PlayerPrefs ("LL_Quality").

using UnityEngine;

namespace LastLight.Core
{
    public sealed class QualitySettingsManager : Singleton<QualitySettingsManager>
    {
        const string PrefsKey = "LL_Quality";

        [Header("Presets")]
        [SerializeField] QualityPreset defaultPreset = QualityPreset.Medium;

        public QualityPreset Current { get; private set; }
        public int TargetFrameRate { get; private set; } = 60;

        protected override void Awake()
        {
            base.Awake();
            int saved = PlayerPrefs.GetInt(PrefsKey, (int)defaultPreset);
            Apply((QualityPreset)Mathf.Clamp(saved, 0, 2));
        }

        /// <summary>Apply a quality preset and persist it.</summary>
        public void Apply(QualityPreset preset)
        {
            Current = preset;
            PlayerPrefs.SetInt(PrefsKey, (int)preset);

            switch (preset)
            {
                case QualityPreset.Low:
                    QualitySettings.SetQualityLevel(0, true);
                    Application.targetFrameRate = 30;
                    TargetFrameRate = 30;
                    break;
                case QualityPreset.Medium:
                    QualitySettings.SetQualityLevel(1, true);
                    Application.targetFrameRate = 60;
                    TargetFrameRate = 60;
                    break;
                case QualityPreset.High:
                    QualitySettings.SetQualityLevel(2, true);
                    Application.targetFrameRate = 60;
                    TargetFrameRate = 60;
                    break;
            }
            // Keep vSync off on mobile so targetFrameRate is authoritative.
            QualitySettings.vSyncCount = 0;
            Debug.Log($"[Quality] Applied {preset} — target {TargetFrameRate} FPS");
        }

        public void CycleNext() => Apply((QualityPreset)(((int)Current + 1) % 3));
    }
}
