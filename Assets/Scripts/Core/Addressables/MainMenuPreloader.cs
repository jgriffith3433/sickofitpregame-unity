using UnityEngine;
using UnityEngine.UI;
using SickOfIt.Core.Flow;
using SickOfIt.Data;

namespace SickOfIt.Core.Addressables
{
    /// <summary>
    /// Main menu controller that handles preloading for Continue vs New Game
    /// Add this to your MainMenu scene
    /// </summary>
    public class MainMenuPreloader : MonoBehaviour
    {
        [Header("Preload Settings")]
        [SerializeField] private bool preloadOnStart = true;
        [SerializeField] private bool showPreloadProgress = true;

        [Header("UI References")]
        [SerializeField] private GameObject continueButton;

        [Header("Optional UI (for showing download progress)")]
        [SerializeField] private GameObject preloadingIndicator;
        [SerializeField] private TMPro.TextMeshProUGUI preloadStatusText;

        private void Start()
        {
            // Check if player has played before and show/hide Continue button
            UpdateContinueButtonVisibility();

            if (preloadOnStart)
            {
                PreloadAppropriateLevel();
            }
        }

        /// <summary>
        /// Show Continue button only if player has progress
        /// </summary>
        private void UpdateContinueButtonVisibility()
        {
            if (continueButton == null) return;

            bool hasProgress = PlayerHasProgress();
            continueButton.SetActive(hasProgress);

            if (hasProgress)
            {
                Debug.Log("[MainMenu] Player has progress - Continue button shown");
            }
            else
            {
                Debug.Log("[MainMenu] New player - Continue button hidden");
            }
        }

        /// <summary>
        /// Check if player has any saved progress
        /// </summary>
        private bool PlayerHasProgress()
        {
            // Check if game has been initialized before
            if (!PlayerPrefs.HasKey("GameInitialized"))
                return false;

            // Check if player has beaten the game (no more levels to play)
            if (GameProgressManager.Instance.HasBeatenGame())
            {
                Debug.Log("[MainMenu] Player has beaten all levels - Continue button hidden");
                return false;
            }

            // Check if player has reached beyond level 1
            int highestLevel = GameProgressManager.Instance.GetHighestLevelReached();
            if (highestLevel > 1)
                return true;

            // Check if they've completed level 1
            if (GameProgressManager.Instance.IsLevelCompleted(1))
                return true;

            return false;
        }

        /// <summary>
        /// Called when Continue button is pressed
        /// Preloads the player's current level
        /// </summary>
        public void OnContinueButtonPressed()
        {
            int currentLevel = GameProgressManager.Instance.GetCurrentLevel();

            // Check if player should see the video first
            if (ShouldPlayVideoBeforeLevel(currentLevel))
            {
                ShowPreloadingUI($"Loading Video {currentLevel}...");
                PreloadVideoThenLevel(currentLevel);
            }
            else
            {
                ShowPreloadingUI($"Preparing Level {currentLevel}...");
                LoadLevelDirectly(currentLevel);
            }
        }

        /// <summary>
        /// Called when New Game button is pressed
        /// Preloads Level 1
        /// </summary>
        public void OnNewGameButtonPressed()
        {
            // Reset progress for new game
            GameProgressManager.Instance.ResetAllProgress();

            // Unload any preloaded scenes that are no longer needed (e.g., Level2 preloaded for a returning player)
            AddressablesSceneLoader.Instance.UnloadAllPreloadedScenes();

            // New game should play Video1 first
            ShowPreloadingUI("Loading Video 1...");
            PreloadVideoThenLevel(1);
        }

        /// <summary>
        /// Automatically preload Video1 in background when menu loads
        /// </summary>
        private void PreloadAppropriateLevel()
        {
            // If player has beaten the game or is a new player, preload Video1
            if (GameProgressManager.Instance.HasBeatenGame())
            {
                Debug.Log($"[MainMenu] Player has beaten all levels - preloading Video1 for New Game...");
                AddressablesSceneLoader.Instance.PreloadVideo(1, (success) =>
                {
                    if (success)
                        Debug.Log($"[MainMenu] Video1 preloaded - ready for instant transition!");
                    else
                        Debug.LogError($"[MainMenu] Failed to preload Video1");
                });
                return;
            }

            int currentLevel = GameProgressManager.Instance.GetCurrentLevel();

            if (currentLevel == 1)
            {
                // New player - preload Video1
                Debug.Log($"[MainMenu] Preloading Video1 in background...");
                AddressablesSceneLoader.Instance.PreloadVideo(1, (success) =>
                {
                    if (success)
                        Debug.Log($"[MainMenu] Video1 preloaded - ready for instant transition!");
                    else
                        Debug.LogError($"[MainMenu] Failed to preload Video1");
                });
            }
            else
            {
                // Returning player - preload their current level
                Debug.Log($"[MainMenu] Preloading Level{currentLevel} in background...");
                AddressablesSceneLoader.Instance.PreloadLevel(currentLevel, (success) =>
                {
                    if (success)
                        Debug.Log($"[MainMenu] Level{currentLevel} preloaded - ready for instant transition!");
                    else
                        Debug.LogError($"[MainMenu] Failed to preload Level{currentLevel}");
                });
            }
        }

        private void ShowPreloadingUI(string message)
        {
            if (!showPreloadProgress) return;

            if (preloadingIndicator != null)
            {
                preloadingIndicator.SetActive(true);
            }

            if (preloadStatusText != null)
            {
                preloadStatusText.text = message;
            }
        }

        private void HidePreloadingUI()
        {
            if (preloadingIndicator != null)
            {
                preloadingIndicator.SetActive(false);
            }
        }

        /// <summary>
        /// Check if the player should see the video before entering the level
        /// </summary>
        private bool ShouldPlayVideoBeforeLevel(int levelNumber)
        {
            // Check if video is unlocked
            if (!GameProgressManager.Instance.IsVideoUnlocked(levelNumber))
                return false;

            // Check if player has already seen this video
            return !HasSeenVideoBefore(levelNumber);
        }

        private bool HasSeenVideoBefore(int videoNumber)
        {
            return PlayerPrefs.GetInt($"Video_{videoNumber}_Seen", 0) == 1;
        }

        /// <summary>
        /// Preload video, play it, then go to level
        /// </summary>
        private void PreloadVideoThenLevel(int levelNumber)
        {
            string videoLabel = $"Video{levelNumber}_Assets";

            PreloadManager.Instance.PreloadLabel(videoLabel, (success) =>
            {
                HidePreloadingUI();
                if (success)
                {
                    // Set which level to load after video
                    PlayerPrefs.SetInt("ReturnToLevel", levelNumber);
                    PlayerPrefs.SetString("ReturnScene", "Level");
                    PlayerPrefs.Save();

                    // Load the video
                    SceneTransitionManager.Instance.LoadVideo(levelNumber);
                }
                else
                {
                    // Failed to download video, go to level directly
                    Debug.LogWarning($"Failed to download video{levelNumber}, loading level directly");
                    LoadLevelDirectly(levelNumber);
                }
            });
        }

        /// <summary>
        /// Load level directly without video
        /// </summary>
        private void LoadLevelDirectly(int levelNumber)
        {
            string levelLabel = $"Level{levelNumber}_Assets";

            PreloadManager.Instance.PreloadLabel(levelLabel, (success) =>
            {
                HidePreloadingUI();
                if (success)
                {
                    SceneTransitionManager.Instance.LoadLevel(levelNumber);
                }
                else
                {
                    Debug.LogError($"Failed to download Level{levelNumber}");
                }
            });
        }
    }
}
