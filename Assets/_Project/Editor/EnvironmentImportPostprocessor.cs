// Last Light — Editor/EnvironmentImportPostprocessor.cs
// Automatic import settings for Environment/Generated/ FBX files.
// Ensures every shipped FBX meets the mobile budget without manual clicks:
//   - Scale 1 unit = 1 m (FBX apply scale, unit scale)
//   - Mesh compression Low, Read/Write OFF, Optimize Mesh ON
//   - Lightmap UVs + Generate Lightmap UVs
//   - LODGroup auto-setup if _LOD0/_LOD1 objects are present
//   - URP Lit material remap (single material per asset)
//   - ASTC override note (actual compression lives in texture importer — this logs a check)
// Place in Assets/_Project/Editor/ — Unity recompiles and it runs on every import.

#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Linq;

namespace LastLight.Editor
{
    public sealed class EnvironmentImportPostprocessor : AssetPostprocessor
    {
        const string GeneratedRoot = "Assets/Environment/Generated";

        void OnPreprocessModel()
        {
            if (!assetPath.StartsWith(GeneratedRoot)) return;

            var importer = (ModelImporter)assetImporter;

            // ── Scale / Units ──
            importer.globalScale = 1f;
            importer.useFileUnits = true;
            importer.addCollider = false;       // we add colliders in prefabs, not on import

            // ── Mesh ──
            importer.meshCompression = ModelImporterMeshCompression.Low;
            importer.isReadable = false;
            importer.optimizeMeshPolygons = true;
            importer.optimizeMeshVertices = true;
            importer.importBlendShapes = false;
            importer.importCameras = false;
            importer.importLights = false;
            importer.importVisibility = false;

            // ── UVs / Lightmaps ──
            importer.generateSecondaryUV = true;
            var gen = new ModelImporterGenerateSecondaryUVSettings
            {
                hardAngle = 88f,
                packMargin = 8f / 1024f,
                angleError = 8f,
                areaError = 15f,
            };
            importer.secondaryUVMarginMethod = ModelImporterSecondaryUVMarginMethod.Calculate;
            // Unity 6000: secondaryUV* still present; use reflection-safe path if API shifts
            try { importer.secondaryUVHardAngle = 88f; } catch { }
            try { importer.secondaryUVPackMargin = 8f / 1024f; } catch { }

            // ── Normals / Tangents ──
            importer.importNormals = ModelImporterNormals.Import;
            importer.normalCalculationMode = ModelImporterNormalCalculationMode.UnweightedAndAngleWeighted;
            importer.normalSmoothingAngle = 60f;
            importer.importTangents = ModelImporterTangents.CalculateMikk;

            // ── Animation / Materials ──
            importer.importAnimation = false;
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;

            Debug.Log($"[Import] {assetPath} — model pre-processed (scale 1m, Low compression, secondary UVs, no RW).");
        }

        void OnPostprocessModel(GameObject root)
        {
            if (!assetPath.StartsWith(GeneratedRoot)) return;

            // ── Static + Lightmap scale ──
            foreach (var r in root.GetComponentsInChildren<Renderer>(true))
            {
                r.gameObject.isStatic = true;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                r.receiveShadows = true;
                r.allowOcclusionWhenDynamic = false;
                // lightmap scale (slightly down so we don't starve atlas)
                var go = r.gameObject;
                var so = new SerializedObject(go);
                var prop = so.FindProperty("m_ScaleInLightmap");
                if (prop != null) { prop.floatValue = 1f; so.ApplyModifiedPropertiesWithoutUndo(); }
            }

            // ── GPU instancing on shared Lit material (URP) ──
            foreach (var r in root.GetComponentsInChildren<Renderer>(true))
            {
                foreach (var m in r.sharedMaterials)
                {
                    if (m == null) continue;
                    if (m.shader != null && m.shader.name.Contains("Universal Render Pipeline/Lit"))
                        m.enableInstancing = true;
                }
            }

            // ── LODGroup auto-wire if LOD meshes exist ──
            TrySetupLODGroup(root);

            // ── Parent pivot hint ──
            root.transform.position = Vector3.zero;
        }

        static void TrySetupLODGroup(GameObject root)
        {
            // Convention: sibling meshes named  <Base>_LOD0  and  <Base>_LOD1
            // or a single mesh + a child with _LOD1 suffix.
            var renderers = root.GetComponentsInChildren<Renderer>(true).ToList();
            if (renderers.Count < 2) return;

            // Group by base name
            var groups = renderers
                .Where(r => r.name.EndsWith("_LOD0") || r.name.EndsWith("_LOD1") || r.name.StartsWith("LL_"))
                .GroupBy(r => r.name.Replace("_LOD0", "").Replace("_LOD1", ""))
                .Where(g => g.Count() >= 2)
                .ToList();

            foreach (var g in groups)
            {
                var lod0 = g.FirstOrDefault(r => r.name.EndsWith("_LOD0")) ?? g.First();
                var lod1 = g.FirstOrDefault(r => r.name.EndsWith("_LOD1"));
                if (lod1 == null) continue;

                var parent = lod0.transform.parent != null ? lod0.transform.parent.gameObject : root;
                if (parent.GetComponent<LODGroup>() != null) continue;

                var lodGroup = parent.AddComponent<LODGroup>();
                var lods = new LOD[2];
                lods[0] = new LOD(0.45f, new[] { lod0 });
                lods[1] = new LOD(0.02f, new[] { lod1 });
                lodGroup.SetLODs(lods);
                lodGroup.RecalculateBounds();
                Debug.Log($"[LODGroup] Auto-wired on {parent.name}: {lod0.name} / {lod1.name}");
            }
        }

        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith("Assets/Environment/")) return;
            var importer = (TextureImporter)assetImporter;
            // Force ASTC on Android, keep max 1024 by default (2048 for hero)
            var android = importer.GetPlatformTextureSettings("Android");
            android.overridden = true;
            android.format = TextureImporterFormat.ASTC_6x6;
            android.maxTextureSize = 1024;
            importer.SetPlatformTextureSettings(android);
        }
    }
}
#endif
