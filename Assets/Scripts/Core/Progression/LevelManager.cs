using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using SickOfIt.Core.Addressables;
using SickOfIt.Core.Flow;
using SickOfIt.Combat.Enemies.Spawning;
using SickOfIt.VideoFrameExtractor.Runtime;
using SickOfIt.Data;

namespace SickOfIt.Core.Progression
{
    /// <summary>
    /// Base manager for individual level scenes. Add this to each level scene and call CompleteLevel() when the player wins.
    /// </summary>
    public class LevelManager : MonoBehaviour
    {
        [Header("Level Settings")]
        [SerializeField] private int levelNumber = 1;

        [Header("Background Preloading")]
        [Tooltip("Enable preloading next video during gameplay (may cause OOM on low-memory platforms)")]
        [SerializeField] private bool enableVideoPreloading = true;
        [Tooltip("Delay before starting video preload (seconds). Increase on low-memory platforms.")]
        [SerializeField] private float videoPreloadDelay = 2f;
        [Tooltip("Disable preloading on WebGL to prevent memory issues")]
        [SerializeField] private bool disablePreloadOnWebGL = true;

        [Header("Death Scene Preloading")]
        [Tooltip("Name of the death scene to preload (e.g., 'Death1')")]
        [SerializeField] private string deathSceneName = "Death1";
        [Tooltip("Whether to preload the death scene during gameplay")]
        [SerializeField] private bool preloadDeathScene = true;

        [Header("Loading System")]
        [Tooltip("Background sprite players that need to finish loading")]
        [SerializeField] private List<MonoBehaviour> backgroundPlayerBehaviours = new List<MonoBehaviour>();

        [Tooltip("GameObjects to activate once all assets are loaded (they should start deactivated)")]
        [SerializeField] private List<GameObject> objectsToActivateAfterLoading = new List<GameObject>();

        [Tooltip("GameObjects to deactivate once all assets are loaded (e.g., loading indicators)")]
        [SerializeField] private List<GameObject> objectsToDeactivateAfterLoading = new List<GameObject>();

        [Header("Scene Transition Loading")]
        [Tooltip("GameObjects to activate when transitioning to next scene (e.g., 'Loading...' UI)")]
        [SerializeField] private List<GameObject> objectsToActivateOnSceneTransition = new List<GameObject>();

        [Tooltip("GameObjects to deactivate when transitioning to next scene (e.g., player controls, game UI)")]
        [SerializeField] private List<GameObject> objectsToDeactivateOnSceneTransition = new List<GameObject>();

        [Header("Spawner Control")]
        [Tooltip("SpriteFramePlayer that when finished, will kill all spawners")]
        [SerializeField] private MonoBehaviour spawnerKillTriggerPlayer;
        
        [Tooltip("Automatically find and kill all spawners when trigger player finishes")]
        [SerializeField] private bool autoKillSpawnersOnComplete = false;

        [Header("Loading Debug")]
        [SerializeField] private bool isLoading = false;
        [SerializeField] private bool allAssetsLoaded = false;

        private readonly List<IFrameSequencePlayer> backgroundPlayers = new List<IFrameSequencePlayer>();
        private IFrameSequencePlayer triggerPlayer;
        private List<EnemySpawner> levelSpawners = new List<EnemySpawner>();

        private void Awake()
        {
            backgroundPlayers.Clear();
            foreach (var behaviour in backgroundPlayerBehaviours)
            {
                if (behaviour is IFrameSequencePlayer player)
                {
                    backgroundPlayers.Add(player);
                }
            }

            // Setup spawner kill trigger
            if (spawnerKillTriggerPlayer != null && spawnerKillTriggerPlayer is IFrameSequencePlayer framePlayer)
            {
                triggerPlayer = framePlayer;
                triggerPlayer.PlaybackCompleted += OnSpawnerKillTriggerComplete;
            }

            // Find all spawners in the level
            if (autoKillSpawnersOnComplete)
            {
                levelSpawners.AddRange(FindObjectsOfType<EnemySpawner>());
            }

            // Start in loading state if we have assets to wait for
            if (HasAssetsToLoad())
            {
                isLoading = true;
                StartCoroutine(WaitForAssetsToLoad());
            }
            else
            {
                // No assets to load, activate everything immediately
                ActivateGameObjects();
            }

            // Preload next video in background while player plays
            StartCoroutine(PreloadNextVideoInBackground());
        }

        private void Start()
        {
            // Level loads directly now - videos are played from MainMenu/LevelSelection before entering
        }

        /// <summary>
        /// Preload the next video during gameplay
        /// </summary>
        private IEnumerator PreloadNextVideoInBackground()
        {
#if UNITY_WEBGL
            // Skip video preloading on WebGL to prevent Out of Memory crashes
            if (disablePreloadOnWebGL)
            {
                Debug.Log($"[LevelManager] Skipping video preload on WebGL to conserve memory");
                yield break;
            }
#endif

            if (!enableVideoPreloading)
            {
                Debug.Log($"[LevelManager] Video preloading disabled");
                yield break;
            }

            // Wait for level to fully initialize and give time for memory to stabilize
            yield return new WaitForSeconds(videoPreloadDelay);

            int nextVideo = levelNumber + 1;

            Debug.Log($"[LevelManager] Starting background preload of Video{nextVideo}");
            AddressablesSceneLoader.Instance.PreloadVideo(nextVideo, null);

            // Also preload death scene if enabled
            if (preloadDeathScene && !string.IsNullOrEmpty(deathSceneName))
            {
                yield return new WaitForSeconds(1f); // Stagger preloads slightly

                AddressablesSceneLoader.Instance.PreloadScene(deathSceneName, null);
            }
        }

