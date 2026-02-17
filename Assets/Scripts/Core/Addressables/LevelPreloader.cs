using UnityEngine;
using System.Collections.Generic;

namespace SickOfIt.Core.Addressables
{
    /// <summary>
    /// Simple per-scene component that defines what to preload for this level
    /// Add this to each Level scene and configure what you want to preload
    /// </summary>
    public class LevelPreloader : MonoBehaviour
    {
        [Header("What to Preload")]
        [Tooltip("Labels to download in background (e.g., Video1, Level2)")]
        [SerializeField] private List<string> labelsToPreload = new List<string>();

        [Header("Settings")]
        [SerializeField] private bool preloadOnStart = true;
        [SerializeField] private float delayBeforePreload = 0.5f; // Small delay to let scene settle

        private void Start()
        {
            if (preloadOnStart && labelsToPreload.Count > 0)
            {
                Invoke(nameof(StartPreloading), delayBeforePreload);
            }
        }

        private void StartPreloading()
        {
            // Preloading labels

            foreach (string label in labelsToPreload)
            {
                PreloadManager.Instance.PreloadLabel(label, (success) => {
                    if (success)
                    {
                        // Successfully preloaded
                    }
                });
            }
        }

        /// <summary>
        /// Manually trigger preloading
        /// </summary>
        public void TriggerPreload()
        {
            StartPreloading();
        }
    }
}
