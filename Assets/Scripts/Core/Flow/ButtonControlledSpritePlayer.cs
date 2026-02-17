using UnityEngine;
using MobileControllerFramework;

namespace SickOfIt.Core.Flow
{
    /// <summary>
    /// Controls multiple SpriteFramePlayers based on button presses.
    /// Plays an idle/default player automatically, then switches to action players when buttons are pressed.
    /// </summary>
    public class ButtonControlledSpritePlayer : MonoBehaviour
    {
        [Header("Sprite Frame Players")]
        [Tooltip("The default/idle sprite frame player that plays automatically and loops")]
        [SerializeField] private MonoBehaviour idlePlayerBehaviour;

        [Tooltip("Sprite frame player to play when button 1 is pressed")]
        [SerializeField] private MonoBehaviour action1PlayerBehaviour;

        [Tooltip("Sprite frame player to play when button 2 is pressed")]
        [SerializeField] private MonoBehaviour action2PlayerBehaviour;

        [Header("Control Buttons")]
        [Tooltip("Button that triggers action sprite player 1")]
        [SerializeField] private MobileControllerFramework.Button button1;

        [Tooltip("Button that triggers action sprite player 2")]
        [SerializeField] private MobileControllerFramework.Button button2;

        [Header("Settings")]
        [Tooltip("If true, idle player will be shown again after action players complete")]
        [SerializeField] private bool returnToIdleAfterAction = false;

        [Tooltip("If true, action players will hide the idle player when they start playing")]
        [SerializeField] private bool hideIdleWhenPlayingAction = true;

        private IFrameSequencePlayer idlePlayer;
        private IFrameSequencePlayer action1Player;
        private IFrameSequencePlayer action2Player;

        private bool isPlayingAction = false;

        private void Awake()
        {
            // Get IFrameSequencePlayer interfaces
            if (idlePlayerBehaviour != null)
            {
                idlePlayer = idlePlayerBehaviour as IFrameSequencePlayer;
                if (idlePlayer == null)
                {
                    Debug.LogWarning("[ButtonControlledSpritePlayer] Idle player does not implement IFrameSequencePlayer");
                }
            }

            if (action1PlayerBehaviour != null)
            {
                action1Player = action1PlayerBehaviour as IFrameSequencePlayer;
                if (action1Player == null)
                {
                    Debug.LogWarning("[ButtonControlledSpritePlayer] Action1 player does not implement IFrameSequencePlayer");
                }
            }

            if (action2PlayerBehaviour != null)
            {
                action2Player = action2PlayerBehaviour as IFrameSequencePlayer;
                if (action2Player == null)
                {
                    Debug.LogWarning("[ButtonControlledSpritePlayer] Action2 player does not implement IFrameSequencePlayer");
                }
            }

            // Subscribe to button events
            if (button1 != null)
            {
                button1.TapEndEvent.AddListener(OnButton1Pressed);
            }
            else
            {
                Debug.LogWarning("[ButtonControlledSpritePlayer] Button1 is not assigned!");
            }

            if (button2 != null)
            {
                button2.TapEndEvent.AddListener(OnButton2Pressed);
            }
            else
            {
                Debug.LogWarning("[ButtonControlledSpritePlayer] Button2 is not assigned!");
            }
        }

        private void Start()
        {
            // Preload action players in the background so they're ready when buttons are pressed
            if (action1PlayerBehaviour != null)
            {
                // Use reflection to call PreloadAllChunks if it exists
                var preloadMethod = action1PlayerBehaviour.GetType().GetMethod("PreloadAllChunks", new System.Type[] { });
                if (preloadMethod != null)
                {
                    // Ensure GameObject is initially active for preloading, then hide it
                    bool wasActive = action1PlayerBehaviour.gameObject.activeSelf;
                    action1PlayerBehaviour.gameObject.SetActive(true);
                    preloadMethod.Invoke(action1PlayerBehaviour, null);
                    if (!wasActive)
                    {
                        action1PlayerBehaviour.gameObject.SetActive(false);
                    }
                }
            }

            if (action2PlayerBehaviour != null)
            {
                // Use reflection to call PreloadAllChunks if it exists
                var preloadMethod = action2PlayerBehaviour.GetType().GetMethod("PreloadAllChunks", new System.Type[] { });
                if (preloadMethod != null)
                {
                    // Ensure GameObject is initially active for preloading, then hide it
                    bool wasActive = action2PlayerBehaviour.gameObject.activeSelf;
                    action2PlayerBehaviour.gameObject.SetActive(true);
                    preloadMethod.Invoke(action2PlayerBehaviour, null);
                    if (!wasActive)
                    {
                        action2PlayerBehaviour.gameObject.SetActive(false);
                    }
                }
            }
        }

        private void OnDestroy()
        {
            // Unsubscribe from button events
            if (button1 != null)
            {
                button1.TapEndEvent.RemoveListener(OnButton1Pressed);
            }

            if (button2 != null)
            {
                button2.TapEndEvent.RemoveListener(OnButton2Pressed);
            }

            // Unsubscribe from playback completion events
            if (action1Player != null)
            {
                action1Player.PlaybackCompleted -= OnAction1Completed;
            }

            if (action2Player != null)
            {
                action2Player.PlaybackCompleted -= OnAction2Completed;
            }
        }