        /// <summary>
        /// Call this method when the player completes the level's win condition
        /// </summary>
        public void CompleteLevel()
        {
            // Mark level as complete
            GameProgressManager.Instance.SetLevelCompleted(levelNumber);

            // Track level completion
            AnalyticsManager.Instance.TrackLevelComplete(levelNumber);

            // Unlock the NEXT level's video
            int nextLevelVideo = levelNumber + 1;
            GameProgressManager.Instance.UnlockVideo(nextLevelVideo);

            // You can add completion effects here (particles, sounds, etc.)

            // After a short delay, play next level's video OR go to next level
            Invoke(nameof(ProceedToNextLevel), 2f);
        }

        /// <summary>
        /// Clla this method when the player completes the level but wants to return to the main menu instead of proceeding
        /// </summary>
        public void CompleteLevelAndReturnToMainMenu()
        {
            // Mark level as complete
            GameProgressManager.Instance.SetLevelCompleted(levelNumber);

            // Track level completion
            AnalyticsManager.Instance.TrackLevelComplete(levelNumber);

            // Unlock the NEXT level's video
            int nextLevelVideo = levelNumber + 1;
            GameProgressManager.Instance.UnlockVideo(nextLevelVideo);

            // You can add completion effects here (particles, sounds, etc.)

            // After a short delay, return to main menu
            Invoke(nameof(ReturnToMainMenu), 2f);
        }

        private void ProceedToNextLevel()
        {
            int nextLevel = levelNumber + 1;
            int nextLevelVideo = levelNumber + 1;

            // Show loading UI before transitioning
            ShowSceneTransitionUI();

            // Play the next level's video (if it exists and hasn't been seen)
            if (GameProgressManager.Instance.IsVideoUnlocked(nextLevelVideo))
            {
                // Store which level to load after the video
                PlayerPrefs.SetInt("ReturnToLevel", nextLevel);
                PlayerPrefs.Save();

                SceneTransitionManager.Instance.LoadVideo(nextLevelVideo);
            }
            else
            {
                // No video, go directly to next level or level select
                SceneTransitionManager.Instance.LoadLevelSelection();
            }
        }

        public void ReturnToLevelSelect()
        {
            ShowSceneTransitionUI();
            SceneTransitionManager.Instance.LoadLevelSelection();
        }

        public void ReturnToMainMenu()
        {
            ShowSceneTransitionUI();
            SceneTransitionManager.Instance.LoadMainMenu();
        }

        public void RestartLevel()
        {
            // Go through death scene instead of direct reload

            // Store which scene to reload after death animation
            UnityEngine.SceneManagement.Scene currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            PlayerPrefs.SetString("DeathSceneToReload", currentScene.name);
            PlayerPrefs.Save();

            ShowSceneTransitionUI();

            // Load death scene (or directly load the level if no death scene configured)
            if (!string.IsNullOrEmpty(deathSceneName))
            {
                SceneTransitionManager.Instance.LoadDeathScene(deathSceneName);
            }
            else
            {
                Debug.LogWarning("[LevelManager] No death scene configured, loading level directly");
                SceneTransitionManager.Instance.LoadLevel(levelNumber);
            }
        }



        /// <summary>
        /// Check if there are any assets that need to be loaded
        /// </summary>
        private bool HasAssetsToLoad()
        {
            return backgroundPlayers != null && backgroundPlayers.Count > 0;
        }

        /// <summary>
        /// Wait for all addressable assets to finish loading
        /// </summary>
        private IEnumerator WaitForAssetsToLoad()
        {
            // Wait until all assets are loaded
            while (!AreAllAssetsLoaded())
            {
                yield return null; // Wait one frame
            }

            allAssetsLoaded = true;
            isLoading = false;

            // Deactivate loading indicators
            DeactivateGameObjects();

            // Activate game objects now that loading is complete
            ActivateGameObjects();
        }

        /// <summary>
        /// Check if all registered assets have finished loading
        /// </summary>
        private bool AreAllAssetsLoaded()
        {
            // Check all background players
            if (backgroundPlayers != null)
            {
                foreach (var player in backgroundPlayers)
                {
                    if (player != null && !player.IsLoaded)
                    {
                        return false; // Still waiting for this one
                    }
                }
            }

            return true; // Everything is loaded
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

            // Track level start event when player gains control
            AnalyticsManager.Instance.TrackLevelStart(levelNumber);
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

        /// <summary>
        /// Show loading UI when transitioning to another scene
        /// </summary>
        public void ShowSceneTransitionUI()
        {
            // Activate loading indicators
            if (objectsToActivateOnSceneTransition != null)
            {
                foreach (var obj in objectsToActivateOnSceneTransition)
                {
                    if (obj != null)
                    {
                        obj.SetActive(true);
                    }
                }
            }

            // Deactivate game UI/controls
            if (objectsToDeactivateOnSceneTransition != null)
            {
                foreach (var obj in objectsToDeactivateOnSceneTransition)
                {
                    if (obj != null)
                    {
                        obj.SetActive(false);
                    }
                }
            }
        }

        /// <summary>
        /// Called when the spawner kill trigger SpriteFramePlayer finishes playback
        /// </summary>
        private void OnSpawnerKillTriggerComplete()
        {
            if (!autoKillSpawnersOnComplete)
                return;

            Debug.Log($"[LevelManager] Spawner kill trigger complete - killing {levelSpawners.Count} spawners");

            // Kill all spawners
            foreach (var spawner in levelSpawners)
            {
                if (spawner != null)
                {
                    spawner.KillSpawner();
                }
            }
        }

        private void OnDestroy()
        {
            // Unsubscribe from trigger player event
            if (triggerPlayer != null)
            {
                triggerPlayer.PlaybackCompleted -= OnSpawnerKillTriggerComplete;
            }
        }
    }
}
