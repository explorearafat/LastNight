// Cheap moss/algae/wetness URP shader graph helper
// Define URP ShaderGraphs in project: LL_BrickMoss and LL_TerrainMoss are created as
// template URP Lit graphs with vertex-color-driven moss. This C# drives wetness and
// passes world params. Works without custom HLSL — vertex color G = moss, B = algae, R = wetness.

using UnityEngine;

namespace LastLight.World
{
    public sealed class MossWetnessDriver : MonoBehaviour
    {
        [Header("Refs (auto-found)")]
        [SerializeField] WeatherManager weather;

        [Header("Global shader params driven per frame")]
        [Range(0,1)] [SerializeField] float mossStrength = 0.9f;   // static look, not weather-driven
        [Range(0,1)] [SerializeField] float algaeHeight = 0.22f;   // metres above waterline where algae starts
        [SerializeField] Color mossColor  = new Color(0.18f, 0.32f, 0.14f, 1f);
        [SerializeField] Color algaeColor = new Color(0.16f, 0.28f, 0.18f, 1f);

        void Awake()
        {
            if (weather == null) weather = FindFirstObjectByType<WeatherManager>();
        }

        void Update()
        {
            float storm01 = weather != null ? weather.StormIntensity : 0f;
            float wet01 = Mathf.Clamp01(storm01 * 1.2f + (weather != null && weather.CurrentState == Core.WeatherState.Rain ? 0.35f : 0f));
            // Wetness lowers roughness, darkens a bit. Cheap: drive global floats that the shader graphs read.
            Shader.SetGlobalFloat("_LL_Wetness01", wet01);
            Shader.SetGlobalFloat("_LL_Storm01", storm01);
            Shader.SetGlobalFloat("_LL_MossStrength", mossStrength);
            Shader.SetGlobalFloat("_LL_AlgaeHeight", algaeHeight);
            Shader.SetGlobalColor("_LL_MossColor", mossColor);
            Shader.SetGlobalColor("_LL_AlgaeColor", algaeColor);
            // roughness scale: wet surfaces are shinier (lower roughness)
            Shader.SetGlobalFloat("_LL_RoughnessScale", Mathf.Lerp(1f, 0.55f, wet01));
        }

        // Static helper so brick kit meshes can carry moss in vertex colors without this component
        public static void PaintVertexMoss(GameObject root)
        {
            // Walk meshes and write vertex color: G = upward-facing amount (moss), B = low-height (algae), R = ambient occlusion proxy
            var filters = root.GetComponentsInChildren<MeshFilter>(true);
            foreach (var mf in filters)
            {
                var mesh = mf.sharedMesh;
                if (mesh == null) continue;
                var colors = new Color[mesh.vertexCount];
                var verts = mesh.vertices;
                var norms = mesh.normals;
                if (norms.Length != verts.Length) mesh.RecalculateNormals();
                norms = mesh.normals;
                for (int i=0;i<verts.Length;i++)
                {
                    float upward = Mathf.Clamp01(norms[i].y);           // 1 on top faces
                    float algae  = Mathf.Clamp01(1f - (verts[i].y + 1.5f) * 0.4f); // bottom faces near waterline
                    float ao     = 0.85f + upward * 0.15f;               // cheap AO
                    // G=moss on upward, B=algae near bottom, R=ao, A=1
                    colors[i] = new Color(ao, upward * 0.9f, algae * 0.7f, 1f);
                }
                mesh.colors = colors;
            }
        }
    }
}
