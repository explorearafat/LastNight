// Last Light — Player/CharacterSetup.cs
// One-time fixup for an imported humanoid: scale, root-motion, skinning sanity.
// Attach to the imported character root. Runs in Awake and then disables itself.
// Safe to leave on a placeholder capsule — it no-ops when no humanoid Animator is found.

using UnityEngine;

namespace LastLight.Player
{
    public sealed class CharacterSetup : MonoBehaviour
    {
        [Header("Fixups")]
        [Tooltip("If the imported model is not 1 unit ≈ 1 metre, set the correction here (1 = no change).")]
        [SerializeField] float modelScaleCorrection = 1f;
        [Tooltip("Disable Animator root motion so CharacterController owns translation.")]
        [SerializeField] bool disableRootMotion = true;
        [Tooltip("Clamp skinned mesh bone count warning threshold (mobile budget).")]
        [SerializeField] int maxBonesPerVertex = 2;

        [Header("Collider")]
        [SerializeField] float controllerHeight = 1.8f;
        [SerializeField] float controllerRadius = 0.32f;

        void Awake()
        {
            var animator = GetComponentInChildren<Animator>();
            var cc = GetComponent<CharacterController>();

            if (animator != null)
            {
                if (disableRootMotion) animator.applyRootMotion = false;
                animator.cullingMode = AnimatorCullingMode.CullCompletely;

                // Mobile sanity: warn about heavy skinning
                var renderers = GetComponentsInChildren<SkinnedMeshRenderer>();
                foreach (var smr in renderers)
                {
                    smr.quality = SkinQuality.Bone1; // mobile: 1 bone/vertex where possible
                    if (smr.bones != null && smr.bones.Length > 56)
                        Debug.LogWarning($"[CharacterSetup] {smr.name} has {smr.bones.Length} bones — consider reducing for mobile.", smr);
                    smr.updateWhenOffscreen = false;
                }

                // Ensure full body is rendered (no frustum culling pop from tight bounds)
                foreach (var smr in renderers)
                    smr.localBounds = new Bounds(Vector3.up * 0.9f, new Vector3(1f, 2f, 1f));
            }

            if (modelScaleCorrection != 1f)
                transform.localScale *= modelScaleCorrection;

            if (cc != null)
            {
                cc.height = controllerHeight;
                cc.radius = controllerRadius;
                cc.center = new Vector3(0, controllerHeight * 0.5f, 0);
            }

            enabled = false; // one-shot
        }
    }
}
