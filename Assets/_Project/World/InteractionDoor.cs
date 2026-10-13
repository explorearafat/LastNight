// Last Light — World/InteractionDoor.cs
// Simple door that opens/closes on interact. No animation clip required — it lerps.

using UnityEngine;
using LastLight.Core;

namespace LastLight.World
{
    [RequireComponent(typeof(Collider))]
    public sealed class InteractionDoor : MonoBehaviour, IInteractable
    {
        [SerializeField] Transform doorPivot; // rotate this; if null, this transform rotates
        [SerializeField] float openAngle = 92f;
        [SerializeField] float speed     = 220f;

        public string Prompt => _open ? "Close door" : "Open door";
        public Vector3 InteractionPoint => transform.position;
        public bool CanInteract => true;

        bool  _open;
        float _yaw;

        void Reset()
        {
            var col = GetComponent<Collider>();
            col.isTrigger = true;
        }

        void Update()
        {
            float target = _open ? openAngle : 0f;
            _yaw = Mathf.MoveTowards(_yaw, target, speed * Time.deltaTime);
            var pivot = doorPivot != null ? doorPivot : transform;
            pivot.localRotation = Quaternion.Euler(0, _yaw, 0);
        }

        public void Interact(GameObject instigator)
        {
            _open = !_open;
            instigator.GetComponentInParent<Player.PlayerVisuals>()?.TriggerInteract();
        }
    }
}
