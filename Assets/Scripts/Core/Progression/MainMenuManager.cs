using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using MobileControllerFramework;
using SickOfIt.Core.Addressables;
using SickOfIt.Core.Flow;
using SickOfIt.Data;

namespace SickOfIt.Core.Progression
{
    /// <summary>
    /// Manages the Main Menu scene
    /// </summary>
    public class MainMenuManager : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private MobileControllerFramework.Button continueButton;
        [SerializeField] private MobileControllerFramework.Button newGameButton;
        [SerializeField] private MobileControllerFramework.Button levelSelectButton;
        [SerializeField] private MobileControllerFramework.Button videosButton;
        [SerializeField] private MobileControllerFramework.Button quitButton;

        [Header("Optional UI Elements")]
        [SerializeField] private TextMeshProUGUI continueButtonText;

        [Header("Scene Loading")]
        [SerializeField] private SceneLoadingManager sceneLoadingManager;

        [Header("Transition Animation")]
        [Tooltip("The SpriteFramePlayer to deactivate when Continue/New Game is pressed (e.g. idle menu animation)")]
        [SerializeField] private MonoBehaviour idleSpriteFramePlayer;
        [Tooltip("The SpriteFramePlayer to play as a transition before loading the next scene")]
        [SerializeField] private MonoBehaviour transitionSpriteFramePlayer;

        [Header("Delayed Hide")]
        [Tooltip("How long to wait before hiding the objects (in seconds)")]
        [SerializeField] private float hideDelay = 1.5f;
        [Tooltip("GameObjects to hide after Continue/New Game is pressed")]
        [SerializeField] private List<GameObject> objectsToHideOnStart;

        private void Awake()
        {
            // Preload common shaders/materials to prevent pink shader bug
            PreloadManager.Instance.PreloadLabel("Common");
        }

        private void Start()
        {
            // Track game start when main menu loads
            SickOfIt.Core.AnalyticsManager.Instance.TrackGameStart();

            // Initialize game progress for first-time players
            GameProgressManager.Instance.InitializeNewPlayer();

            SetupButtons();
            UpdateContinueButton();
        }

        private void SetupButtons()
        {
            if (continueButton != null)
                continueButton.TapEndEvent.AddListener(OnContinueClicked);

            if (newGameButton != null)
                newGameButton.TapEndEvent.AddListener(OnNewGameClicked);

            if (levelSelectButton != null)
                levelSelectButton.TapEndEvent.AddListener(OnLevelSelectClicked);

            if (videosButton != null)
                videosButton.TapEndEvent.AddListener(OnVideosClicked);

            if (quitButton != null)
                quitButton.TapEndEvent.AddListener(OnQuitClicked);
        }

        private void UpdateContinueButton()
        {
            if (continueButton != null)
            {
                int currentLevel = GameProgressManager.Instance.GetCurrentLevel();
                int totalLevels = GameProgressManager.Instance.GetTotalLevels();

                // Hide continue button if player has beaten the game
                if (currentLevel > totalLevels)
                {
                    continueButton.gameObject.SetActive(false);
                    Debug.Log("[MainMenu] Player has beaten all levels - Continue button hidden");
                    return;
                }

                // Show button and update text
                continueButton.gameObject.SetActive(true);

                // Update button text if available
                if (continueButtonText != null)
                {
                    continueButtonText.text = $"Continue (Level {currentLevel})";
                }
            }
        }

        private void OnContinueClicked()
        {
            StartCoroutine(DelayedHideObjects(hideDelay));
            StartCoroutine(PlayTransitionThenExecute(() =>
            {
                if (sceneLoadingManager != null)
                    sceneLoadingManager.ContinueGame();
                else
                    SceneTransitionManager.Instance.ContinueGame();
            }));
        }

        private void OnNewGameClicked()
        {
            Debug.Log("[MOBILE DEBUG] New Game button clicked!");

            // Reset all progress and start from level 1
            GameProgressManager.Instance.ResetAllProgress();
            GameProgressManager.Instance.InitializeNewPlayer();

            // Mark Level1 as visited to prevent auto-play loop
            PlayerPrefs.SetInt("Level_1_Visited", 1);

            // Play Video1 first, then load Level1
            PlayerPrefs.SetInt("ReturnToLevel", 1);
            PlayerPrefs.Save();

            Debug.Log("[MOBILE DEBUG] Attempting to load Video1...");
            Debug.Log($"[MOBILE DEBUG] SceneLoadingManager present: {sceneLoadingManager != null}");

            StartCoroutine(DelayedHideObjects(hideDelay));
            StartCoroutine(PlayTransitionThenExecute(() =>
            {
                if (sceneLoadingManager != null)
                    sceneLoadingManager.LoadVideo(1);
                else
                    SceneTransitionManager.Instance.LoadVideo(1);
            }));
        }

        /// <summary>
        /// Waits for the specified delay then deactivates all GameObjects in the objectsToHideOnStart list.
        /// </summary>
        private IEnumerator DelayedHideObjects(float delay)
        {
            yield return new WaitForSeconds(delay);

            if (objectsToHideOnStart != null)
            {
                foreach (var obj in objectsToHideOnStart)
                {
                    if (obj != null)
                        obj.SetActive(false);
                }
                Debug.Log($"[MainMenu] Hidden {objectsToHideOnStart.Count} objects after {delay}s delay");
            }
        }

        /// <summary>
        /// Deactivates the idle SpriteFramePlayer, plays the transition SpriteFramePlayer,
        /// waits for it to finish, then invokes the provided action.
        /// Falls back to invoking the action immediately if the transition player is not assigned.
        /// </summary>
        private IEnumerator PlayTransitionThenExecute(Action onComplete)
        {
            // Deactivate the idle player
            if (idleSpriteFramePlayer != null)
            {
                idleSpriteFramePlayer.gameObject.SetActive(false);
                Debug.Log("[MainMenu] Deactivated idle SpriteFramePlayer");
            }

            // If no transition player is assigned, just proceed immediately
            if (transitionSpriteFramePlayer == null)
            {
                onComplete?.Invoke();
                yield break;
            }

            var player = transitionSpriteFramePlayer as IFrameSequencePlayer;
            if (player == null)
            {
                Debug.LogWarning("[MainMenu] Transition SpriteFramePlayer does not implement IFrameSequencePlayer, skipping transition");
                onComplete?.Invoke();
                yield break;
            }

            // Make sure the transition player's GameObject is active
            transitionSpriteFramePlayer.gameObject.SetActive(true);

            // Wait until loaded
            while (!player.IsLoaded)
            {
                yield return null;
            }

            // Subscribe to completion and play
            bool finished = false;
            void OnFinished()
            {
                finished = true;
            }

            player.PlaybackCompleted += OnFinished;
            player.Play();
            Debug.Log("[MainMenu] Playing transition SpriteFramePlayer");

            // Wait for playback to complete
            while (!finished)
            {
                yield return null;
            }

            player.PlaybackCompleted -= OnFinished;
            Debug.Log("[MainMenu] Transition SpriteFramePlayer finished, proceeding");

            onComplete?.Invoke();
        }

        private void OnLevelSelectClicked()
        {
            SceneLoadingManager.ShowLoadingUI();
            SceneTransitionManager.Instance.LoadLevelSelection();
        }

        private void OnVideosClicked()
        {
            SceneLoadingManager.ShowLoadingUI();
            SceneTransitionManager.Instance.LoadVideoSelection();
        }

        private void OnQuitClicked()
        {
            SceneTransitionManager.Instance.QuitGame();
        }
    }
}
