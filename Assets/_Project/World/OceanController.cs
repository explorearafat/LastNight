// Last Light — World/OceanController.cs — Day 5: archipelago ocean with threatening channel.
// Extends the mobile-cheap plane + vertex sine; adds depth/channel darkness and shore foam logic.
// No GrabPass, no reflection probe per frame.

using UnityEngine;

namespace LastLight.World
{
    public sealed class OceanController : MonoBehaviour
    {
        [Header("Ocean Plane (auto-created as a large Plane primitive if null)")]
        [SerializeField] Transform oceanTransform;
        [SerializeField] Material  oceanMaterial;
        [SerializeField] Vector2   oceanSize = new Vector2(1400, 1400);
        [SerializeField] float     oceanY    = -0.9f;

        [Header("Waves (vertex sine — no shader required)")]
        [SerializeField] float waveAmplitude = 0.35f;
        [SerializeField] float waveFrequency = 0.22f;
        [SerializeField] float waveSpeed     = 0.9f;
        [SerializeField] float stormWaveBoost = 1.8f;

        [Header("Colors (URP Lit vertex color / MPB path keeps it cheap)")]
        [SerializeField] Color shallowColor = new Color(0.12f, 0.38f, 0.50f, 0.92f);
        [SerializeField] Color deepColor    = new Color(0.04f, 0.10f, 0.22f, 0.96f);
        [SerializeField] Color channelColor = new Color(0.02f, 0.07f, 0.16f, 0.98f); // shark water between islet+island
        [SerializeField] Color foamColor    = new Color(0.85f, 0.88f, 0.82f, 0.85f);
        [SerializeField] float  shoreBand   = 38f;

        [Header("Channel (shark water) — world XZ rect that tints darker")]
        [SerializeField] Vector2 channelMin = new Vector2(-500f, -60f);
        [SerializeField] Vector2 channelMax = new Vector2(-180f,  60f);
        [SerializeField] float   channelDarkAtStorm = 0.25f;

        GameObject _oceanGo;
        Mesh       _oceanMesh;
        Vector3[]  _baseVerts, _worldXZ;
        Vector3    _planeScale;
        Vector3    _planePos;
        Renderer   _renderer;
        MaterialPropertyBlock _mpb;

        // Exposed so sharks/boat know where the channel is
        public Vector2 ChannelMin => channelMin;
        public Vector2 ChannelMax => channelMax;
        public bool IsInsideChannel(Vector2 xz) =>
            xz.x >= channelMin.x && xz.x <= channelMax.x && xz.y >= channelMin.y && xz.y <= channelMax.y;

        void Awake() => BuildIfNeeded();

        void BuildIfNeeded()
        {
            if (GameObject.Find("Ocean") != null) { TryCache(); return; }
            _oceanGo = GameObject.CreatePrimitive(PrimitiveType.Plane);
            _oceanGo.name = "Ocean";
            _oceanGo.transform.position = new Vector3(0, oceanY, 0);
            _oceanGo.transform.localScale = new Vector3(oceanSize.x/10f, 1f, oceanSize.y/10f);
            oceanTransform = _oceanGo.transform;
            _renderer = _oceanGo.GetComponent<Renderer>();
            if (oceanMaterial != null) _renderer.sharedMaterial = oceanMaterial;
            else if (_renderer.sharedMaterial != null) _renderer.sharedMaterial.color = deepColor;
            if (_renderer.sharedMaterial != null) _renderer.sharedMaterial.enableInstancing = true;
            _planePos = _oceanGo.transform.position;
            _planeScale = _oceanGo.transform.localScale;
            _oceanMesh = _oceanGo.GetComponent<MeshFilter>().sharedMesh;
            if (_oceanMesh != null) CacheVerts();
            _mpb = new MaterialPropertyBlock();
        }

