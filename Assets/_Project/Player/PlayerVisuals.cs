// Last Light — Player/PlayerVisuals.cs
// Bridges PlayerController / PlayerStats / Weather / Inventory into Animator + Rigging.
// No hard dependency on a specific rig — every effect is null-guarded and degrades
// gracefully when the animator or rig components are absent.
// When a real character model is imported, wire the animator + optional rig targets
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
#if UNITY_ANIMATION_RIGGING
        [SerializeField] Rig footRig;
        [SerializeField] Rig headRig;
#endif
        [Tooltip("Optional breath VFX (particle system that puffs when cold).")]
        [SerializeField] ParticleSystem breathVFX;

        [Header("Tuning")]
        [SerializeField] float footRayLength = 1.1f;
        [SerializeField] LayerMask groundMask = ~0;
        [SerializeField] float breathFogThreshold = 0.6f;
        [SerializeField] float shiverColdThreshold = 0.45f;
        [SerializeField] float breathEmitInterval = 2.2f;

        PlayerController       _controller;
        Survival.PlayerStats   _stats;
        World.WeatherManager   _weather;

        // Animator hash cache — matches the Day-2 controller params.
        static readonly int HashSpeed       = Animator.StringToHash("Speed");
        static readonly int HashSprint      = Animator.StringToHash("IsSprinting");
        static readonly int HashGrounded    = Animator.StringToHash("IsGrounded");
        static readonly int HashCold01      = Animator.StringToHash("Cold01");
        static readonly int HashFatigue     = Animator.StringToHash("Fatigue");
        static readonly int HashStumble     = Animator.StringToHash("Stumble");
        static readonly int HashShiver      = Animator.StringToHash("Shiver");
        static readonly int HashTurnYaw     = Animator.StringToHash("TurnYaw");    // -1..1 for turn-in-place
        static readonly int HashStairs01    = Animator.StringToHash("Stairs01");   // 0 flat, 1 stairs
        static readonly int HashInteract    = Animator.StringToHash("Interact");   // trigger

        float _breathTimer;

        void Awake()
        {
            _controller = GetComponent<PlayerController>();
            _stats      = GetComponent<Survival.PlayerStats>();
            if (animator == null) animator = GetComponentInChildren<Animator>();
        }

        void Start() => _weather = FindFirstObjectByType<World.WeatherManager>();

        void Update()
        {
            UpdateAnimator();
            UpdateFootIK();
            UpdateHeadLook();
            UpdateBreath();
        }

        void UpdateAnimator()
        {
            if (animator == null) return;

            float speed01  = _controller != null ? _controller.NormalizedSpeed : 0f;
            bool  sprint   = _controller != null && _controller.IsSprinting;
            bool  grounded = _controller != null && _controller.IsGrounded;

            animator.SetFloat(HashSpeed,    speed01);
            animator.SetBool (HashSprint,   sprint);
            animator.SetBool (HashGrounded, grounded);

            float cold01    = _stats != null ? _stats.Cold01 : 0f;
            float stamina01 = _stats != null ? _stats.Stamina01 : 1f;
            float fatigue   = 1f - stamina01;
            float stumble   = _controller != null ? _controller.WindStumble01 : 0f;
            float shiver    = cold01 > shiverColdThreshold ? Mathf.InverseLerp(shiverColdThreshold, 1f, cold01) : 0f;

            animator.SetFloat(HashCold01,  cold01);
            animator.SetFloat(HashFatigue, fatigue);
            animator.SetFloat(HashStumble, stumble);
            animator.SetFloat(HashShiver,  shiver);

            // Turn-in-place hint: signed yaw delta (consumed by a 1D blend tree).
            if (_controller != null)
            {
                float turnYaw = 0f;
                Vector2 mv = _controller.LastWishDir; // wish direction in world xz
                if (mv.sqrMagnitude > 0.01f)
                {
                    Vector3 wish3 = new Vector3(mv.x, 0, mv.y);
                    float targetYaw = Mathf.Atan2(wish3.x, wish3.z) * Mathf.Rad2Deg;
                    float curYaw    = transform.eulerAngles.y;
                    float delta     = Mathf.DeltaAngle(curYaw, targetYaw) / 90f; // -2..2 → clamp to -1..1
                    turnYaw = Mathf.Clamp(delta, -1f, 1f);
                    // Only feed turn blend when nearly stationary (movement blend owns it otherwise)
                    if (speed01 > 0.12f) turnYaw = 0f;
                }
                animator.SetFloat(HashTurnYaw, turnYaw);
            }

            // Stairs blend (set externally or by a trigger volume on the stairs mesh)
            // Kept at 0 here; a StairsVolume can call SetStairs01.
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
            Vector3 dir = _controller != null && _controller.Velocity.sqrMagnitude > 0.01f
                ? _controller.Velocity.normalized : transform.forward;
            headLookTarget.position = transform.position + dir * 5f + Vector3.up * 1.4f;
        }

        void UpdateBreath()
        {
            if (breathVFX == null || _stats == null) return;
            if (_stats.Cold01 < breathFogThreshold) return;
            _breathTimer -= Time.deltaTime;
            if (_breathTimer <= 0f)
            {
                _breathTimer = Mathf.Lerp(breathEmitInterval, 0.9f, _stats.Cold01);
                breathVFX.Play();
            }
        }

        // Called by interaction code to fire the Interact trigger.
        public void TriggerInteract() { if (animator != null) animator.SetTrigger(HashInteract); }

        // Called by StairsVolume or similar.
        public void SetStairs01(float v) { if (animator != null) animator.SetFloat(HashStairs01, Mathf.Clamp01(v)); }
    }
}
