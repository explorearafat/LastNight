// Last Light — Player/ThirdPersonCamera.cs
// Cinemachine-aware third-person follow camera.
// - Positional lag via Cinemachine damping; handheld Perlin sway; FOV kick on sprint.
// - No snapping on teleport (OnTargetObjectWarped).
// - Camera collisions via CinemachineCollider component on the vcam (set up in scene).
// Works even without Cinemachine — falls back to nudging the main Camera.

using UnityEngine;

namespace LastLight.Player
{
    public sealed class ThirdPersonCamera : MonoBehaviour
    {
        [Header("Framing")]
        [SerializeField] float defaultFov  = 58f;
        [SerializeField] float sprintFov   = 64f;
        [SerializeField] float fovLerpSpeed = 3f;

        [Header("Handheld / Storm")]
        [SerializeField] float handheldSwayAmp  = 0.35f;
        [SerializeField] float handheldSwayFreq = 0.45f;
        [SerializeField] float stormShakeAmp    = 1.2f;

        [Header("Collision")]
        [Tooltip("How far the camera can be pushed in by walls before clipping.")]
        [SerializeField] float collisionRadius = 0.22f;
        [SerializeField] LayerMask collisionMask = ~0;

        float  _perlinOffset;
        Camera _cam;
        PlayerController _player;

#if UNITY_CINEMACHINE
        Unity.Cinemachine.CinemachineCamera _vcam;
#endif

        void Awake()
        {
            _cam = GetComponentInChildren<Camera>();
            if (_cam == null) _cam = Camera.main;
            _perlinOffset = Random.Range(0f, 100f);
            _player = FindFirstObjectByType<PlayerController>();
#if UNITY_CINEMACHINE
            _vcam = GetComponentInChildren<Unity.Cinemachine.CinemachineCamera>();
#endif
        }

        void LateUpdate()
        {
            float dt = Time.deltaTime;

            if (_cam != null && _player != null)
            {
                float targetFov = _player.IsSprinting ? sprintFov : defaultFov;
                _cam.fieldOfView = Mathf.Lerp(_cam.fieldOfView, targetFov, dt * fovLerpSpeed);
            }

            float storm01 = 0f;
            var wm = FindFirstObjectByType<World.WeatherManager>();
            if (wm != null) storm01 = wm.StormIntensity;

            float t = Time.time * handheldSwayFreq + _perlinOffset;
            float sway = (Mathf.PerlinNoise(t, 0.5f) - 0.5f) * 2f;
            float amp  = handheldSwayAmp + storm01 * stormShakeAmp;
            Vector3 nudge = new Vector3(sway * amp * 0.02f, Mathf.Sin(t * 1.3f) * amp * 0.01f, 0f);

            Transform camT = null;
#if UNITY_CINEMACHINE
            if (_vcam != null) camT = _vcam.transform; else
#endif
            if (_cam != null) camT = _cam.transform;

            if (camT != null) camT.localPosition += nudge * dt * 60f;

            ResolveCameraCollision(dt);
        }

        void ResolveCameraCollision(float dt)
        {
            // Simple sphere-cast from the player head toward the camera to clamp position.
            // Only runs when there is no CinemachineCollider handling it.
#if UNITY_CINEMACHINE
            if (_vcam != null && _vcam.GetComponent<Unity.Cinemachine.CinemachineCollider>() != null) return;
#endif
            if (_cam == null || _player == null) return;
            Vector3 playerHead = _player.transform.position + Vector3.up * 1.5f;
            Vector3 camPos     = _cam.transform.position;
            Vector3 dir        = camPos - playerHead;
            float   dist       = dir.magnitude;
            if (dist < 0.01f) return;
            dir /= dist;
            if (Physics.SphereCast(playerHead, collisionRadius, dir, out var hit, dist, collisionMask, QueryTriggerInteraction.Ignore))
            {
                Vector3 safe = hit.point - dir * 0.1f;
                _cam.transform.position = Vector3.Lerp(camPos, safe, dt * 12f);
            }
        }

        public void WarpTo(Transform target)
        {
#if UNITY_CINEMACHINE
            if (_vcam != null)
                Unity.Cinemachine.CinemachineCamera.OnTargetObjectWarped(target, target.position - _vcam.transform.position);
#endif
        }
    }
}
