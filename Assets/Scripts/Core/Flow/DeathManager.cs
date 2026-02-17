using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using MobileControllerFramework;
using SickOfIt.Core.Addressables;

namespace SickOfIt.Core.Flow
{
    /// <summary>
    /// Manager for death scenes. Handles loading of death animation and automatically
    /// reloads the level after animation completes.
    /// Works similarly to VideoManager but for death sequences.
    /// </summary>
    public class DeathManager : MonoBehaviour
    {
        [Header("Loading System")]
        [Tooltip("Death animation sprite player that needs to finish loading")]
        [SerializeField] private MonoBehaviour deathPlayerBehaviour;

        [Tooltip("GameObjects to activate once death assets are loaded (they should start deactivated)")]
        [SerializeField] private List<GameObject> objectsToActivateAfterLoading = new List<GameObject>();

        [Tooltip("GameObjects to deactivate once death assets are loaded (e.g., loading indicators)")]
        [SerializeField] private List<GameObject> objectsToDeactivateAfterLoading = new List<GameObject>();

        [Header("Skip Settings")]
        [Tooltip("Optional skip button using MobileControllerFramework")]
        [SerializeField] private MobileControllerFramework.Button skipButton;

        [Header("Playback Settings")]
        [Tooltip("How long to wait after death animation finishes before reloading level")]
        [SerializeField] private float delayBeforeReload = 1f;

        [Header("Loading Debug")]
        [SerializeField] private bool isLoading = false;
        [SerializeField] private bool animationLoaded = false;
        [SerializeField] private bool animationComplete = false;

        private IFrameSequencePlayer deathPlayer;

        private void Awake()
        {
            deathPlayer = deathPlayerBehaviour as IFrameSequencePlayer;

            // Subscribe to skip button
            if (skipButton != null)
            {
                skipButton.TapEndEvent.AddListener(SkipDeathAnimation);
            }

            // Subscribe to death animation completion callback
            if (deathPlayer != null)
            {
                deathPlayer.PlaybackCompleted += OnDeathAnimationComplete;
                isLoading = true;
                StartCoroutine(WaitForDeathAnimationToLoad());
            }
            else
            {
                // No death player, activate everything immediately and just reload
                ActivateGameObjects();
                StartCoroutine(ReloadLevelAfterDelay());
            }
        }

        private void OnDestroy()
        {
            // Unsubscribe from callback
            if (deathPlayer != null)
            {
                deathPlayer.PlaybackCompleted -= OnDeathAnimationComplete;
            }

            // Unsubscribe from skip button
            if (skipButton != null)
            {
                skipButton.TapEndEvent.RemoveListener(SkipDeathAnimation);
            }
        }

        /// <summary>
        /// Wait for death animation assets to finish loading
        /// </summary>
        private IEnumerator WaitForDeathAnimationToLoad()
        {
            // Wait until animation is loaded
            while (deathPlayer != null && !deathPlayer.IsLoaded)
            {
                yield return null; // Wait one frame
            }

            animationLoaded = true;
            isLoading = false;

            // Deactivate loading indicators
            DeactivateGameObjects();

            // Activate game objects now that loading is complete
            ActivateGameObjects();

            // Play the death animation automatically
            if (deathPlayer != null)
            {
                deathPlayer.Play();

                // Preload level while animation plays (callback will handle transition)
                StartCoroutine(PreloadLevelDuringAnimation());
            }
            else
            {
                // No player, just reload
                StartCoroutine(ReloadLevelAfterDelay());
            }
        }

        /// <summary>
        /// Preload the level while animation plays
        /// </summary>
        private IEnumerator PreloadLevelDuringAnimation()
        {
            // Preload the level while animation plays
            string sceneName = PlayerPrefs.GetString("DeathSceneToReload", "");
            if (!string.IsNullOrEmpty(sceneName))
            {
                AddressablesSceneLoader.Instance.PreloadScene(sceneName, null);
            }

            yield break;
        }

        /// <summary>
        /// Skip the currently playing death animation
        /// </summary>
        public void SkipDeathAnimation()
        {
            if (deathPlayer != null)
            {
                Debug.Log("DeathManager: Death animation skipped by player");
                deathPlayer.SkipToEnd();
            }
        }

        /// <summary>
        /// Called when death animation completes (via callback)
        /// </summary>
        private void OnDeathAnimationComplete()
        {
            animationComplete = true;

            // Wait a moment before reloading
            StartCoroutine(ReloadLevelAfterDelay());
        }

        /// <summary>
        /// Reload after a delay (fallback if no animation)
        /// </summary>
        private IEnumerator ReloadLevelAfterDelay()
        {
            yield return new WaitForSeconds(delayBeforeReload);
            ReloadLevel();
        }

        /// <summary>
        /// Reload the level that the player died in
        /// </summary>
        private void ReloadLevel()
        {
            // Check if there's a level stored to reload
            if (PlayerPrefs.HasKey("DeathSceneToReload"))
            {
                string sceneName = PlayerPrefs.GetString("DeathSceneToReload");
                PlayerPrefs.DeleteKey("DeathSceneToReload");
                PlayerPrefs.Save();

                // Load the scene (which will unload Death1 and activate the preloaded level)
                if (sceneName.StartsWith("Level"))
                {
                    // Extract level number and load via SceneTransitionManager
                    string numberStr = sceneName.Replace("Level", "");
                    if (int.TryParse(numberStr, out int levelNum))
                    {
                        SceneTransitionManager.Instance.LoadLevel(levelNum);
                    }
                    else
                    {
                        Debug.LogError($"[DeathManager] Could not parse level number from '{sceneName}'");
                        SceneTransitionManager.Instance.LoadMainMenu();
                    }
                }
                else
                {
                    // Generic scene load
                    SceneTransitionManager.Instance.LoadDeathScene(sceneName); // Reusing LoadDeathScene for any scene name
                }
            }
            else
            {
                Debug.LogWarning("[DeathManager] No scene to reload, returning to main menu");
                SceneTransitionManager.Instance.LoadMainMenu();
            }
        }

        /// <summary>
        /// Activate all game objects that were waiting for loading to complete
        /// </summary>
        private void ActivateGameObjects()
        {
            if (objectsToActivateAfterLoading != null)
            {
                foreach (var obj in objectsToActivateAfterLoading)
                {
                    if (obj != null)
                    {
                        obj.SetActive(true);
                    }
                }
            }
        }

        /// <summary>
        /// Deactivate loading indicators after loading is complete
        /// </summary>
        private void DeactivateGameObjects()
        {
            if (objectsToDeactivateAfterLoading != null)
            {
                foreach (var obj in objectsToDeactivateAfterLoading)
                {
                    if (obj != null)
                    {
                        obj.SetActive(false);
                    }
                }
            }
        }
    }
}
