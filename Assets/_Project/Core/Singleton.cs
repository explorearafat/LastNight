// Last Light — Core/Singleton.cs
// Lightweight MonoBehaviour singleton base. No DontDestroyOnLoad — each scene owns its managers
// and a bootstrapper decides persistence. Keeps the pattern safe for testing.

using UnityEngine;

namespace LastLight.Core
{
    /// <summary>Generic singleton for MonoBehaviours that must be unique in the scene.</summary>
    public abstract class Singleton<T> : MonoBehaviour where T : Singleton<T>
    {
        public static T Instance { get; private set; }

        protected virtual void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning($"[Singleton] Duplicate {typeof(T).Name} destroyed on {name}.");
                Destroy(gameObject);
                return;
            }
            Instance = (T)this;
        }

        protected virtual void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}
