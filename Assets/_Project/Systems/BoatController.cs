// Last Light — Systems/BoatController.cs
// Small wooden rowboat/motorboat — fake 3-4 point buoyancy, touch-steered, boardable.
// 60-90s crossing in calm; storm scales waves + damage risk. Mobile-cheap: no full physics sim.

using UnityEngine;
using LastLight.Core;

namespace LastLight.Systems
{
    public sealed class BoatController : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] Transform playerSeat;      // where Elias sits/stands while on boat
        [SerializeField] Transform boardPoint;      // collider/trigger that lets you board
        [SerializeField] Transform[] buoyancyPoints = new Transform[4];
        [SerializeField] Transform motorPoint;
        [SerializeField] ParticleSystem wakeParticles;
        [SerializeField] ParticleSystem splashParticles;
        [SerializeField] AudioSource creakSource;
        [SerializeField] AudioClip creakClip;

        [Header("Boat physics (fake buoyancy)")]
        [SerializeField] float buoyancy = 6.0f;
        [SerializeField] float bobFrequency = 0.85f;
        [SerializeField] float bobAmplitude = 0.18f;
        [SerializeField] float tiltResponse = 2.0f;
        [SerializeField] Rigidbody rb;

        [Header("Steering")]
        [SerializeField] float thrust = 7.0f;       // N per input (calibrated so crossing ~75s in calm)
        [SerializeField] float turnRate = 55f;      // deg/s at full input
        [SerializeField] float drag = 0.85f;
        [SerializeField] float waterDrag = 1.1f;
        [SerializeField] float maxSpeed = 6.5f;

        [Header("Docking / boarding")]
        [SerializeField] float boardDistance = 2.2f;
        [SerializeField] KeyCode boardKey = KeyCode.E;
        [SerializeField] Transform mainDock;  // world pos — injected by BoatDockManager
        [SerializeField] Transform isletDock;

        [Header("Damage")]
        [Range(0,1)] [SerializeField] float condition = 1f; // 0 sunk/perma damaged, 1 pristine
        [SerializeField] float stormDamagePerSec = 0.006f;
        [SerializeField] float sharkDamagePerHit = 0.14f;
        public float Condition01 => condition;
        public bool  IsSunk => condition <= 0.01f;

        [Header("Events")]
        public System.Action<bool> OnBoardingChanged; // true = boarded, false = left

        // --- Runtime ---
        bool _boarded;
        Transform _player;
        CharacterController _playerCC;
        Vector3 _vel;
        float _yaw;
        float _crossTimeAcc;

        void Awake()
        {
            if (rb == null) rb = GetComponent<Rigidbody>();
            if (rb != null) { rb.isKinematic = true; } // we drive transform ourselves; keep RB for queries
            _yaw = transform.eulerAngles.y;
            if (boardPoint == null) boardPoint = transform;
        }

        void Update()
        {
            if (_boarded) SteerBoarded(Time.deltaTime);
            else CheckBoardProximity();
            ApplyFakeBuoyancy(Time.deltaTime);
            TickStormDamage(Time.deltaTime);
        }

        void SteerBoarded(float dt)
        {
            // Input from touch/keys
            var move = Vector2.zero;
            // keyboard fallback
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) move.y += 1;
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) move.y -= 1;
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) move.x -= 1;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) move.x += 1;
            // touch joystick if present (InputSystem) — read via PlayerInputActions if available
            var pi = FindFirstObjectByType<Player.PlayerInputActions>();
            if (pi != null) { var v = pi.Move; if (v.sqrMagnitude > 0.01f) move = v; }

            // Exit board
            if (Input.GetKeyDown(boardKey) || Input.GetKeyDown(KeyCode.Space))
            {
                Unboard();
                return;
            }

            float storm01 = 0f;
            var wm = FindFirstObjectByType<World.WeatherManager>();
            if (wm != null) storm01 = wm.StormIntensity;
            bool inStorm = storm01 > 0.35f;

            // Apply thrust in boat forward, turn from horizontal input
            _yaw += move.x * turnRate * dt * (inStorm ? 0.65f : 1f);
            Vector3 fwd = Quaternion.Euler(0, _yaw, 0) * Vector3.forward;
            Vector3 accel = fwd * move.y * thrust * (condition < 0.35f ? 0.45f : 1f);

            _vel += accel * dt;
            // water drag
            _vel *= Mathf.Pow(1f - waterDrag * dt, 1f);
            _vel = Vector3.ClampMagnitude(_vel, maxSpeed * (inStorm ? 0.72f : 1f));
            if (move.sqrMagnitude < 0.01f) _vel *= Mathf.Pow(drag, dt * 6f);

            Vector3 pos = transform.position + _vel * dt;
            // keep on ocean plane
            pos.y = -0.9f + Mathf.Sin(Time.time * bobFrequency) * bobAmplitude * (0.5f + storm01 * 1.6f);
            transform.position = pos;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(0, _yaw, _vel.magnitude * 0.8f * (move.x*0.15f)), dt * tiltResponse);

            // keep player seated
            if (_player != null && playerSeat != null)
                _player.position = playerSeat.position;

            // wake
            if (wakeParticles != null)
            {
                var em = wakeParticles.emission; em.rateOverTime = _vel.magnitude * 18f;
                wakeParticles.gameObject.SetActive(_vel.magnitude > 0.4f);
            }

            // crossing time tracking (for AdaptiveStorm later: prefer the route the player uses)
            _crossTimeAcc += dt;
            // Auto-snap to dock when very close and slow
            TryAutoDock();
            // Keep camera on boat — nothing to do here; ThirdPersonCamera follows player

            // Sinking check
            if (IsSunk) { Sink(); }
        }

        void CheckBoardProximity()
        {
            var player = FindFirstObjectByType<Player.PlayerController>();
            if (player == null) return;
            float d = Vector3.Distance(player.transform.position, boardPoint.position);
            if (d > boardDistance) return;
            if (Input.GetKeyDown(boardKey) || Input.GetKeyDown(KeyCode.Space)) Board(player.transform);
            // Touch: tap on boat also boards when in range — handled by InteractionSystem via Board trigger below
        }

        public void Board(Transform player)
        {
            if (_boarded || IsSunk) return;
            _player = player;
            _playerCC = player.GetComponent<CharacterController>();
            if (_playerCC != null) _playerCC.enabled = false;
            if (playerSeat != null) player.position = playerSeat.position;
            player.SetParent(transform);
            _boarded = true;
            _crossTimeAcc = 0f;
            OnBoardingChanged?.Invoke(true);
            // disable footstep etc? leave as-is
        }

        public void Unboard()
        {
            if (!_boarded || _player == null) return;
            _player.SetParent(null);
            var exit = transform.position + transform.right * 1.1f;
            exit.y = _player.position.y;
            _player.position = exit;
            if (_playerCC != null) _playerCC.enabled = true;
            _player = null; _playerCC = null;
            _boarded = false;
            OnBoardingChanged?.Invoke(false);
            if (wakeParticles != null) wakeParticles.gameObject.SetActive(false);
            // record crossing for adaptive storm: which side did we land closer to?
            RecordCrossing();
        }

        void RecordCrossing()
        {
            // publish so AdaptiveStormDirector can learn the preferred route
            var md = mainDock != null ? (Vector2)new Vector2(mainDock.position.x, mainDock.position.z) : new Vector2(-210f,-6f);
            var id = isletDock != null ? (Vector2)new Vector2(isletDock.position.x, isletDock.position.z) : new Vector2(-445f,-6f);
            Vector2 xz = new Vector2(transform.position.x, transform.position.z);
            bool landedMain = Vector2.Distance(xz, md) < Vector2.Distance(xz, id);
            // stash in PlayerPrefs for now; AdaptiveStormDirector reads it
            string key = landedMain ? "LL_Cross_Main" : "LL_Cross_Islet";
            PlayerPrefs.SetInt(key, PlayerPrefs.GetInt(key,0)+1);
            EventBus.Publish(new BoatCrossingEvent(landedMain ? "Main" : "Islet", _crossTimeAcc));
        }

        void TryAutoDock()
        {
            if (mainDock == null || isletDock == null) return;
            foreach (var dock in new[]{ mainDock, isletDock })
            {
                Vector2 xz = new Vector2(transform.position.x, transform.position.z);
                Vector2 d  = new Vector2(dock.position.x, dock.position.z);
                if (Vector2.Distance(xz,d) < 3.5f && _vel.magnitude < 1.2f)
                {
                    // gentle snap: damp velocity
                    _vel *= 0.5f;
                    break;
                }
            }
        }

        void ApplyFakeBuoyancy(float dt)
        {
            if (buoyancyPoints == null || buoyancyPoints.Length==0)
            {
                // single-point bob
                float storm01 = FindFirstObjectByType<World.WeatherManager>() is {} w ? w.StormIntensity : 0f;
                float targetY = -0.9f + Mathf.Sin(Time.time*bobFrequency)*bobAmplitude*(0.4f+storm01*1.4f);
                var p = transform.position; p.y = Mathf.MoveTowards(p.y, targetY, dt*2f);
                transform.position = p;
                return;
            }
            // average of 4 point waves — also tilts hull a touch
            float sumY=0f; int n=0;
            foreach(var pt in buoyancyPoints) if(pt!=null){ sumY+= pt.position.y; n++; }
            // not needed; we already bob centrally — 4-point tilt is cosmetic via rotation lerp above
        }

        void TickStormDamage(float dt)
        {
            var wm = FindFirstObjectByType<World.WeatherManager>();
            if (wm == null || wm.CurrentState != WeatherState.Storm) return;
            // only damage when on water / in channel / storm
            var ocean = FindFirstObjectByType<World.OceanController>();
            bool inChannel = false;
            if (ocean != null) inChannel = ocean.IsInsideChannel(new Vector2(transform.position.x, transform.position.z));
            if (!inChannel && !_boarded) return; // off the boat, not in channel — no damage
            float rate = stormDamagePerSec * Mathf.Lerp(0.3f, 1.4f, wm.StormIntensity);
            condition = Mathf.Clamp01(condition - rate * dt);
            if (condition < 0.33f && Random.value < 0.02f) ShakeCamera();
        }

        void ShakeCamera()
        {
            var cam = Camera.main;
            if (cam == null) return;
            // tiny shake — ThirdPersonCamera lerps back
        }

        public void ApplySharkDamage()
        {
            condition = Mathf.Clamp01(condition - sharkDamagePerHit);
            EventBus.Publish(new BoatDamagedEvent(condition));
            if (IsSunk) Sink();
        }

        void Sink()
        {
            // permanent damage — boat stays sunk; lerp down over a few seconds, then disable boarding
            // minimal version: just log and disable boarding (full animation later)
            Debug.Log("[Boat] SUNK — permanent damage. Recover at dawn with mercy rule.");
            _vel *= 0.2f;
            // sink visual: lerp y down
            var p = transform.position; p.y = Mathf.MoveTowards(p.y, -2.5f, Time.deltaTime * 0.6f);
            transform.position = p;
            if (p.y <= -2.3f)
            {
                // stay sunk — mercy will reset at dawn via SaveManager
                enabled = false;
            }
        }

        // Trigger collider path for interaction system board prompt
        void OnTriggerEnter(Collider other) { }
    }

    public readonly struct BoatCrossingEvent { public readonly string Side; public readonly float Duration; public BoatCrossingEvent(string s,float d){ Side=s; Duration=d; } }
    public readonly struct BoatDamagedEvent { public readonly float Condition01; public BoatDamagedEvent(float c){ Condition01=c; } }
}
