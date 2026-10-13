// Last Light — Audio/InteriorAudioZone.cs
// Small trigger that toggles interior muffling + ambience when the player enters/exits
// the lighthouse interior (or any enclosed space).

using UnityEngine;

namespace LastLight.Audio
{
    [RequireComponent(typeof(Collider))]
    public sealed class InteriorAudioZone : MonoBehaviour
    {
        void Reset() => GetComponent<Collider>().isTrigger = true;

        void OnTriggerEnter(Collider other)
        {
            if (other.GetComponent<Player.PlayerController>() == null) return;
            var am = FindFirstObjectByType<AudioManager>();
            am?.SetInterior(true);
        }

        void OnTriggerExit(Collider other)
        {
            if (other.GetComponent<Player.PlayerController>() == null) return;
            var am = FindFirstObjectByType<AudioManager>();
            am?.SetInterior(false);
        }
    }
}
