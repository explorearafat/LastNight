// Last Light — Systems/BoatDockManager.cs
// Spawns docks on both sides of the channel and wires the boat. No scene setup required.

using UnityEngine;

namespace LastLight.Systems
{
    public sealed class BoatDockManager : MonoBehaviour
    {
        [Header("Prefabs (optional — uses dock FBX if assigned, else blockout)")]
        [SerializeField] GameObject dockPrefab;  // LL_Dock_5m
        [SerializeField] GameObject boatPrefab;  // LL_Boat

        [Header("Dock config")]
        [SerializeField] Vector3 mainDockPos = new Vector3(-210f, 0.10f, -6f);
        [SerializeField] Vector3 isletDockPos = new Vector3(-445f, 0.10f, -6f);
        [SerializeField] Vector3 boatStartPos = new Vector3(-215f, -0.9f, -6f);

        GameObject _mainDock, _isletDock;
        BoatController _boat;

        void Awake() => BuildIfNeeded();

        void BuildIfNeeded()
        {
            if (GameObject.Find("Boat") != null) return;

            var cfg = FindFirstObjectByType<World.WorldConfig>();
            if (cfg != null)
            {
                mainDockPos = new Vector3(cfg.mainIslandDock.x, 0.10f, cfg.mainIslandDock.y);
                isletDockPos = new Vector3(cfg.isletDock.x, 0.10f, cfg.isletDock.y);
            }

            _mainDock = SpawnDock(dockPrefab, mainDockPos, Quaternion.Euler(0, 0, 0), "Dock_MainIsland");
            _isletDock= SpawnDock(dockPrefab, isletDockPos, Quaternion.Euler(0, 0, 0), "Dock_Islet");

            // Boat
            if (boatPrefab != null)
            {
                var go = Instantiate(boatPrefab, boatStartPos, Quaternion.identity);
                go.name = "Boat";
                _boat = go.GetComponent<BoatController>() ?? go.AddComponent<BoatController>();
            }
            else
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.name = "Boat";
                go.transform.position = boatStartPos;
                go.transform.localScale = new Vector3(4.2f, 0.6f, 1.5f);
                _boat = go.AddComponent<BoatController>();
                // add trigger for boarding
                var col = go.GetComponent<BoxCollider>(); col.isTrigger = true; col.size = new Vector3(1.2f, 1.2f, 1.2f);
                var boardPoint = new GameObject("BoardPoint");
                boardPoint.transform.SetParent(go.transform, false);
                boardPoint.transform.localPosition = new Vector3(0, 0.5f, 0);
                var seat = new GameObject("PlayerSeat");
                seat.transform.SetParent(go.transform, false);
                seat.transform.localPosition = new Vector3(0.35f, 0.55f, 0);
                var fiBoard = typeof(BoatController).GetField("boardPoint", System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);
                var fiSeat  = typeof(BoatController).GetField("playerSeat", System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);
                fiBoard?.SetValue(_boat, boardPoint.transform);
                fiSeat?.SetValue(_boat, seat.transform);
            }
            // wire docks into boat
            if (_boat != null)
            {
                var fiMain  = typeof(BoatController).GetField("mainDock", System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);
                var fiIslet = typeof(BoatController).GetField("isletDock", System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);
                fiMain?.SetValue(_boat, _mainDock != null ? _mainDock.transform : null);
                fiIslet?.SetValue(_boat, _isletDock != null ? _isletDock.transform : null);
            }
            Debug.Log("[BoatDockManager] Docks + boat spawned. Board with E/Space near the boat; steer with joystick/WASD.");
        }

        GameObject SpawnDock(GameObject prefab, Vector3 pos, Quaternion rot, string name)
        {
            GameObject go;
            if (prefab != null)
            {
                go = Instantiate(prefab, pos, rot);
                go.name = name;
            }
            else
            {
                go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.name = name;
                go.transform.position = pos;
                go.transform.localScale = new Vector3(5f, 0.18f, 1.8f);
                go.transform.rotation = rot;
            }
            go.isStatic = true;
            var col = go.GetComponent<Collider>();
            if (col != null) col.isTrigger = false; // dock is walkable
            return go;
        }
    }
}