        void TryCache()
        {
            var go = GameObject.Find("Ocean");
            if (go == null) return;
            _oceanGo = go; oceanTransform = go.transform;
            _renderer = go.GetComponent<Renderer>();
            _planePos = go.transform.position; _planeScale = go.transform.localScale;
            _oceanMesh = go.GetComponent<MeshFilter>()?.sharedMesh;
            if (_oceanMesh != null) CacheVerts();
            _mpb = new MaterialPropertyBlock();
        }

        void CacheVerts()
        {
            _baseVerts = _oceanMesh.vertices;
            _worldXZ = new Vector3[_baseVerts.Length];
            for (int i=0;i<_baseVerts.Length;i++)
            {
                Vector3 v = _baseVerts[i];
                _worldXZ[i] = new Vector3(v.x * _planeScale.x * 10f + _planePos.x, 0, v.z * _planeScale.z * 10f + _planePos.z);
            }
        }

        void Update()
        {
            if (_oceanGo == null || _oceanMesh == null || _baseVerts == null) return;

            float storm01 = 0f;
            var wm = FindFirstObjectByType<WeatherManager>();
            if (wm != null) storm01 = wm.StormIntensity;

            float amp = waveAmplitude * (1f + storm01 * stormWaveBoost);
            float channelFactor = Mathf.Lerp(1f, 1.6f, storm01); // channel waves more violent in storm
            float t   = Time.time * waveSpeed;
            var verts  = _oceanMesh.vertices;
            var colors = _oceanMesh.colors;
            bool hasColors = colors != null && colors.Length == verts.Length;

            for (int i=0;i<verts.Length && i < _baseVerts.Length;i++)
            {
                Vector3 b = _baseVerts[i];
                Vector2 xz = new Vector2(_worldXZ[i].x, _worldXZ[i].z);
                bool inChannel = IsInsideChannel(xz);
                float localAmp = amp * (inChannel ? channelFactor : 1f);
                float w1 = Mathf.Sin(b.x * waveFrequency * 0.9f + t) * 0.65f;
                float w2 = Mathf.Sin(b.z * waveFrequency * 0.6f + t * 0.83f) * 0.35f;
                verts[i].y = b.y + (w1 + w2) * localAmp;
                if (hasColors)
                {
                    float shoreDist = ShoreDistance01(xz);
                    Color c = inChannel ? channelColor : Color.Lerp(deepColor, shallowColor, 1f - shoreDist);
                    if (inChannel && storm01 > 0.4f) c = Color.Lerp(c, channelColor * 0.7f, (storm01-0.4f)/0.6f);
                    // foam near shore (not channel)
                    if (!inChannel && shoreDist < 0.22f) c = Color.Lerp(foamColor, c, shoreDist/0.22f);
                    colors[i] = c;
                }
            }
            _oceanMesh.vertices = verts;
            if (hasColors) _oceanMesh.colors = colors;
            _oceanMesh.RecalculateNormals();

            // MPB tint so even without vertex colors the water reads
            if (_renderer != null)
            {
                _renderer.GetPropertyBlock(_mpb);
                _mpb.SetColor("_BaseColor", Color.Lerp(deepColor, channelColor, storm01 * channelDarkAtStorm));
                _mpb.SetFloat("_WaveAmp", amp);
                _renderer.SetPropertyBlock(_mpb);
            }
        }

        float ShoreDistance01(Vector2 xz)
        {
            // distance to nearest landmass edge (islet or main island)
            float dIslet = (xz - new Vector2(-520f, 0f)).magnitude;
            float dMain  = Mathf.Min(
                (xz - new Vector2(0f, 0f)).magnitude - 140f, // rough main island radius
                Mathf.Abs(xz.x + 210f) < 80f && Mathf.Abs(xz.y) < 80f ? 0f : 999f
            );
            float dChannel = IsInsideChannel(xz) ? 999f : Mathf.Min(dIslet - 75f, 999f);
            float d = Mathf.Min(dMain / 180f, dChannel / shoreBand, 1f);
            return Mathf.Clamp01(d);
        }
    }
}
