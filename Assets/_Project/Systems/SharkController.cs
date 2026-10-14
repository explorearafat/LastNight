// Last Light — Systems/SharkController.cs — mobile-cheap shark channel threat.
// 3-5 sharks (LL_Shark fin proxy), ScriptableObject-tuned, simple steering in the channel.
// Fins visible above water, bob + drift, aggression scales with time/weather/route.
// Telegraph: fin always visible, splash + camera shake on approach, audio via AudioManager.

using UnityEngine;
using LastLight.Core;

namespace LastLight.Systems
{
    [CreateAssetMenu(menuName = "LastLight/Shark Profile", fileName = "SharkProfile")]
    public sealed class SharkProfile : ScriptableObject
    {
        [Header("Count")]
        [Range(1,5)] public int sharkCount = 3;
        public float spawnMargin = 8f;

        [Header("Movement (XZ plane, y is ocean)")]
        public float cruiseSpeed = 2.4f;
        public float stalkSpeed  = 4.2f;
        public float attackSpeed = 6.8f;
        [Range(10f,180f)] public float turnRateDeg = 75f;

        [Header("Detection radii (m)")]
        public float detectBoatRadius = 38f;
        public float detectSwimRadius = 18f;
        public float attackRadius     = 3.0f;

        [Header("Aggression multipliers")]
        public float nightAggression = 1.35f;
        public float stormAggression = 1.55f;
        public float routeAggression = 1.35f; // fave channel route

        [Header("Timing (s)")]
        public float attackCooldown = 7f;
        public float repelDuration  = 5f;
        public float circleTime     = 3.5f;

        [Header("Damage")]
        public float boatDamagePerHit = 0.12f;
        public float swimDamagePerSec = 0.55f;

        [Header("Bob")]
        public float bobAmp = 0.11f;
        public float bobHz  = 0.55f;

        [Header("Telegraph")]
        public float shakeOnApproach = 0.18f;
        public float finWobbleAmp = 0.06f;
    }

    public sealed class SharkController : MonoBehaviour
    {
        public static SharkController Instance { get; private set; }

        [SerializeField] SharkProfile profile;
        [SerializeField] GameObject sharkPrefab; // LL_Shark (fin+body)

        enum State { Patrol, Circle, Stalk, Attack, Repelled }

        struct Shark { public Transform tr; public Vector3 vel; public State state; public float t; public float cooldown; public float repelT; public float phase; }

        Shark[] _sharks;
        Transform _boatTr;
        Transform _playerTr;
        World.OceanController _ocean;
        World.WeatherManager _weather;
        World.DayNightCycle _dayNight;

        // External hooks
        public void SplashAt(Vector3 pos, float strength = 1f)
        {
            if (_sharks==null) return;
            for(int i=0;i<_sharks.Length;i++)
            {
                float d = Vector3.Distance(_sharks[i].tr.position, pos);
                if(d < 50f * Mathf.Clamp01(strength) + 8f)
                {
                    _sharks[i].state = State.Stalk;
                    _sharks[i].t = 0f;
                    _sharks[i].vel = (pos - _sharks[i].tr.position).WithY(0).normalized * profile.stalkSpeed;
                }
            }
        }

        public void Repel(Vector3 pos, float radius = 18f)
        {
            if (_sharks==null) return;
            for(int i=0;i<_sharks.Length;i++)
                if(Vector3.Distance(_sharks[i].tr.position,pos) < radius)
                { _sharks[i].state=State.Repelled; _sharks[i].repelT=profile.repelDuration; _sharks[i].t=0f; }
        }

        void Awake()
        {
            Instance = this;
            _ocean  = FindFirstObjectByType<World.OceanController>();
            _weather= FindFirstObjectByType<World.WeatherManager>();
            _dayNight=FindFirstObjectByType<World.DayNightCycle>();
            Spawn();
            if(FindBoat()==null) Invoke(nameof(RebindBoat),0.6f);
        }

        Transform FindBoat()
        {
            var bc = FindFirstObjectByType<BoatController>();
            if(bc!=null) return bc.transform;
            var go = GameObject.Find("Boat");
            return go!=null? go.transform : null;
        }
        void RebindBoat()=> _boatTr = FindBoat();

        void OnEnable(){ EventBus.Subscribe<BoatCrossingEvent>(_=>{}); }
        void OnDisable(){ EventBus.Unsubscribe<BoatCrossingEvent>(_=>{}); }

