// Last Light — Player/PlayerInputActions.cs
// Thin wrapper around Unity Input System actions.
// Built in code so the project compiles without a generated .inputactions asset.
// To move to a visual asset later, replace this with the generated C# class
// while keeping the same public surface (Move, Look, SprintHeld, InteractPressed).

using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LastLight.Player
{
    public sealed class PlayerInputActions : MonoBehaviour
    {
        public Vector2 Move  { get; private set; }
        public Vector2 Look  { get; private set; }
        public bool    SprintHeld { get; private set; }
        public event Action InteractPressed;

        [Header("Fallback (Editor)")]
        [Tooltip("Also poll Keyboard directly so WASD stays responsive even if action callbacks miss a frame.")]
        [SerializeField] bool useFallbackPolling = true;

        InputAction _move, _look, _sprint, _interact;
        InputActionMap _map;

        void Awake() => BuildActions();

        void BuildActions()
        {
            _map = new InputActionMap("Player");

            // Move — composite WASD + gamepad stick
            _move = _map.AddAction("Move", InputActionType.Value);
            _move.AddCompositeBinding("2DVector")
                .With("Up",    "<Keyboard>/w")
                .With("Down",  "<Keyboard>/s")
                .With("Left",  "<Keyboard>/a")
                .With("Right", "<Keyboard>/d");
            _move.AddBinding("<Gamepad>/leftStick");

            // Look — mouse delta + gamepad + touch
            _look = _map.AddAction("Look", InputActionType.Value);
            _look.AddBinding("<Mouse>/delta");
            _look.AddBinding("<Gamepad>/rightStick");
            _look.AddBinding("<Touchscreen>/delta");

            // Sprint
            _sprint = _map.AddAction("Sprint", InputActionType.Button);
            _sprint.AddBinding("<Keyboard>/leftShift");
            _sprint.AddBinding("<Gamepad>/leftStickPress");

            // Interact
            _interact = _map.AddAction("Interact", InputActionType.Button);
            _interact.AddBinding("<Keyboard>/e");
            _interact.AddBinding("<Keyboard>/space");
            _interact.AddBinding("<Gamepad>/buttonSouth");

            _move.performed   += ctx => Move = ctx.ReadValue<Vector2>();
            _move.canceled    += _   => Move = Vector2.zero;
            _look.performed   += ctx => Look = ctx.ReadValue<Vector2>();
            _look.canceled    += _   => Look = Vector2.zero;
            _sprint.performed += _   => SprintHeld = true;
            _sprint.canceled  += _   => SprintHeld = false;
            _interact.performed += _ => InteractPressed?.Invoke();

            _map.Enable();
        }

        void OnDestroy() => _map?.Disable();

        void Update()
        {
            if (!useFallbackPolling) return;
            if (Keyboard.current == null) return;
            if (Move.sqrMagnitude >= 0.01f) return; // gamepad / touch is authoritative

            float x = (Keyboard.current.dKey.isPressed ? 1 : 0) - (Keyboard.current.aKey.isPressed ? 1 : 0);
            float y = (Keyboard.current.wKey.isPressed ? 1 : 0) - (Keyboard.current.sKey.isPressed ? 1 : 0);
            if (x != 0 || y != 0) Move = new Vector2(x, y).normalized;
        }

        /// <summary>Called by TouchInput to inject joystick values (authoritative on mobile).</summary>
        public void InjectMove(Vector2 v) => Move = Vector2.ClampMagnitude(v, 1f);
        public void InjectLook(Vector2 v) => Look = v;

        /// <summary>Raised by TouchInput's interact button.</summary>
        public void NotifyInteract() => InteractPressed?.Invoke();
    }
}
