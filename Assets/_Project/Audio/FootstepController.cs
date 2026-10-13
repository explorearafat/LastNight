// Last Light — Audio/FootstepController.cs
// Attach to the player. Raycasts down to identify the SurfaceType underfoot and
// plays a pooled footstep via AudioManager. Keeps its own step timer so movement
// speed drives the cadence.

using UnityEngine;
using LastLight.Core;

namespace LastLight.Audio
{
    public sealed class FootstepController : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] AudioManager audioManager;
        [SerializeField] Player.PlayerController controller;

        [Header("Detection")]
        [SerializeField] LayerMask groundMask = ~0;
        [SerializeField] float rayLength = 1.6f;
        [SerializeField] float stepThresholdSpeed = 0.35f; // below this, no footsteps

        float _stepTimer;

        void Awake()
        {
            if (audioManager == null) audioManager = FindFirstObjectByType<AudioManager>();
            if (controller   == null) controller   = GetComponent<Player.PlayerController>();
        }

        void Update()
        {
            if (audioManager == null || controller == null) return;
            if (!controller.IsGrounded) return;

            float speed01 = controller.NormalizedSpeed;
            if (speed01 < stepThresholdSpeed) return;

            bool sprint = controller.IsSprinting;
            float interval = sprint ? GameDefines.FootstepIntervalRun : GameDefines.FootstepIntervalWalk;
            // Heavier inventory = slightly slower cadence
            var inv = GetComponent<Survival.Inventory>();
            if (inv != null) interval *= 1f + inv.Weight01 * 0.12f;

            _stepTimer -= Time.deltaTime;
            if (_stepTimer > 0f) return;
            _stepTimer = interval;

            SurfaceType surface = DetectSurface();
            float volume = sprint ? 0.52f : 0.42f;
            audioManager.PlayFootstep(surface, transform.position, volume);
        }

        SurfaceType DetectSurface()
        {
            Vector3 origin = transform.position + Vector3.up * 0.5f;
            if (!Physics.Raycast(origin, Vector3.down, out var hit, rayLength, groundMask, QueryTriggerInteraction.Ignore))
                return SurfaceType.Stone;

            // Tag-based surface hint — set your ground meshes' tags to Wood/Metal/Sand/Grass/etc.
            string tag = hit.collider.tag;
            return tag switch
            {
                "Wood"  => SurfaceType.Wood,
                "Metal" => SurfaceType.Metal,
                "Sand"  => SurfaceType.Sand,
                "Grass" => SurfaceType.Grass,
                "Water" => SurfaceType.Water,
                _       => SurfaceType.Stone,
            };
        }
    }
}
