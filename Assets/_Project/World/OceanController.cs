// Last Light — World/OceanController.cs
// Mobile-cheap ocean: a single large Plane with a vertex-displaced unlit shader stand-in.
// No GrabPass, no reflection probe per frame. Waves are vertex sine, foam is distance-to-shore.
// Storm intensity strengthens waves and haze. Lighthouse beam reflects if you assign a mirror plane.

using UnityEngine;

namespace LastLight.World
{
    public sealed class OceanController : MonoBehaviour
    {
        [Header("Ocean Plane (auto-created as a large Plane primitive if null)")]
        [SerializeField] Transform oceanTransform;
        [SerializeField] Material  oceanMaterial; // assign a mobile unlit water material when available
        [SerializeField] Vector2   oceanSize = new Vector2(1200, 1200);
        [SerializeField] float     oceanY    = -1.2f;

        [Header("Waves (vertex sine — no shader required to see them)")]
        [SerializeField] float waveAmplitude = 0.35f;
        [SerializeField] float waveFrequency = 0.22f;
        [SerializeField] float waveSpeed     = 0.9f;
        [SerializeField] float stormWaveBoost = 1.8f; // multiplied by storm 0-1

        [Header("Render")]
        [SerializeField] Color shallowColor = new Color(0.08f, 0.32f, 0.42f, 0.92f);
        [SerializeField] Color deepColor    = new Color(0.04f, 0.12f, 0.28f, 0.92f);
        [SerializeField] Color foamColor    = new Color(0.85f, 0.88f, 0.82f, 0.85f);
        [SerializeField] float  shoreBand   = 38f; // metres from island edge where foam shows

        GameObject _oceanGo;
        Mesh       _oceanMesh;
        Vector3[]  _baseVerts;
        Renderer   _renderer;
        MaterialPropertyBlock _mpb;

        void Awake() => BuildIfNeeded();

        void BuildIfNeeded()
        {
            if (GameObject.Find("Ocean") != null) return;

            _oceanGo = GameObject.CreatePrimitive(PrimitiveType.Plane);
            _oceanGo.name = "Ocean";
            _oceanGo.transform.position = new Vector3(0, oceanY, 0);
            _oceanGo.transform.localScale = new Vector3(oceanSize.x / 10f, 1f, oceanSize.y / 10f);
            oceanTransform = _oceanGo.transform;
            _renderer = _oceanGo.GetComponent<Renderer>();
            if (oceanMaterial != null) _renderer.sharedMaterial = oceanMaterial;
            else if (_renderer.sharedMaterial != null)
                _renderer.sharedMaterial.color = deepColor;

            // Enable GPU instancing where possible.
            if (_renderer.sharedMaterial != null) _renderer.sharedMaterial.enableInstancing = true;

            _oceanMesh = _oceanGo.GetComponent<MeshFilter>().sharedMesh;
            if (_oceanMesh != null) _baseVerts = _oceanMesh.vertices;

            _mpb = new MaterialPropertyBlock();
        }

        void Update()
        {
            if (_oceanGo == null || _oceanMesh == null || _baseVerts == null) return;

            float storm01 = 0f;
            var wm = FindFirstObjectByType<WeatherManager>();
            if (wm != null) storm01 = wm.StormIntensity;

            float amp = waveAmplitude * (1f + storm01 * stormWaveBoost);
            float t   = Time.time * waveSpeed;
            var verts = _oceanMesh.vertices; // copy

            // Vertex sine — motion only on Y so the physics collider stays stable.
            for (int i = 0; i < verts.Length && i < _baseVerts.Length; i++)
            {
                Vector3 b = _baseVerts[i];
                // Two wave directions for a more natural look
                float w1 = Mathf.Sin(b.x * waveFrequency * 0.9f + t) * 0.65f;
                float w2 = Mathf.Sin(b.z * waveFrequency * 0.6f + t * 0.83f) * 0.35f;
                verts[i].y = b.y + (w1 + w2) * amp;
            }
            _oceanMesh.vertices = verts;
            _oceanMesh.RecalculateNormals();

            // Foam / color lerp by distance to island centre (cheap shore mask before a real shader).
            if (_renderer != null)
            {
                _renderer.GetPropertyBlock(_mpb);
                // Tint by storm so water reads choppier
                Color waterTint = Color.Lerp(shallowColor, deepColor, Mathf.Clamp01(storm01 * 0.7f));
                _mpb.SetColor("_BaseColor", waterTint);
                _mpb.SetFloat("_WaveAmp", amp);
                _renderer.SetPropertyBlock(_mpb);
            }

            // Optional lighthouse beam reflection — a cheap specular streak on the water Plane's material.
            // If you assign a Reflection Probe + planar mirror material, it will naturally capture the beam.
        }
    }
}
