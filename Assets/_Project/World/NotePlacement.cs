// Last Light — World/NotePlacement.cs
// Places 8 NotePickup slots across the island at build time (data-driven placeholders).
// Each slot has a distinct WorldNoteData asset when authored; until then they use a shared placeholder.

using UnityEngine;
using System.Collections.Generic;

namespace LastLight.World
{
    public sealed class NotePlacement : MonoBehaviour
    {
        [Header("Notes (assign WorldNoteData assets 1-8)")]
        [SerializeField] List<WorldNoteData> notes = new();

        [Header("Placement (local island XZ + Y)")]
        [SerializeField] List<Vector3> positions = DefaultPositions();

        void Awake() => PlaceIfNeeded();

        void PlaceIfNeeded()
        {
            if (GameObject.Find("Note_01") != null) return;

            var root = new GameObject("Notes");
            root.transform.SetParent(transform, false);
            int count = Mathf.Min(8, positions.Count);
            for (int i = 0; i < count; i++)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.name = $"Note_{(i + 1):00}";
                go.transform.SetParent(root.transform, false);
                go.transform.position = positions[i];
                if (go.transform.position.y < 0.3f) go.transform.position += Vector3.up * 0.35f;
                go.transform.localScale = new Vector3(0.35f, 0.45f, 0.05f);
                go.tag = "Untagged";

                var pickup = go.AddComponent<NotePickup>();
                var note = i < notes.Count ? notes[i] : null;
                if (note == null)
                {
                    // Create an in-memory placeholder ScriptableObject so the note is readable.
                    note = ScriptableObject.CreateInstance<WorldNoteData>();
                    note.noteNumber = i + 1;
                    note.noteTitle  = $"Keeper's Log — Day {40 + i * 3}";
                    note.bodyText   = $"[Placeholder log {i + 1}/8 — replace with the keeper's story in the WorldNoteData asset.]";
                    note.locationHint = $"{go.name}";
                }
                var fi = typeof(NotePickup).GetField("note", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                fi?.SetValue(pickup, note);

                var vf = typeof(NotePickup).GetField("visualToHide", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                vf?.SetValue(pickup, go);

                var col = go.GetComponent<BoxCollider>();
                col.isTrigger = true;
            }
        }

        static List<Vector3> DefaultPositions() => new()
        {
            new Vector3(-520f+0.8f, 0.9f,  0.4f), // 1 keeper's room (islet)
            new Vector3(-520f+0.6f, 4.2f,  0.2f), // 2 stairs (islet)
            new Vector3(-520f+0f,   8.2f,  0f  ), // 3 lens room (islet)
            new Vector3(  40f,  0.5f, 140f),     // 4 castle
            new Vector3(-140f, 0.8f,  90f),     // 5 bunker
            new Vector3(-180f, 0.8f,-140f),     // 6 cannery/pier
            new Vector3( 110f, 18f, 280f),      // 7 mountain / cave
            new Vector3( -20f, 0.6f, -40f),     // 8 ruins / mid-island
        };
    }
}
