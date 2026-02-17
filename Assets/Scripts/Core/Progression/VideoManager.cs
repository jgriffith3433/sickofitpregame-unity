using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using MobileControllerFramework;
using SickOfIt.Core.Addressables;
using SickOfIt.Core.Flow;

namespace SickOfIt.Core.Progression
{
    /// <summary>
    /// Manager for video scenes. Handles loading of SpriteFramePlayer and manages scene activation.
    /// Also preloads next level during video playback for seamless transitions.
    /// </summary>
    public class VideoManager : MonoBehaviour
    {
        [Header("Video Info")]
        [SerializeField] private int videoNumber = 1;

        [Header("Preload Next Content")]
        [SerializeField] private bool preloadDuringPlayback = true;
        [Tooltip("Preload the next level while video is playing")]
        [SerializeField] private bool preloadNextLevel = true;

        [Header("Scene Transition")]
        [Tooltip("If enabled, returns to main menu after video instead of loading next level")]
        [SerializeField] private bool returnToMainMenu = false;

        [Header("Skip Settings")]
        [Tooltip("Optional skip button using MobileControllerFramework")]
        [SerializeField] private MobileControllerFramework.Button skipButton;

        [Header("Loading System")]
        [Tooltip("Video sprite player that needs to finish loading")]
        [SerializeField] private MonoBehaviour videoPlayerBehaviour;

        [Tooltip("GameObjects to activate once video assets are loaded (they should start deactivated)")]
        [SerializeField] private List<GameObject> objectsToActivateAfterLoading = new List<GameObject>();

        [Tooltip("GameObjects to deactivate once video assets are loaded (e.g., loading indicators)")]
        [SerializeField] private List<GameObject> objectsToDeactivateAfterLoading = new List<GameObject>();

        [Header("Loading Debug")]
        [SerializeField] private bool isLoading = false;
        [SerializeField] private bool videoLoaded = false;

        private IFrameSequencePlayer videoPlayer;
        private bool videoWasSkipped = false;

        private void Awake()
        {
            videoPlayer = videoPlayerBehaviour as IFrameSequencePlayer;

            // Subscribe to skip button
            if (skipButton != null)
            {
                skipButton.TapEndEvent.AddListener(SkipVideo);
            }

            // Subscribe to video completion callback
            if (videoPlayer != null)
            {
                videoPlayer.PlaybackCompleted += OnVideoComplete;
                isLoading = true;
                StartCoroutine(WaitForVideoToLoad());
            }
            else
            {
                // No video player, activate everything immediately
                ActivateGameObjects();
            }
        }

        private void OnDestroy()
        {
            // Unsubscribe from callback
            if (videoPlayer != null)
            {
                videoPlayer.PlaybackCompleted -= OnVideoComplete;
            }

            // Unsubscribe from skip button
            if (skipButton != null)
            {
                skipButton.TapEndEvent.RemoveListener(SkipVideo);
            }
        }

        /// <summary>
        /// Called when video playback completes
        /// </summary>
        private void OnVideoComplete()
        {
            // Track video completion (only if not skipped)
            if (!videoWasSkipped)
            {
                SickOfIt.Core.AnalyticsManager.Instance.TrackVideoComplete(videoNumber);
            }

            // Open relative URL based on video number
            if (videoNumber == 1)
            {
                Debug.Log("VideoManager: Video 1 complete, opening /Game");
                OpenRelativeURL("/Game");
                return;
            }
            else if (videoNumber == 2)
            {
                Debug.Log("VideoManager: Video 2 complete, opening /TakeGame");
                OpenRelativeURL("/TakeGame");
                return;
            }

            // Check if configured to return to main menu
            if (returnToMainMenu)
            {
                SceneTransitionManager.Instance.LoadMainMenu();
                return;
            }

            // Check if we should return to a specific level (set by main menu or video selection)
            if (PlayerPrefs.HasKey("ReturnToLevel"))
            {
                int levelToLoad = PlayerPrefs.GetInt("ReturnToLevel");
                PlayerPrefs.DeleteKey("ReturnToLevel");
                PlayerPrefs.Save();

                // Special case: -1 means return to main menu (from video gallery)
                if (levelToLoad == -1)
                {
                    SceneTransitionManager.Instance.LoadMainMenu();
                }
                else
                {
                    SceneTransitionManager.Instance.LoadLevel(levelToLoad);
                }
            }
            else
            {
                // Default behavior: Load the level matching this video number
                SceneTransitionManager.Instance.LoadLevel(videoNumber);
            }
        }

        /// <summary>
        /// Wait for video assets to finish loading
        /// </summary>
        private IEnumerator WaitForVideoToLoad()
        {
            // Wait until video is loaded
            while (videoPlayer != null && !videoPlayer.IsLoaded)
            {
                yield return null; // Wait one frame
            }

            videoLoaded = true;
            isLoading = false;

            // Deactivate loading indicators
            DeactivateGameObjects();

            // Activate game objects now that loading is complete
            ActivateGameObjects();

            // Play the video automatically
            if (videoPlayer != null)
            {
                videoPlayer.Play();

                // Track video start
                SickOfIt.Core.AnalyticsManager.Instance.TrackVideoStart(videoNumber);

                // Preload next content while video is playing
                if (preloadDuringPlayback)
                {
                    StartCoroutine(PreloadNextContentDuringVideo());
                }
            }
        }

        /// <summary>
        /// Skip the currently playing video
        /// </summary>
        public void SkipVideo()
        {
            if (videoPlayer != null)
            {
                Debug.Log("VideoManager: Video skipped by player");

                // Track video skip
                videoWasSkipped = true;
                SickOfIt.Core.AnalyticsManager.Instance.TrackVideoSkip(videoNumber);

                videoPlayer.SkipToEnd();
            }
        }

        /// <summary>
        /// Preload the next level during video playback
        /// </summary>
        private IEnumerator PreloadNextContentDuringVideo()
        {
            // Wait a bit to let video start smoothly
            yield return new WaitForSeconds(1f);

            int nextLevel = videoNumber;
            Debug.Log($"VideoManager: Preloading Level{nextLevel} during video playback...");

            AddressablesSceneLoader.Instance.PreloadLevel(nextLevel, (success) =>
            {
                if (success)
                    Debug.Log($"VideoManager: Level{nextLevel} preloaded successfully!");
                else
                    Debug.LogError($"VideoManager: Failed to preload Level{nextLevel}");
            });
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
                        Debug.Log($"VideoManager: Activated {obj.name}");
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
                        Debug.Log($"VideoManager: Deactivated {obj.name}");
                    }
                }
            }
        }

        /// <summary>
        /// Open a relative URL (WebGL-compatible)
        /// </summary>
        private void OpenRelativeURL(string relativeUrl)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            OpenURLInCurrentWindow(relativeUrl);
#else
            Debug.Log($"VideoManager: Would navigate to {relativeUrl} (WebGL only)");
            Application.OpenURL(relativeUrl);
#endif
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern void OpenURLInCurrentWindow(string url);
#endif
    }
}