        void Spawn()
        {
            if(profile==null) profile = ScriptableObject.CreateInstance<SharkProfile>();
            _sharks = new Shark[profile.sharkCount];
            Vector2 mn = _ocean!=null? _ocean.ChannelMin : new Vector2(-500,-60);
            Vector2 mx = _ocean!=null? _ocean.ChannelMax : new Vector2(-180,60);
            for(int i=0;i<_sharks.Length;i++)
            {
                float x = Random.Range(mn.x+profile.spawnMargin, mx.x-profile.spawnMargin);
                float z = Random.Range(mn.y+profile.spawnMargin, mx.y-profile.spawnMargin);
                GameObject go;
                if(sharkPrefab!=null) go = Instantiate(sharkPrefab, new Vector3(x,-0.9f,z), Quaternion.identity, transform);
                else { go = GameObject.CreatePrimitive(PrimitiveType.Cube); go.transform.localScale=new Vector3(1.4f,0.22f,0.42f); }
                go.name=$"Shark_{i}";
                _sharks[i]= new Shark{ tr=go.transform, vel=Random.insideUnitSphere.WithY(0).normalized*profile.cruiseSpeed, state=State.Patrol, phase=Random.value*6.28f };
                _sharks[i].tr.position = new Vector3(x,-0.9f,z);
            }
        }

        void Update()
        {
            if(_sharks==null) return;
            if(_boatTr==null) _boatTr = FindBoat();
            if(_playerTr==null) _playerTr = FindFirstObjectByType<Player.PlayerController>()?.transform;

            float dt = Time.deltaTime;
            bool isNight = _dayNight!=null && _dayNight.CurrentPhase==DayPhase.Night;
            float storm01= _weather!=null? _weather.StormIntensity:0f;
            float aggro  = 1f * (isNight?profile.nightAggression:1f) * Mathf.Lerp(1f,profile.stormAggression, storm01);

            // route bias (boat crossing history)
            string favSide=null; Vector2 favPos=Vector2.zero;
            int mainC = PlayerPrefs.GetInt("LL_Cross_Main",0), isletC=PlayerPrefs.GetInt("LL_Cross_Islet",0);
            if(mainC > isletC*1.35f) favSide="Main";
            else if(isletC > mainC*1.35f) favSide="Islet";
            if(favSide!=null)
            {
                var cfg = FindFirstObjectByType<World.WorldConfig>();
                if(cfg!=null) favPos = favSide=="Main"? cfg.mainIslandDock : cfg.isletDock;
            }

            for(int i=0;i<_sharks.Length;i++)
            {
                var s = _sharks[i];
                if(s.tr==null) continue;

                // repel
                if(s.state==State.Repelled)
                {
                    s.repelT -= dt;
                    if(s.repelT<=0f) s.state=State.Patrol;
                    else { s.vel = Vector3.Lerp(s.vel, -s.vel.normalized*profile.cruiseSpeed, dt*1.3f); Step(ref s,dt); _sharks[i]=s; continue; }
                }

                // route-aggro nudge
                if(favSide!=null && s.state==State.Patrol)
                {
                    Vector2 xz=new Vector2(s.tr.position.x,s.tr.position.z);
                    if(Vector2.Distance(xz,favPos)<68f) aggro *= profile.routeAggression;
                }

                bool boatIn = _boatTr!=null && Vector3.Distance(s.tr.position,_boatTr.position) < profile.detectBoatRadius*aggro;
                bool swimIn = false;
                if(_playerTr!=null)
                {
                    bool inWater = _playerTr.position.y < -0.15f;
                    swimIn = inWater && Vector3.Distance(s.tr.position,_playerTr.position) < profile.detectSwimRadius*aggro;
                }

                if(swimIn)
                {
                    s.state = State.Attack;
                    s.vel = (_playerTr.position - s.tr.position).WithY(0).normalized * profile.attackSpeed;
                    if(Vector3.Distance(s.tr.position,_playerTr.position) < profile.attackRadius) DamageSwimmer(dt);
                    Telegraph(s.tr.position, 0.45f);
                }
                else if(boatIn && s.cooldown<=0f)
                {
                    if(s.state!=State.Circle && s.state!=State.Stalk) s.state=State.Circle;
                    if(s.state==State.Circle)
                    {
                        s.t += dt;
                        Vector3 toBoat = (_boatTr.position - s.tr.position).WithY(0);
                        Vector3 orbit = new Vector3(-toBoat.z,0,toBoat.x).normalized * profile.cruiseSpeed;
                        float t = Mathf.PingPong(s.t*0.4f,1f);
                        s.vel = Vector3.Slerp(s.vel, Vector3.Lerp(toBoat.normalized*profile.cruiseSpeed, orbit, t), dt*2f);
                        Telegraph(s.tr.position, 0.25f);
                        if(s.t > profile.circleTime + Random.value*2f){ s.state=State.Attack; s.t=0f; }
                    }
                    else
                    {
                        s.vel = (_boatTr.position - s.tr.position).WithY(0).normalized * (s.state==State.Attack?profile.attackSpeed:profile.stalkSpeed);
                        if(Vector3.Distance(s.tr.position,_boatTr.position) < profile.attackRadius) HitBoat(ref s);
                    }
                }
                else
                {
                    if(s.state==State.Attack||s.state==State.Stalk||s.state==State.Circle) s.state=State.Patrol;
                    Patrol(ref s,dt);
                }

                s.cooldown = Mathf.Max(0f, s.cooldown - dt);
                YawToVel(ref s,dt);
                Step(ref s,dt);
                ClampChannel(ref s);
                Bob(ref s);
                _sharks[i]=s;
            }
        }

