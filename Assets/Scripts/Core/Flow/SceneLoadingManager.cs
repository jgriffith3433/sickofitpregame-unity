using UnityEngine;
using System.Collections.Generic;

namespace SickOfIt.Core.Flow
{
    /// <summary>
    /// Manages UI state during scene transitions. Place this in menu scenes (MainMenu, LevelSelection, VideoSelection)
    /// to show loading indicators and hide UI elements while scenes are loading.
    /// </summary>
    public class SceneLoadingManager : MonoBehaviour
    {
        private static SceneLoadingManager _currentInstance;

        [Header("Loading UI")]
        [Tooltip("GameObjects to activate when scene loading starts (e.g., loading screen, spinner)")]
        [SerializeField] private List<GameObject> objectsToActivateOnLoad = new List<GameObject>();

        [Tooltip("GameObjects to deactivate when scene loading starts (e.g., menu buttons, UI panels)")]
        [SerializeField] private List<GameObject> objectsToDeactivateOnLoad = new List<GameObject>();

        [Header("Debug")]
        [SerializeField] private bool isLoading = false;

        private void OnEnable()
        {
            // Register this as the current scene's loading manager
            Debug.Log($"[SceneLoadingManager] OnEnable() called. Setting as _currentInstance.");
            _currentInstance = this;
            Debug.Log($"[SceneLoadingManager] _currentInstance set. GameObject: {gameObject.name}");
        }

        private void OnDisable()
        {
            // Unregister when scene is unloading
            Debug.Log($"[SceneLoadingManager] OnDisable() called");
            if (_currentInstance == this)
            {
                Debug.Log($"[SceneLoadingManager] Clearing _currentInstance");
                _currentInstance = null;
            }
        }

        /// <summary>
        /// Call this before initiating any scene load to show loading UI
        /// </summary>
        public static void ShowLoadingUI()
        {
            Debug.Log($"[SceneLoadingManager] ShowLoadingUI() called. _currentInstance: {_currentInstance != null}");

            if (_currentInstance != null)
            {
                Debug.Log($"[SceneLoadingManager] Calling ActivateLoadingUI on current instance");
                _currentInstance.ActivateLoadingUI();
            }
            else
            {
                Debug.LogWarning($"[SceneLoadingManager] No current instance found! Loading UI not shown.");
            }
        }

        /// <summary>
        /// Internal method to activate loading UI elements
        /// </summary>
        private void ActivateLoadingUI()
        {
            Debug.Log($"[SceneLoadingManager] ActivateLoadingUI() called. isLoading: {isLoading}");

            if (isLoading)
            {
                Debug.Log("[MOBILE DEBUG] SceneLoadingManager: Already loading, skipping UI activation");
                return; // Already loading
            }

            isLoading = true;
            Debug.Log("[MOBILE DEBUG] SceneLoadingManager: Showing loading UI");
            Debug.Log($"[SceneLoadingManager] Set isLoading = true");

            // Activate loading indicators
            if (objectsToActivateOnLoad != null)
            {
                Debug.Log($"[MOBILE DEBUG] Activating {objectsToActivateOnLoad.Count} loading UI objects");
                Debug.Log($"[SceneLoadingManager] objectsToActivateOnLoad count: {objectsToActivateOnLoad.Count}");

                foreach (var obj in objectsToActivateOnLoad)
                {
                    if (obj != null)
                    {
                        Debug.Log($"[SceneLoadingManager] Activating object: {obj.name}, currently active: {obj.activeSelf}");
                        obj.SetActive(true);
                        Debug.Log($"[MOBILE DEBUG] Activated: {obj.name}");
                        Debug.Log($"[SceneLoadingManager] Object {obj.name} now active: {obj.activeSelf}");
                    }
                    else
                    {
                        Debug.LogWarning($"[SceneLoadingManager] NULL object in objectsToActivateOnLoad list!");
                    }
                }
            }
            else
            {
                Debug.LogWarning("[MOBILE DEBUG] No loading objects to activate!");
                Debug.LogWarning($"[SceneLoadingManager] objectsToActivateOnLoad is NULL!");
            }

            // Deactivate menu UI
            if (objectsToDeactivateOnLoad != null)
            {
                Debug.Log($"[MOBILE DEBUG] Deactivating {objectsToDeactivateOnLoad.Count} menu UI objects");
                Debug.Log($"[SceneLoadingManager] objectsToDeactivateOnLoad count: {objectsToDeactivateOnLoad.Count}");

                foreach (var obj in objectsToDeactivateOnLoad)
                {
                    if (obj != null)
                    {
                        Debug.Log($"[SceneLoadingManager] Deactivating object: {obj.name}, currently active: {obj.activeSelf}");
                        obj.SetActive(false);
                        Debug.Log($"[SceneLoadingManager] Object {obj.name} now active: {obj.activeSelf}");
                    }
                    else
                    {
                        Debug.LogWarning($"[SceneLoadingManager] NULL object in objectsToDeactivateOnLoad list!");
                    }
                }
            }
            else
            {
                Debug.Log($"[SceneLoadingManager] objectsToDeactivateOnLoad is NULL (no objects to deactivate)");
            }

            Debug.Log($"[SceneLoadingManager] ActivateLoadingUI() completed");
        }

        /// <summary>
        /// Helper method to show loading UI and then load a level
        /// </summary>
        public void LoadLevel(int levelNumber)
        {
            Debug.Log($"[SceneLoadingManager] LoadLevel({levelNumber}) called");
            ShowLoadingUI();
            Debug.Log($"[SceneLoadingManager] Calling SceneTransitionManager.LoadLevel({levelNumber})");
            SceneTransitionManager.Instance.LoadLevel(levelNumber);
            Debug.Log($"[SceneLoadingManager] LoadLevel({levelNumber}) completed (scene load initiated)");
        }

        /// <summary>
        /// Helper method to show loading UI and then load a video
        /// </summary>
        public void LoadVideo(int videoNumber)
        {
            Debug.Log($"[MOBILE DEBUG] SceneLoadingManager.LoadVideo({videoNumber}) called");
            Debug.Log($"[SceneLoadingManager] LoadVideo({videoNumber}) called");
            ShowLoadingUI();
            Debug.Log($"[MOBILE DEBUG] Calling SceneTransitionManager.LoadVideo({videoNumber})");
            Debug.Log($"[SceneLoadingManager] Calling SceneTransitionManager.LoadVideo({videoNumber})");
            SceneTransitionManager.Instance.LoadVideo(videoNumber);
            Debug.Log($"[SceneLoadingManager] LoadVideo({videoNumber}) completed (scene load initiated)");
        }

        /// <summary>
        /// Helper method to show loading UI and then continue game
        /// </summary>
        public void ContinueGame()
        {
            Debug.Log($"[SceneLoadingManager] ContinueGame() called");
            ShowLoadingUI();
            Debug.Log($"[SceneLoadingManager] Calling SceneTransitionManager.ContinueGame()");
            SceneTransitionManager.Instance.ContinueGame();
            Debug.Log($"[SceneLoadingManager] ContinueGame() completed (scene load initiated)");
        }

        /// <summary>
        /// Helper method to show loading UI and then start new game
        /// </summary>
        public void NewGame()
        {
            ShowLoadingUI();
            // Assuming new game starts at level 1 or video 1
            SceneTransitionManager.Instance.LoadLevel(1);
        }
    }
}
