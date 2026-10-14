// Last Light — World/WorldHints.cs
// Diegetic hints: smoke, distant light, gull/creak sounds pointing toward hero spots — no markers.

using UnityEngine;

namespace LastLight.World
{
    public sealed class WorldHints : MonoBehaviour
    {
        [Header("Hint spots (world XZ) — smoke column + warm light + audio cue")]
        [SerializeField] Vector3 castleHint = new Vector3(40f, 0, 140f);
        [SerializeField] Vector3 bunkerHint = new Vector3(-140f, 0, 90f);
        [SerializeField] Vector3 caveHint   = new Vector3(110f, 0, 280f);
        [SerializeField] Vector3 canneryHint= new Vector3(-180f,0,-140f);

        [Header("Particle (assign smoke prefab, or will use primitives)")]
        [SerializeField] GameObject smokePrefab;

        [Header("Light hint")]
        [SerializeField] Color warmHintColor = new Color(1f, 0.52f, 0.18f, 1f);
        [SerializeField] float hintLightRange = 55f;
        [SerializeField] float hintLightIntensity = 1.1f;

        GameObject _root;

        void Awake()=> SpawnIfNeeded();

        void SpawnIfNeeded()
        {
            if (GameObject.Find("WorldHints_Root") != null) return;
            _root = new GameObject("WorldHints_Root");
            SpawnHint("Hint_Castle",  castleHint + Vector3.up*2.2f, smokePrefab, new Color(0.55f,0.52f,0.48f,0.55f));
            SpawnHint("Hint_Bunker",  bunkerHint  + Vector3.up*1.2f, smokePrefab, new Color(0.45f,0.45f,0.45f,0.52f));
            SpawnHint("Hint_Cave",    caveHint    + Vector3.up*3.0f, null,        new Color(0.68f,0.62f,0.42f,0.42f));
            SpawnHint("Hint_Cannery", canneryHint + Vector3.up*1.8f, smokePrefab, new Color(0.60f,0.55f,0.50f,0.50f));
        }

        void SpawnHint(string name, Vector3 pos, GameObject prefab, Color smokeTint)
        {
            GameObject go;
            if (prefab != null) { go = Instantiate(prefab, pos, Quaternion.identity, _root.transform); go.name=name; }
            else
            {
                go = new GameObject(name); go.transform.SetParent(_root.transform, false); go.transform.position = pos;
                var ps = go.AddComponent<ParticleSystem>();
                var main = ps.main;
                main.startLifetime = 8f; main.startSpeed = 0.8f; main.startSize = 2.5f; main.startColor = smokeTint;
                main.simulationSpace = ParticleSystemSimulationSpace.World;
                main.maxParticles = 24;
                var sh = ps.shape; sh.enabled=true; sh.shapeType=ParticleSystemShapeType.Cone; sh.angle=6f; sh.radius=0.35f;
                var em = ps.emission; em.rateOverTime = 1.2f;
                var rend = ps.GetComponent<ParticleSystemRenderer>();
                rend.material = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
                rend.material.SetColor("_BaseColor", smokeTint);
                // warm point light nearby
                var lh = new GameObject(name+"_Light"); lh.transform.SetParent(go.transform,false); lh.transform.localPosition=Vector3.down*1.4f;
                var l = lh.AddComponent<Light>(); l.type=LightType.Point; l.range=hintLightRange; l.intensity=hintLightIntensity; l.color=warmHintColor; l.shadows=LightShadows.None;
                l.intensity *= 0.35f; // subtle
            }
        }
    }
}
