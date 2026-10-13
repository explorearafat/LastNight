// Last Light — UI/NotePresenter.cs
// Diegetic note UI — a single overlay panel that shows a WorldNoteData's text.
// No menu state machine — the note just appears over the world and fades.

using UnityEngine;
using UnityEngine.UI;

namespace LastLight.UI
{
    public sealed class NotePresenter : MonoBehaviour
    {
        [Header("UI (auto-created if missing)")]
        [SerializeField] CanvasGroup panel;
        [SerializeField] Text titleText;
        [SerializeField] Text bodyText;
        [SerializeField] Button closeButton;

        [Header("Timing")]
        [SerializeField] float fadeSpeed = 4f;
        [SerializeField] float autoHideAfter = 0f; // 0 = manual close only

        float _targetAlpha;
        float _autoHideTimer;

        void Awake()
        {
            if (panel == null) TryCreatePanel();
            if (closeButton != null) closeButton.onClick.AddListener(Hide);
            _targetAlpha = 0f;
            if (panel != null) panel.alpha = 0f;
        }

        void Update()
        {
            if (panel == null) return;
            panel.alpha = Mathf.MoveTowards(panel.alpha, _targetAlpha, Time.deltaTime * fadeSpeed);
            panel.interactable   = panel.alpha > 0.5f;
            panel.blocksRaycasts = panel.alpha > 0.5f;

            if (_targetAlpha > 0.5f && autoHideAfter > 0f)
            {
                _autoHideTimer -= Time.deltaTime;
                if (_autoHideTimer <= 0f) Hide();
            }
        }

        public void Show(World.WorldNoteData note)
        {
            if (note == null) return;
            if (titleText != null) titleText.text = $"#{note.noteNumber:00}  {note.noteTitle}";
            if (bodyText  != null) bodyText.text  = note.bodyText;
            _targetAlpha = 1f;
            _autoHideTimer = autoHideAfter;
        }

        public void Hide() => _targetAlpha = 0f;

        void TryCreatePanel()
        {
            var canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                var go = new GameObject("NoteCanvas");
                canvas = go.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 900;
                go.AddComponent<CanvasScaler>();
                go.AddComponent<GraphicRaycaster>();
            }

            var root = new GameObject("NotePresenter");
            root.transform.SetParent(canvas.transform, false);
            var img = root.AddComponent<Image>();
            img.color = new Color(0.08f, 0.08f, 0.10f, 0.88f);
            panel = root.AddComponent<CanvasGroup>();
            panel.alpha = 0f;

            var rt = root.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.08f, 0.12f);
            rt.anchorMax = new Vector2(0.92f, 0.88f);
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;

            // Title
            var titleGo = new GameObject("Title");
            titleGo.transform.SetParent(root.transform, false);
            titleText = titleGo.AddComponent<Text>();
            titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Font.CreateDynamicFontFromOSFont("Arial", 16);
            titleText.fontSize = 16; titleText.fontStyle = FontStyle.Bold;
            titleText.color = new Color(0.95f, 0.92f, 0.82f);
            titleText.alignment = TextAnchor.UpperCenter;
            var trt = titleText.rectTransform;
            trt.anchorMin = new Vector2(0, 0.82f); trt.anchorMax = new Vector2(1, 1);
            trt.offsetMin = new Vector2(16, 0); trt.offsetMax = new Vector2(-16, -12);

            // Body
            var bodyGo = new GameObject("Body");
            bodyGo.transform.SetParent(root.transform, false);
            bodyText = bodyGo.AddComponent<Text>();
            bodyText.font = titleText.font;
            bodyText.fontSize = 13; bodyText.color = new Color(0.90f, 0.88f, 0.82f);
            bodyText.alignment = TextAnchor.UpperLeft;
            bodyText.horizontalOverflow = HorizontalWrapMode.Wrap;
            bodyText.verticalOverflow  = VerticalWrapMode.Truncate;
            var brt = bodyText.rectTransform;
            brt.anchorMin = new Vector2(0, 0.12f); brt.anchorMax = new Vector2(1, 0.82f);
            brt.offsetMin = new Vector2(20, 0); brt.offsetMax = new Vector2(-20, 0);

            // Close button
            var btnGo = new GameObject("Close");
            btnGo.transform.SetParent(root.transform, false);
            var btnImg = btnGo.AddComponent<Image>(); btnImg.color = new Color(0.22f, 0.22f, 0.26f, 0.9f);
            closeButton = btnGo.AddComponent<Button>();
            var brt2 = btnGo.GetComponent<RectTransform>();
            brt2.anchorMin = new Vector2(0.5f, 0); brt2.anchorMax = new Vector2(0.5f, 0);
            brt2.anchoredPosition = new Vector2(0, 14); brt2.sizeDelta = new Vector2(90, 26);
            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(btnGo.transform, false);
            var lbl = labelGo.AddComponent<Text>();
            lbl.font = titleText.font; lbl.fontSize = 12; lbl.color = Color.white;
            lbl.alignment = TextAnchor.MiddleCenter; lbl.text = "Close";
            var lrt = lbl.rectTransform; lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
            lrt.offsetMin = Vector2.zero; lrt.offsetMax = Vector2.zero;
        }
    }
}
