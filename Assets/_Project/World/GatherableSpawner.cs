// Last Light — World/GatherableSpawner.cs
// Placeholder gatherable: a visible primitive the player can interact to collect.
// Mobile-cheap respawn: single timer + SetActive toggle, no pooling or spawning overhead.

using UnityEngine;
using LastLight.Core;

namespace LastLight.World
{
    public sealed class GatherableSpawner : MonoBehaviour, IInteractable
    {
        [Header("Item")]
        [SerializeField] ItemType itemType = ItemType.Wood;
        [SerializeField] int      amount  = 1;
        [SerializeField] float    holdSeconds = 0.45f; // tap-and-hold feel (0 = instant)

        [Header("Respawn")]
        [Tooltip("Seconds until this node respawns. 0 = never.")]
        [SerializeField] float respawnSeconds = 90f;
        [SerializeField] bool  startAvailable = true;

        [Header("Visual (optional)")]
        [SerializeField] GameObject visualRoot; // toggle visibility separately from collider
        [SerializeField] Renderer   mainRenderer;
        [SerializeField] Color      availableTint  = Color.white;
        [SerializeField] Color      depletedTint   = new Color(0.45f, 0.42f, 0.4f, 0.7f);

        // IInteractable
        public string Prompt => _available ? _promptReady : "Depleted";
        public Vector3 InteractionPoint => transform.position;
        public bool CanInteract => _available;

        string _promptReady;
        bool   _available;
        float  _respawnTimer;
        float  _holdTimer;
        bool   _holding;

        void Awake()
        {
            _promptReady = itemType switch
            {
                ItemType.Fuel        => "Pick up fuel",
                ItemType.Wood        => "Gather wood",
                ItemType.Food        => "Gather food",
                ItemType.RepairParts => "Pick up parts",
                _ => "Gather"
            };
            _available = startAvailable;
            ApplyVisual();
        }

        void Update()
        {
            if (_available) return;
            if (respawnSeconds <= 0f) return;
            _respawnTimer -= Time.deltaTime;
            if (_respawnTimer <= 0f) { _available = true; ApplyVisual(); }
        }

        public void Interact(GameObject instigator)
        {
            if (!_available) return;

            // Hold-to-gather: require the button to be held
            if (holdSeconds > 0.01f && !_holding)
            {
                _holding = true; _holdTimer = 0f;
            }
        }

        // Called every frame while the interact button is held.
        // TouchInput can optionally call this via a hold callback; if not called, we auto-complete in one press.
        public void TickHold(float dt, GameObject instigator)
        {
            if (!_available) return;
            if (holdSeconds <= 0.01f) { CompleteGather(instigator); return; }
            _holdTimer += dt;
            if (_holdTimer >= holdSeconds) CompleteGather(instigator);
        }

        public void CancelHold() { _holding = false; _holdTimer = 0f; }

        void CompleteGather(GameObject instigator)
        {
            _holding = false; _holdTimer = 0f;

            var inv = instigator.GetComponentInParent<Survival.Inventory>();
            if (inv == null) inv = FindFirstObjectByType<Survival.Inventory>();

            if (inv != null)
            {
                int added = inv.TryAdd(itemType, amount);
                if (added == 0)
                {
                    Debug.Log("[Gather] Inventory full.");
                    return;
                }
                if (added < amount) Debug.Log($"[Gather] Only {added}/{amount} fit — inventory full.");
            }
            else
            {
                // No inventory — still consume the node (direct effect fallback)
                ApplyDirectEffect(instigator);
            }

            _available = false;
            _respawnTimer = respawnSeconds > 0f ? respawnSeconds : float.MaxValue;
            ApplyVisual();
            Debug.Log($"[Gather] {itemType} x{amount} gathered.");

            // Track for adaptive storm (which resource they farm most)
            var tracker = FindFirstObjectByType<AdaptiveStormDirector>();
            tracker?.RecordGather(itemType);
        }

        void ApplyDirectEffect(GameObject instigator)
        {
            var stats = instigator.GetComponentInParent<Survival.PlayerStats>();
            if (stats == null) return;
            switch (itemType)
            {
                case ItemType.Food: stats.RestoreHunger(22f); break;
                case ItemType.Fuel:
                    var lh = FindFirstObjectByType<Lighthouse.LighthouseState>();
                    lh?.Refuel(18f);
                    break;
            }
        }

        void ApplyVisual()
        {
            if (visualRoot != null) visualRoot.SetActive(_available || respawnSeconds > 0f);
            if (mainRenderer != null)
            {
                // Cheap visual feedback without extra materials.
                if (mainRenderer.material != null)
                    mainRenderer.material.color = _available ? availableTint : depletedTint;
            }
            // Squash depleted nodes slightly so the player reads "picked over".
            transform.localScale = _available ? Vector3.one : new Vector3(0.85f, 0.45f, 0.85f);
        }

        // Editor helper: spawn a ring of gatherables from WorldBuilder.
        public static GatherableSpawner CreatePlaceholder(ItemType type, Vector3 pos, Transform parent = null)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = $"Gather_{type}";
            go.transform.position = pos;
            go.transform.localScale = new Vector3(0.7f, 0.5f, 0.7f);
            if (parent != null) go.transform.SetParent(parent, true);
            var gs = go.AddComponent<GatherableSpawner>();
            gs.itemType = type;
            gs.respawnSeconds = type switch
            {
                ItemType.Fuel => GameDefines.GatherRespawnLong,
                ItemType.RepairParts => GameDefines.GatherRespawnLong,
                _ => GameDefines.GatherRespawnShort
            };
            gs.holdSeconds = GameDefines.GatherInteractHoldSeconds;
            gs.visualRoot = go;
            gs.mainRenderer = go.GetComponent<Renderer>();
            gs.Awake();
            return gs;
        }
    }
}
