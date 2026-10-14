// Patch LighthouseAreaAssembler for islet position; keep original file but offset its root.
using UnityEngine;

namespace LastLight.World
{
    // Tiny patch that re-parents the assembled LighthouseArea_Root to the islet center
    // so Day 4 FBX don't need re-export. Runs after LighthouseAreaAssembler.Awake ordering.
    [DefaultExecutionOrder(10)]
    public sealed class IsletPositionPatch : MonoBehaviour
    {
        [SerializeField] Vector3 isletCenter = new Vector3(-520f, 0.15f, 0f);

        void Start()
        {
            var root = GameObject.Find("LighthouseArea_Root");
            if (root != null) root.transform.position = isletCenter;
            var isletGround = GameObject.Find("Islet_Ground");
            if (isletGround != null) isletGround.transform.position = new Vector3(isletCenter.x, 0.15f, isletCenter.z);
        }
    }
}
