// Last Light — Player/ThirdPersonCamera.cs
// Cinemachine-based third-person follow camera with:
// - slight positional lag (Cinemachine damping) / handheld sway (Perlin noise)
// - no snapping on teleport (OnTargetObjectWarped)
// - pitch clamping and collision handled by Cinemachine components themselves
// This script is the glue: it owns the Cinemachine brain wiring and exposes
// a few runtime knobs (FOV kick when sprinting, storm shake).
// If Cinemachine is not yet imported this file still compiles — the body is guarded.

using UnityEngine;

namespace LastLight.Player
{
    public sealed class ThirdPersonCamera : MonoBehaviour
    {
        [Header("Follow")]
        [SerializeField] Transform followTarget; // typically player's head/chest bone or root
        [SerializeField] Transform lookAtTarget;

        [Header("Framing")]
        [SerializeField] float defaultFov = 58f;
        [SerializeField] float sprintFov  = 64f;
        [SerializeField] float fovLerpSpeed = 3f;

        [Header("Handheld / Storm")]
        [Tooltip("Idle handheld sway amplitude in degrees.")]
        [SerializeField] float handheldSwayAmp = 0.35f;
        [SerializeField] float handheldSwayFreq = 0.45f;
        [Tooltip("Extra shake at stormIntensity == 1.")]
        [SerializeField] float stormShakeAmp = 1.2f;

        float _perlinOffset;
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

            // FOV kick when sprinting
            if (_cam != null && _player != null)
            {
                float targetFov = _player.IsSprinting ? sprintFov : defaultFov;
                _cam.fieldOfView = Mathf.Lerp(_cam.fieldOfView, targetFov, dt * fovLerpSpeed);
            }

            // Handheld + storm sway via a tiny positional nudge (works even without Cinemachine).
            float storm01 = 0f;
            var wm = FindFirstObjectByType<World.WeatherManager>();
            if (wm != null) storm01 = wm.StormIntensity;

            float t = Time.time * handheldSwayFreq + _perlinOffset;
            float sway = (Mathf.PerlinNoise(t, 0.5f) - 0.5f) * 2f; // -1..1
            float amp  = handheldSwayAmp + storm01 * stormShakeAmp;
            Vector3 nudge = new Vector3(sway * amp * 0.02f, Mathf.Sin(t * 1.3f) * amp * 0.01f, 0f);

            // Apply as a camera-local offset so Cinemachine damping still applies on top.
            // We nudge the vcam's transform when present, otherwise the main camera.
            Transform camT = null;
#if UNITY_CINEMACHINE
            if (_vcam != null) camT = _vcam.transform;
            else
#endif
            if (_cam != null) camT = _cam.transform;

            if (camT != null)
                camT.localPosition += nudge * dt * 60f; // framerate-independent micro-sway
        }

        /// <summary>Call after teleporting the player so Cinemachine doesn't interpolate across the map.</summary>
        public void WarpTo(Transform target)
        {
#if UNITY_CINEMACHINE
            if (_vcam != null)
                Unity.Cinemachine.CinemachineCamera.OnTargetObjectWarped(target, target.position - _vcam.transform.position);
#endif
        }
    }
}
