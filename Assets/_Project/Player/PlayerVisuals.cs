// Last Light — Player/PlayerVisuals.cs
// Bridges PlayerController / PlayerStats / Weather into Animator + Animation Rigging.
// No hard dependency on a specific rig — every effect is guarded by null checks and
// degrades gracefully when the animator or rig components are absent.
// When a real character model is imported, wire the animator and optional rig targets
// in the inspector. Until then this component still compiles and runs as a placeholder.

using UnityEngine;
#if UNITY_ANIMATION_RIGGING
using UnityEngine.Animations.Rigging;
#endif

namespace LastLight.Player
{
    public sealed class PlayerVisuals : MonoBehaviour
    {
        [Header("References (optional until real rig imported)")]
        [SerializeField] Animator animator;
        [SerializeField] Transform headLookTarget;
        [SerializeField] Transform leftFootIKTarget;
        [SerializeField] Transform rightFootIKTarget;
        // Rig weights — assigned only when Animation Rigging is present.
#if UNITY_ANIMATION_RIGGING
        [SerializeField] Rig footRig;
        [SerializeField] Rig headRig;
#endif

        [Header("Tuning")]
        [SerializeField] float footRayLength = 1.1f;
        [SerializeField] LayerMask groundMask = ~0;
        [SerializeField] float breathFogThreshold = 0.6f; // cold01 above this shows breath
        [SerializeField] float shiverColdThreshold = 0.45f;

        PlayerController       _controller;
        Survival.PlayerStats   _stats;
        World.WeatherManager   _weather;

        // Animator hash cache
        static readonly int HashSpeed    = Animator.StringToHash("Speed");
        static readonly int HashSprint   = Animator.StringToHash("IsSprinting");
        static readonly int HashGrounded = Animator.StringToHash("IsGrounded");
        static readonly int HashCold01   = Animator.StringToHash("Cold01");
        static readonly int HashFatigue  = Animator.StringToHash("Fatigue");
        static readonly int HashStumble  = Animator.StringToHash("Stumble");
        static readonly int HashShiver   = Animator.StringToHash("Shiver");

        void Awake()
        {
            _controller = GetComponent<PlayerController>();
            _stats      = GetComponent<Survival.PlayerStats>();
            if (animator == null) animator = GetComponentInChildren<Animator>();
        }

        void Start()
        {
            // Lazy find — WeatherManager may not exist in a minimal test scene.
            _weather = FindFirstObjectByType<World.WeatherManager>();
        }

        void Update()
        {
            UpdateAnimator();
            UpdateFootIK();
            UpdateHeadLook();
        }

        void UpdateAnimator()
        {
            if (animator == null) return;

            float speed01 = _controller != null ? _controller.NormalizedSpeed : 0f;
            bool  sprint  = _controller != null && _controller.IsSprinting;
            bool  grounded= _controller != null && _controller.IsGrounded;

            animator.SetFloat(HashSpeed,    speed01);
            animator.SetBool (HashSprint,   sprint);
            animator.SetBool (HashGrounded, grounded);

            float cold01   = _stats != null ? _stats.Cold01 : 0f;
            float stamina01= _stats != null ? _stats.Stamina01 : 1f;
            float fatigue  = 1f - stamina01; // 0 rested → 1 exhausted
            float stumble  = _controller != null ? _controller.WindStumble01 : 0f;
            float shiver   = cold01 > shiverColdThreshold
                ? Mathf.InverseLerp(shiverColdThreshold, 1f, cold01) : 0f;

            animator.SetFloat(HashCold01,  cold01);
            animator.SetFloat(HashFatigue, fatigue);
            animator.SetFloat(HashStumble, stumble);
            animator.SetFloat(HashShiver,  shiver);

            // Breath fog / shaking-hands effects are typically particle or procedural —
            // here we just expose the values. Actual VFX can read animator floats or stats directly.
            // Example hook: if (cold01 > breathFogThreshold) BreathVFX.Emit(...);
        }

        void UpdateFootIK()
        {
            if (leftFootIKTarget == null && rightFootIKTarget == null) return;
            if (_controller != null && !_controller.IsGrounded) return;

            if (leftFootIKTarget  != null) PlaceFoot(leftFootIKTarget,  -transform.right * 0.12f);
            if (rightFootIKTarget != null) PlaceFoot(rightFootIKTarget,  transform.right * 0.12f);
        }

        void PlaceFoot(Transform ik, Vector3 lateralOffset)
        {
            Vector3 origin = transform.position + Vector3.up * 0.5f + lateralOffset;
            if (Physics.Raycast(origin, Vector3.down, out var hit, footRayLength, groundMask, QueryTriggerInteraction.Ignore))
            {
                ik.position = hit.point + Vector3.up * 0.04f;
                ik.rotation = Quaternion.FromToRotation(Vector3.up, hit.normal) * transform.rotation;
            }
        }

        void UpdateHeadLook()
        {
            if (headLookTarget == null) return;
            // Simple look-at 4-6 metres ahead in movement direction; falls back to forward.
            Vector3 dir = _controller != null && _controller.Velocity.sqrMagnitude > 0.01f
                ? _controller.Velocity.normalized : transform.forward;
            headLookTarget.position = transform.position + dir * 5f + Vector3.up * 1.4f;
        }
    }
}