        void Telegraph(Vector3 sharkPos, float intensity)
        {
            // camera shake hint: nudge main camera a touch (ThirdPersonCamera lerps back)
            var cam = Camera.main;
            if(cam==null) return;
            // very cheap: one-frame offset driven by intensity; ThirdPersonCamera re-centres next frame
            if(intensity>0.2f && Random.value < 0.04f * intensity * 6f)
                cam.transform.position += Random.insideUnitSphere * (profile.shakeOnApproach * intensity);
        }

        void Patrol(ref Shark s,float dt)
        {
            s.t += dt;
            if(s.t>4f && Random.value<0.06f){ s.vel = Quaternion.Euler(0,Random.Range(-50f,50f),0)*s.vel; s.t=0f; }
            if(s.vel.magnitude<0.45f) s.vel = Random.insideUnitSphere.WithY(0).normalized*profile.cruiseSpeed;
            s.vel = Vector3.MoveTowards(s.vel, s.vel.normalized*profile.cruiseSpeed, dt*1.1f);
        }

        void Step(ref Shark s,float dt){ s.tr.position += s.vel*dt; }
        void Bob(ref Shark s){ var p=s.tr.position; p.y = -0.9f + Mathf.Sin(Time.time*profile.bobHz + s.phase)*profile.bobAmp + Mathf.Sin(Time.time*profile.bobHz*1.7f + s.phase*0.7f)*profile.finWobbleAmp; s.tr.position=p; }
        void YawToVel(ref Shark s,float dt){ if(s.vel.sqrMagnitude>0.01f) s.tr.rotation = Quaternion.Slerp(s.tr.rotation, Quaternion.LookRotation(s.vel.WithY(0).normalized, Vector3.up), dt*(profile.turnRateDeg/60f)*6f); }

        void ClampChannel(ref Shark s)
        {
            Vector2 mn=_ocean!=null?_ocean.ChannelMin:new Vector2(-500,-60);
            Vector2 mx=_ocean!=null?_ocean.ChannelMax:new Vector2(-180,60);
            float pad=6f; var p=s.tr.position;
            if(p.x<mn.x+pad){ p.x=mn.x+pad; s.vel.x=Mathf.Abs(s.vel.x); }
            if(p.x>mx.x-pad){ p.x=mx.x-pad; s.vel.x=-Mathf.Abs(s.vel.x); }
            if(p.z<mn.y+pad){ p.z=mn.y+pad; s.vel.z=Mathf.Abs(s.vel.z); }
            if(p.z>mx.y-pad){ p.z=mx.y-pad; s.vel.z=-Mathf.Abs(s.vel.z); }
            s.tr.position=p;
        }

        void HitBoat(ref Shark s)
        {
            var boat = FindFirstObjectByType<BoatController>();
            if(boat!=null) boat.ApplySharkDamage();
            EventBus.Publish(new SharkHitBoatEvent(s.tr.position));
            s.cooldown = profile.attackCooldown; s.state=State.Patrol; s.t=0f; s.vel = -s.vel*0.35f;
            Telegraph(s.tr.position, 0.9f);
            for(int j=0;j<_sharks.Length;j++) if(j!=System.Array.IndexOf(_sharks,s) && Vector3.Distance(_sharks[j].tr.position, s.tr.position)<38f)
                if(_sharks[j].state==State.Patrol){ _sharks[j].state=State.Stalk; _sharks[j].vel=(s.tr.position-_sharks[j].tr.position).WithY(0).normalized*profile.stalkSpeed; }
        }

        void DamageSwimmer(float dt)
        {
            var stats = FindFirstObjectByType<Survival.PlayerStats>();
            if(stats==null) return;
            stats.RestoreHealth(-profile.swimDamagePerSec * dt * 100f);
            if(Random.value<0.12f) SplashAt(_playerTr.position,0.7f);
            EventBus.Publish(new SharkHitSwimmerEvent(_playerTr.position));
            Telegraph(_playerTr.position, 1f);
        }
    }

    public readonly struct SharkHitBoatEvent { public readonly Vector3 Pos; public SharkHitBoatEvent(Vector3 p){ Pos=p; } }
    public readonly struct SharkHitSwimmerEvent { public readonly Vector3 Pos; public SharkHitSwimmerEvent(Vector3 p){ Pos=p; } }

    static class VecExt2 { public static Vector3 WithY(this Vector3 v,float y)=>new Vector3(v.x,y,v.z); }
}
