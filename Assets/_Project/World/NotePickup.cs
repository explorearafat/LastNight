// Last Light — World/NotePickup.cs
// World note object — interacts once, shows the text, then depletes.
// The note's content lives in a WorldNoteData asset; the collected set is tracked
// in SaveManager so it persists. No final text yet — placeholders are fine.

using UnityEngine;
using LastLight.Core;

namespace LastLight.World
{
    public sealed class NotePickup : MonoBehaviour, IInteractable
    {
        [SerializeField] WorldNoteData note;
        [SerializeField] GameObject    visualToHide;

        bool _collected;

        public string Prompt => _collected ? "Already read" : $"Read: {(note != null ? note.noteTitle : "Note")}";
        public Vector3 InteractionPoint => transform.position;
        public bool CanInteract => !_collected;

        void Awake()
        {
            // If this note was already collected in a previous session, hide it.
            if (note != null && Survival.SaveManager.HasCollectedNote(note.noteNumber))
            {
                _collected = true;
                ApplyVisual();
            }
        }

        public void Interact(GameObject instigator)
        {
            if (_collected || note == null) return;
            _collected = true;

            // Record in save
            Survival.SaveManager.CollectNote(note.noteNumber);
            EventBus.Publish(new NoteCollectedEvent(note.noteNumber, note.noteTitle));

            Debug.Log($"[Note] {note.noteNumber}: {note.noteTitle}\n{note.bodyText}");

            // Show diegetically — wire this to a simple UI panel or just log for now.
            var presenter = FindFirstObjectByType<UI.NotePresenter>();
            if (presenter != null) presenter.Show(note);
            else Debug.Log($"[Note UI] No NotePresenter — text logged above. Assign one to show notes on screen.");

            ApplyVisual();
        }

        void ApplyVisual()
        {
            if (visualToHide != null) visualToHide.SetActive(!_collected);
            else if (_collected) gameObject.SetActive(false);
        }

        public void RestoreCollected(bool v) { _collected = v; ApplyVisual(); }

        public WorldNoteData Note => note;
    }

    public readonly struct NoteCollectedEvent
    {
        public readonly int    Number;
        public readonly string Title;
        public NoteCollectedEvent(int number, string title) { Number = number; Title = title; }
    }
}
