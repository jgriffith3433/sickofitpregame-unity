using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace SickOfIt.Core.Flow
{
    /// <summary>
    /// Waits for multiple IFrameSequencePlayers (SpriteFramePlayers) to finish loading
    /// and then activates the scene content via SceneContentController
    /// </summary>
    public class SpriteFrameLoadWaiter : MonoBehaviour
    {
        [Header("Loading Configuration")]
        [Tooltip("Frame sequence players (e.g., SpriteFramePlayers) to wait for before activating content")]
        [SerializeField] private List<MonoBehaviour> frameSequencePlayers = new List<MonoBehaviour>();

        [Tooltip("Scene content controller to activate once loading is complete")]
        [SerializeField] private SceneContentController sceneContentController;

        [Header("Optional Loading UI")]
        [Tooltip("GameObjects to show while waiting for loading (will be hidden when complete)")]
        [SerializeField] private List<GameObject> loadingIndicators = new List<GameObject>();

        [Header("Settings")]
        [Tooltip("Start waiting automatically on Awake")]
        [SerializeField] private bool waitOnAwake = true;

        [Tooltip("Delay in seconds after loading before activating content")]
        [SerializeField] private float activationDelay = 0f;

        [Header("Debug Info")]
        [SerializeField] private bool isWaiting = false;
        [SerializeField] private bool isComplete = false;
        [SerializeField] private int loadedCount = 0;
        [SerializeField] private int totalCount = 0;

        private List<IFrameSequencePlayer> players = new List<IFrameSequencePlayer>();

        private void Awake()
        {
            // Auto-find SceneContentController if not assigned
            if (sceneContentController == null)
            {
                sceneContentController = GetComponent<SceneContentController>();
                if (sceneContentController == null)
                {
                    sceneContentController = FindObjectOfType<SceneContentController>();
                }

                if (sceneContentController == null)
                {
                    Debug.LogWarning("[SpriteFrameLoadWaiter] No SceneContentController found. Content won't be activated automatically.");
                }
            }

            // Convert MonoBehaviours to interface references
            ConvertToInterfaceReferences();

            // Show loading indicators
            ShowLoadingIndicators();
        }

        private void Start()
        {
            if (waitOnAwake)
            {
                StartWaiting();
            }
        }

        /// <summary>
        /// Convert the serialized MonoBehaviours to IFrameSequencePlayer interfaces
        /// </summary>
        private void ConvertToInterfaceReferences()
        {
            players.Clear();
            foreach (var mb in frameSequencePlayers)
            {
                if (mb != null && mb is IFrameSequencePlayer player)
                {
                    players.Add(player);
                }
                else if (mb != null)
                {
                    Debug.LogWarning($"[SpriteFrameLoadWaiter] {mb.name} does not implement IFrameSequencePlayer");
                }
            }
        }

        /// <summary>
        /// Begin waiting for all frame sequence players to load
        /// </summary>
        public void StartWaiting()
        {
            if (isWaiting || isComplete)
            {
                return;
            }

            // Refresh interface references in case list was modified
            ConvertToInterfaceReferences();

            if (players.Count == 0)
            {
                Debug.LogWarning("[SpriteFrameLoadWaiter] No frame sequence players assigned. Activating content immediately.");
                ActivateContent();
                return;
            }

            totalCount = players.Count;
            isWaiting = true;
            StartCoroutine(WaitForLoadingCoroutine());
        }

        private IEnumerator WaitForLoadingCoroutine()
        {
            // Wait until all sprite frame players are loaded
            while (!AreAllPlayersLoaded())
            {
                // Update loaded count for debug display
                loadedCount = GetLoadedCount();
                yield return null; // Wait one frame
            }

            loadedCount = totalCount;
            isWaiting = false;
            isComplete = true;

            // Optional delay before activation
            if (activationDelay > 0f)
            {
                yield return new WaitForSeconds(activationDelay);
            }

            // All players loaded, activate content
            ActivateContent();
        }

        /// <summary>
        /// Check if all frame sequence players have finished loading
        /// </summary>
        private bool AreAllPlayersLoaded()
        {
            foreach (var player in players)
            {
                if (player != null && !player.IsLoaded)
                {
                    return false; // Still waiting for this one
                }
            }

            return true; // All loaded
        }

        /// <summary>
        /// Get count of players that are currently loaded
        /// </summary>
        private int GetLoadedCount()
        {
            int count = 0;
            foreach (var player in players)
            {
                if (player != null && player.IsLoaded)
                {
                    count++;
                }
            }
            return count;
        }

        /// <summary>
        /// Activate the scene content
        /// </summary>
        private void ActivateContent()
        {
            // Hide loading indicators
            HideLoadingIndicators();

            // Activate scene content
            if (sceneContentController != null)
            {
                sceneContentController.ActivateScene();
            }
        }

        private void ShowLoadingIndicators()
        {
            foreach (var indicator in loadingIndicators)
            {
                if (indicator != null)
                {
                    indicator.SetActive(true);
                }
            }
        }

        private void HideLoadingIndicators()
        {
            foreach (var indicator in loadingIndicators)
            {
                if (indicator != null)
                {
                    indicator.SetActive(false);
                }
            }
        }

        /// <summary>
        /// Force immediate activation without waiting
        /// </summary>
        public void ForceActivate()
        {
            if (isComplete)
            {
                return;
            }

            StopAllCoroutines();
            isWaiting = false;
            isComplete = true;
            ActivateContent();
        }

        /// <summary>
        /// Get loading progress (0 to 1)
        /// </summary>
        public float GetLoadingProgress()
        {
            if (totalCount == 0) return 1f;
            return (float)loadedCount / totalCount;
        }

        /// <summary>
        /// Add a frame sequence player to the wait list (must be called before StartWaiting)
        /// </summary>
        public void AddFrameSequencePlayer(MonoBehaviour playerBehaviour)
        {
            if (playerBehaviour != null && !frameSequencePlayers.Contains(playerBehaviour))
            {
                if (playerBehaviour is IFrameSequencePlayer)
                {
                    frameSequencePlayers.Add(playerBehaviour);
                }
                else
                {
                    Debug.LogWarning($"[SpriteFrameLoadWaiter] {playerBehaviour.name} does not implement IFrameSequencePlayer");
                }
            }
        }

        /// <summary>
        /// Remove a frame sequence player from the wait list
        /// </summary>
        public void RemoveFrameSequencePlayer(MonoBehaviour playerBehaviour)
        {
            frameSequencePlayers.Remove(playerBehaviour);
        }

        public bool IsWaiting => isWaiting;
        public bool IsComplete => isComplete;
        public int LoadedCount => loadedCount;
        public int TotalCount => totalCount;
    }
}