        /// <summary>
        /// Play the idle/default sprite frame player
        /// </summary>
        private void PlayIdlePlayer()
        {
            if (idlePlayer == null || idlePlayerBehaviour == null)
            {
                Debug.LogWarning("[ButtonControlledSpritePlayer] Idle player is not assigned or does not implement IFrameSequencePlayer");
                return;
            }

            // Wait for player to load
            if (!idlePlayer.IsLoaded)
            {
                Debug.Log("[ButtonControlledSpritePlayer] Waiting for idle player to load...");
                StartCoroutine(WaitForPlayerLoadThenPlay(idlePlayer, idlePlayerBehaviour));
                return;
            }

            // Show and play idle player
            idlePlayerBehaviour.gameObject.SetActive(true);
            idlePlayer.Play();
            isPlayingAction = false;
        }

        /// <summary>
        /// Called when button 1 is pressed
        /// </summary>
        private void OnButton1Pressed()
        {
            // Ignore if already playing an action
            if (isPlayingAction)
            {
                Debug.Log("[ButtonControlledSpritePlayer] Already playing an action, ignoring button press");
                return;
            }

            Debug.Log("[ButtonControlledSpritePlayer] Button 1 pressed, playing action 1");
            PlayActionPlayer(action1Player, action1PlayerBehaviour, OnAction1Completed);
        }

        /// <summary>
        /// Called when button 2 is pressed
        /// </summary>
        private void OnButton2Pressed()
        {
            // Ignore if already playing an action
            if (isPlayingAction)
            {
                Debug.Log("[ButtonControlledSpritePlayer] Already playing an action, ignoring button press");
                return;
            }

            Debug.Log("[ButtonControlledSpritePlayer] Button 2 pressed, playing action 2");
            PlayActionPlayer(action2Player, action2PlayerBehaviour, OnAction2Completed);
        }

        /// <summary>
        /// Play an action sprite frame player
        /// </summary>
        private void PlayActionPlayer(IFrameSequencePlayer player, MonoBehaviour playerBehaviour, System.Action onComplete)
        {
            if (player == null || playerBehaviour == null)
            {
                Debug.LogWarning("[ButtonControlledSpritePlayer] Action player is not assigned or does not implement IFrameSequencePlayer");
                return;
            }

            // Hide idle player
            if (hideIdleWhenPlayingAction && idlePlayerBehaviour != null)
            {
                idlePlayerBehaviour.gameObject.SetActive(false);
            }

            // Ensure action player GameObject is active
            if (!playerBehaviour.gameObject.activeSelf)
            {
                playerBehaviour.gameObject.SetActive(true);
            }

            // Wait for player to load
            if (!player.IsLoaded)
            {
                Debug.Log("[ButtonControlledSpritePlayer] Waiting for action player to load...");
                StartCoroutine(WaitForPlayerLoadThenPlay(player, playerBehaviour, onComplete));
                return;
            }

            // Player is loaded and GameObject is active, play it
            isPlayingAction = true;

            // Subscribe to completion event
            player.PlaybackCompleted += onComplete;

            // Play the action
            player.Play();
            
            // Deactivate this controller GameObject now that action is playing
            gameObject.SetActive(false);
        }

        /// <summary>
        /// Called when action 1 completes
        /// </summary>
        private void OnAction1Completed()
        {
            Debug.Log("[ButtonControlledSpritePlayer] Action 1 completed");

            // Unsubscribe
            if (action1Player != null)
            {
                action1Player.PlaybackCompleted -= OnAction1Completed;
            }

            // Hide action player
            if (action1PlayerBehaviour != null)
            {
                action1PlayerBehaviour.gameObject.SetActive(false);
            }

            // Return to idle if enabled
            if (returnToIdleAfterAction)
            {
                PlayIdlePlayer();
            }
            else
            {
                isPlayingAction = false;
            }
        }

        /// <summary>
        /// Called when action 2 completes
        /// </summary>
        private void OnAction2Completed()
        {
            Debug.Log("[ButtonControlledSpritePlayer] Action 2 completed");

            // Unsubscribe
            if (action2Player != null)
            {
                action2Player.PlaybackCompleted -= OnAction2Completed;
            }

            // Hide action player
            if (action2PlayerBehaviour != null)
            {
                action2PlayerBehaviour.gameObject.SetActive(false);
            }

            // Return to idle if enabled
            if (returnToIdleAfterAction)
            {
                PlayIdlePlayer();
            }
            else
            {
                isPlayingAction = false;
            }
        }

        /// <summary>
        /// Coroutine to wait for a player to load, then play it
        /// </summary>
        private System.Collections.IEnumerator WaitForPlayerLoadThenPlay(IFrameSequencePlayer player, MonoBehaviour playerBehaviour, System.Action onComplete = null)
        {
            // Wait for player to load
            while (!player.IsLoaded)
            {
                yield return null;
            }

            // Show and play
            playerBehaviour.gameObject.SetActive(true);

            if (onComplete != null)
            {
                // This is an action player
                isPlayingAction = true;
                player.PlaybackCompleted += onComplete;
            }

            player.Play();
            
            // If this is an action player, deactivate the controller GameObject
            if (onComplete != null)
            {
                gameObject.SetActive(false);
            }
        }
    }
}
