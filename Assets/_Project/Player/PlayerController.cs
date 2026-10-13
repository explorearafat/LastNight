// Last Light — Player/PlayerController.cs
// CharacterController-based third-person controller.
// Design choices:
// - CharacterController over Rigidbody: deterministic grounding, step offset and slope limit built-in,
//   no jitter from physics solver, simpler slope/stairs and no need to fight rigidbody forces for
//   weighted acceleration. Trade-off: we handle gravity and collision response ourselves.
// - Weighted accel/decel with separate rates so starts feel heavy and stops feel planted.
// - Turn-in-place: when input is small the character rotates toward the target yaw without translating.
// - Slope handling: we project velocity onto the ground plane and apply an extra downhill slide past the limit.
// - Foot IK, shiver, stumble and head look-at live in PlayerVisuals / Animation Rigging; this script
//   only exposes normalized parameters for them to read.
//
// Requires: CharacterController, PlayerInputActions on the same GameObject; a child camera rig (Cinemachine).

using UnityEngine;

namespace LastLight.Player
{
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(PlayerInputActions))]
    public sealed class PlayerController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] Transform cameraYawRoot; // typically the camera or an empty that follows the camera yaw

        [Header("Tuning")]
        [SerializeField] float walkSpeed   = Core.GameDefines.PlayerWalkSpeed;
        [SerializeField] float jogSpeed    = Core.GameDefines.PlayerJogSpeed;
        [SerializeField] float sprintSpeed = Core.GameDefines.PlayerSprintSpeed;
        [SerializeField] float acceleration = Core.GameDefines.PlayerAcceleration;
        [SerializeField] float deceleration = Core.GameDefines.PlayerDeceleration;
        [SerializeField] float turnSpeedDeg = Core.GameDefines.PlayerTurnSpeed;
        [SerializeField] float gravity      = Core.GameDefines.PlayerGravity;
        [SerializeField] float slopeLimitDeg = Core.GameDefines.PlayerSlopeLimitDeg;

        [Header("Turn-in-place")]
        [SerializeField] float turnInPlaceThreshold = 0.12f;
        [SerializeField] float turnInPlaceSpeed     = 360f;

        [Header("Grounding")]
        [SerializeField] LayerMask groundMask = ~0;
        [SerializeField] float groundStickForce = 2f;

        // --- Public readouts for visuals / survival ---
        /// <summary>0-1 based on current horizontal speed / sprint speed.</summary>
        public float NormalizedSpeed { get; private set; }
        /// <summary>Current horizontal velocity (world space).</summary>
        public Vector3 Velocity { get; private set; }
        public bool IsGrounded { get; private set; }
        public bool IsSprinting { get; private set; }
        /// <summary>Wind-induced stumble amount 0-1, driven by WeatherManager.</summary>
        public float WindStumble01 { get; set; }

        CharacterController _cc;
        PlayerInputActions  _input;
        Vector3 _planarVelocity; // xz only, smoothed
        float   _verticalVelocity;
        Vector3 _groundNormal = Vector3.up;

        // Exposed for animator / visuals
        float _inputMagnitude; // 0-1

        // Survival reference (optional)
        Survival.PlayerStats _stats;

        void Awake()
        {
            _cc    = GetComponent<CharacterController>();
            _input = GetComponent<PlayerInputActions>();
            _stats = GetComponent<Survival.PlayerStats>();
            if (cameraYawRoot == null) cameraYawRoot = Camera.main != null ? Camera.main.transform : transform;

            _cc.slopeLimit = slopeLimitDeg;
            _cc.stepOffset = Core.GameDefines.PlayerStepHeight;
        }

        void Update()
        {
            float dt = Time.deltaTime;

            GroundCheck();
            HandleMovement(dt);
            HandleRotation(dt);
            ApplyGravity(dt);

            // Combine planar + vertical and move
            Vector3 motion = (Velocity + Vector3.up * _verticalVelocity) * dt;
            _cc.Move(motion);

            // Re-ground after move
            GroundCheck();

            // Survival hooks: sprint drains stamina; wind adds stamina cost
            if (_stats != null)
            {
                bool wantsSprint = _input.SprintHeld && _inputMagnitude > 0.5f && _stats.CanSprint;
                IsSprinting = wantsSprint;
                if (wantsSprint) _stats.DrainStamina((Core.GameDefines.StaminaDrainSprint + WindStumble01 * 10f) * dt);
            }
            else
            {
                IsSprinting = _input.SprintHeld && _inputMagnitude > 0.5f;
            }
        }

        void GroundCheck()
        {
            IsGrounded = _cc.isGrounded;
            if (IsGrounded)
            {
                // Sphere-adjacent ray to get a stable ground normal even on edges.
                Vector3 origin = transform.position + Vector3.up * 0.1f;
                if (Physics.Raycast(origin, Vector3.down, out var hit, 1.2f, groundMask, QueryTriggerInteraction.Ignore))
                    _groundNormal = hit.normal;
                else
                    _groundNormal = Vector3.up;

                if (_verticalVelocity < 0f) _verticalVelocity = -groundStickForce;
            }
        }

        void HandleMovement(float dt)
        {
            Vector2 input2 = _input.Move;
            _inputMagnitude = Mathf.Clamp01(input2.magnitude);

            // Camera-relative input
            Vector3 camFwd = cameraYawRoot.forward; camFwd.y = 0; camFwd.Normalize();
            Vector3 camRight = cameraYawRoot.right; camRight.y = 0; camRight.Normalize();
            Vector3 wishDir = (camFwd * input2.y + camRight * input2.x);
            if (wishDir.sqrMagnitude > 1f) wishDir.Normalize();

            // Turn-in-place zone: rotate but don't translate
            bool turnInPlace = _inputMagnitude < turnInPlaceThreshold && wishDir.sqrMagnitude > 0.01f;
            if (turnInPlace)
            {
                // No planar motion — caller (HandleRotation) will rotate the body.
                // Decay velocity toward zero.
                _planarVelocity = Vector3.MoveTowards(_planarVelocity, Vector3.zero, deceleration * dt);
                Velocity = _planarVelocity;
                NormalizedSpeed = 0f;
                return;
            }

            float targetSpeed = 0f;
            if (_inputMagnitude > 0.01f)
            {
                if (IsSprinting) targetSpeed = sprintSpeed;
                else if (_inputMagnitude > 0.55f) targetSpeed = jogSpeed;
                else targetSpeed = walkSpeed;

                // Wind stumble reduces effective speed and adds randomness at high intensity.
                if (WindStumble01 > 0.05f)
                {
                    float windPenalty = 1f - WindStumble01 * 0.45f;
                    windPenalty += Random.Range(-0.06f, 0.02f) * WindStumble01;
                    targetSpeed *= Mathf.Clamp01(windPenalty);
                }
            }

            Vector3 targetVel = wishDir * targetSpeed;

            // Project target onto ground plane to avoid popping on slopes.
            if (IsGrounded)
            {
                targetVel = Vector3.ProjectOnPlane(targetVel, _groundNormal);
                // Extra slide on slopes steeper than the limit
                float slopeAngle = Vector3.Angle(_groundNormal, Vector3.up);
                if (slopeAngle > slopeLimitDeg)
                {
                    Vector3 slideDir = new Vector3(_groundNormal.x, 0, _groundNormal.z).normalized;
                    float slideAmount = Mathf.InverseLerp(slopeLimitDeg, 60f, slopeAngle);
                    targetVel += slideDir * (slideAmount * 2.5f);
                }
            }

            float accel = (targetVel.magnitude > _planarVelocity.magnitude) ? acceleration : deceleration;
            _planarVelocity = Vector3.MoveTowards(_planarVelocity, targetVel, accel * dt);

            // Clamp tiny residuals
            if (_inputMagnitude < 0.02f && _planarVelocity.magnitude < 0.05f)
                _planarVelocity = Vector3.zero;

            Velocity = _planarVelocity;
            NormalizedSpeed = Mathf.InverseLerp(0f, sprintSpeed, _planarVelocity.magnitude);
        }

        void HandleRotation(float dt)
        {
            Vector3 look = Vector3.zero;
            if (_input.Move.sqrMagnitude > 0.001f)
            {
                Vector3 camFwd = cameraYawRoot.forward; camFwd.y = 0; camFwd.Normalize();
                Vector3 camRight = cameraYawRoot.right; camRight.y = 0; camRight.Normalize();
                look = (camFwd * _input.Move.y + camRight * _input.Move.x).normalized;
            }

            if (look.sqrMagnitude < 0.001f) return;

            Quaternion targetRot = Quaternion.LookRotation(look, Vector3.up);
            float speed = (_inputMagnitude < turnInPlaceThreshold) ? turnInPlaceSpeed : turnSpeedDeg;
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, speed * dt);
        }

        void ApplyGravity(float dt)
        {
            if (!IsGrounded) _verticalVelocity += gravity * dt;
        }
    }
}
