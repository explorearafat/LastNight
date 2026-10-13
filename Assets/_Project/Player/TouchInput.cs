// Last Light — Player/TouchInput.cs
// Touch input layer + editor fallback.
// - Left side: floating joystick (appears where the finger lands).
// - Right side: camera swipe (pan/tilt).
// - Bottom-right: context interact button that only shows when something is nearby.
// Behaviour:
// - On Android (or when a touch is active) this is the authoritative input source and
//   writes directly into PlayerInputActions via InjectMove / InjectLook.
// - In the editor with no touch, PlayerInputActions' own keyboard/gamepad polling
//   remains authoritative and this component just forwards the interact button.
//
// UI wiring: assign the joystick background/handle RectTransforms and the interact Button.
// The joystick visuals use anchoredPosition math so they work with any Canvas scaler.

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LastLight.Player
{
    public sealed class TouchInput : MonoBehaviour
    {
        [Header("Wiring")]
        [SerializeField] PlayerInputActions inputActions;

        [Header("Joystick UI (optional — auto-creates fallback if missing)")]
        [SerializeField] RectTransform joystickBackground;
        [SerializeField] RectTransform joystickHandle;
        [SerializeField] float joystickRadius = 70f;
        [SerializeField] float joystickDeadzone = 0.12f;

        [Header("Camera Swipe")]
        [SerializeField] float lookSensitivity = 0.18f;
        [SerializeField] float swipeDeadzonePx = 2f;

        [Header("Interact Button")]
        [SerializeField] Button interactButton;
        [SerializeField] CanvasGroup interactGroup;
        [SerializeField] float interactCheckRadius = 2.2f;
        [SerializeField] LayerMask interactMask = ~0;

        // --- runtime ---
        int   _moveFingerId = -1;
        int   _lookFingerId = -1;
        Vector2 _joystickOrigin; // screen px
        Vector2 _activeMove;     // -1..1
        Vector2 _activeLook;
        Core.IInteractable _currentInteractable;

        // Split line: left 45% = joystick, right 55% = camera swipe
        float SplitX => Screen.width * 0.45f;

        void Awake()
        {
            if (inputActions == null) inputActions = FindFirstObjectByType<PlayerInputActions>();
            if (interactButton != null)
                interactButton.onClick.AddListener(OnInteractPressed);
            ShowJoystick(false);
        }

        void Update()
        {
            bool hasTouch = Input.touchCount > 0;
            if (hasTouch) HandleTouches();
            else HandleMouseFallback();

            // Feed movement into the input system (authoritative when touch is active)
            if (hasTouch || _activeMove.sqrMagnitude > 0.001f)
                inputActions.InjectMove(_activeMove);

            // Camera swipe is accumulated per frame and consumed by PlayerController / camera rig
            if (_activeLook.sqrMagnitude > 0.001f)
            {
                inputActions.InjectLook(_activeLook);
                _activeLook = Vector2.zero; // consume — next frame starts fresh
            }

            UpdateInteractButton();
        }

        // ── Touches ──

        void HandleTouches()
        {
            foreach (var t in Input.touches)
            {
                switch (t.phase)
                {
                    case TouchPhase.Began:
                        OnTouchBegan(t);
                        break;
                    case TouchPhase.Moved:
                    case TouchPhase.Stationary:
                        OnTouchMoved(t);
                        break;
                    case TouchPhase.Ended:
                    case TouchPhase.Canceled:
                        OnTouchEnded(t);
                        break;
                }
            }
        }

        void OnTouchBegan(Touch t)
        {
            if (IsOverUI(t.position)) return;

            if (t.position.x < SplitX && _moveFingerId == -1)
            {
                _moveFingerId = t.fingerId;
                _joystickOrigin = t.position;
                if (joystickBackground != null)
                {
                    joystickBackground.anchoredPosition = ScreenToCanvas(t.position);
                    ShowJoystick(true);
                }
            }
            else if (t.position.x >= SplitX && _lookFingerId == -1)
            {
                _lookFingerId = t.fingerId;
            }
        }

        void OnTouchMoved(Touch t)
        {
            if (t.fingerId == _moveFingerId)
            {
                Vector2 delta = t.position - _joystickOrigin;
                float mag = delta.magnitude;
                Vector2 dir = mag > 0.01f ? delta / mag : Vector2.zero;
                float mapped = Mathf.InverseLerp(joystickDeadzone * joystickRadius, joystickRadius, mag);
                mapped = Mathf.Clamp01(mapped);
                _activeMove = dir * mapped;

                if (joystickHandle != null)
                    joystickHandle.anchoredPosition = dir * mapped * joystickRadius;
            }
            else if (t.fingerId == _lookFingerId)
            {
                Vector2 d = t.deltaPosition;
                if (d.magnitude < swipeDeadzonePx) return;
                // Camera: horizontal = yaw, vertical = pitch (invert Y so swipe-up looks up).
                _activeLook += new Vector2(d.x, -d.y) * lookSensitivity;
            }
        }

        void OnTouchEnded(Touch t)
        {
            if (t.fingerId == _moveFingerId)
            {
                _moveFingerId = -1;
                _activeMove = Vector2.zero;
                if (joystickHandle != null) joystickHandle.anchoredPosition = Vector2.zero;
                ShowJoystick(false);
            }
            if (t.fingerId == _lookFingerId) _lookFingerId = -1;
        }

        // ── Mouse fallback (editor) ──

        void HandleMouseFallback()
        {
            if (Input.GetMouseButtonDown(0) && !IsOverUI(Input.mousePosition))
            {
                if (Input.mousePosition.x < SplitX)
                {
                    _moveFingerId = -2; // pseudo finger for mouse
                    _joystickOrigin = Input.mousePosition;
                    if (joystickBackground != null)
                    {
                        joystickBackground.anchoredPosition = ScreenToCanvas(Input.mousePosition);
                        ShowJoystick(true);
                    }
                }
            }
            if (Input.GetMouseButton(0) && _moveFingerId == -2)
            {
                Vector2 delta = (Vector2)Input.mousePosition - _joystickOrigin;
                float mag = delta.magnitude;
                Vector2 dir = mag > 0.01f ? delta / mag : Vector2.zero;
                float mapped = Mathf.InverseLerp(joystickDeadzone * joystickRadius, joystickRadius, mag);
                _activeMove = dir * Mathf.Clamp01(mapped);
                if (joystickHandle != null) joystickHandle.anchoredPosition = dir * Mathf.Clamp01(mapped) * joystickRadius;
            }
            if (Input.GetMouseButtonUp(0) && _moveFingerId == -2)
            {
                _moveFingerId = -1;
                _activeMove = Vector2.zero;
                if (joystickHandle != null) joystickHandle.anchoredPosition = Vector2.zero;
                ShowJoystick(false);
            }

            // Right-drag camera swipe in editor
            if (Input.GetMouseButton(1) || (Input.GetMouseButton(0) && Input.mousePosition.x >= SplitX && _moveFingerId == -1))
            {
                Vector2 d = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
                if (d.sqrMagnitude > 0.0001f)
                    _activeLook += new Vector2(d.x, -d.y) * lookSensitivity * 12f;
            }
        }

        // ── Helpers ──

        void ShowJoystick(bool show)
        {
            if (joystickBackground != null) joystickBackground.gameObject.SetActive(show);
            // Handle stays as child — no need to toggle separately.
        }

        Vector2 ScreenToCanvas(Vector2 screenPos)
        {
            if (joystickBackground == null) return screenPos;
            var canvas = joystickBackground.GetComponentInParent<Canvas>();
            if (canvas == null) return screenPos;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                (RectTransform)canvas.transform, screenPos, canvas.worldCamera, out var local);
            return local;
        }

        bool IsOverUI(Vector2 screenPos)
        {
            if (EventSystem.current == null) return false;
            var ped = new PointerEventData(EventSystem.current) { position = screenPos };
            var results = new System.Collections.Generic.List<RaycastResult>();
            EventSystem.current.RaycastAll(ped, results);
            return results.Count > 0;
        }

        void UpdateInteractButton()
        {
            var player = FindFirstObjectByType<PlayerController>();
            Core.IInteractable best = null;
            float bestDist = float.MaxValue;

            if (player != null)
            {
                var hits = Physics.OverlapSphere(player.transform.position, interactCheckRadius, interactMask, QueryTriggerInteraction.Collide);
                foreach (var h in hits)
                {
                    var ia = h.GetComponentInParent<Core.IInteractable>();
                    if (ia == null || !ia.CanInteract) continue;
                    float d = Vector3.Distance(player.transform.position, ia.InteractionPoint);
                    if (d < bestDist) { bestDist = d; best = ia; }
                }
            }

            _currentInteractable = best;
            bool show = best != null;

            if (interactGroup != null)
            {
                interactGroup.alpha = show ? 1f : 0f;
                interactGroup.interactable = show;
                interactGroup.blocksRaycasts = show;
            }
            if (interactButton != null) interactButton.gameObject.SetActive(show);

            // Optional: update button label if it has a Text/TMP child.
            if (show && interactButton != null)
            {
                var txt = interactButton.GetComponentInChildren<TMPro.TMP_Text>();
                if (txt != null) txt.text = best.Prompt;
                else
                {
                    var legacy = interactButton.GetComponentInChildren<Text>();
                    if (legacy != null) legacy.text = best.Prompt;
                }
            }
        }

        void OnInteractPressed()
        {
            if (_currentInteractable != null && _currentInteractable.CanInteract)
                _currentInteractable.Interact(gameObject);
            else
                inputActions?.NotifyInteract();
        }
    }
}
