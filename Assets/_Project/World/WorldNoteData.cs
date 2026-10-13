// Last Light — World/WorldNoteData.cs
// Data-driven lore notes — ScriptableObjects that live in the project, not in scenes.
// Place 8 NotePickup objects in the world; each holds a reference to one of these.

using UnityEngine;

namespace LastLight.World
{
    [CreateAssetMenu(menuName = "LastLight/World Note", fileName = "Note_")]
    public sealed class WorldNoteData : ScriptableObject
    {
        [Header("Identity")]
        public int    noteNumber = 1; // 1-8
        public string noteTitle  = "Log Entry";

        [Header("Content (placeholder — fill with the keeper's story later)")]
        [TextArea(4, 10)] public string bodyText = "[Placeholder — keeper's writing goes here.]";

        [Header("Presentation")]
        [Tooltip("Optional paper texture / portrait to show with the note.")]
        public Sprite paperSprite;

        [Tooltip("Where in the world this note was originally placed (hint for the player).")]
        public string locationHint = "";
    }
}
