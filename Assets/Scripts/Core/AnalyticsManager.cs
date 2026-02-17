using UnityEngine;
using System.Runtime.InteropServices;

namespace SickOfIt.Core
{
    /// <summary>
    /// Manages analytics event tracking for the game using Umami Analytics.
    /// Uses JavaScript interop for WebGL builds to communicate with the website's analytics wrapper.
    /// </summary>
    public class AnalyticsManager : MonoBehaviour
    {
        private static AnalyticsManager _instance;

        public static AnalyticsManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    GameObject go = new GameObject("AnalyticsManager");
                    _instance = go.AddComponent<AnalyticsManager>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        // ============================================================
        // JavaScript Interop for WebGL Builds
        // ============================================================
        // These functions call the JavaScript wrapper on the website
        // The [DllImport("__Internal")] tells Unity to look for these functions in the browser
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern void trackGameStart();

        [DllImport("__Internal")]
        private static extern void trackLevel1Start();

        [DllImport("__Internal")]
        private static extern void trackLevel2Start();

        [DllImport("__Internal")]
        private static extern void trackLevel3Start();

        [DllImport("__Internal")]
        private static extern void trackLevel4Start();

        [DllImport("__Internal")]
        private static extern void trackLevel1Complete();

        [DllImport("__Internal")]
        private static extern void trackLevel2Complete();

        [DllImport("__Internal")]
        private static extern void trackLevel3Complete();

        [DllImport("__Internal")]
        private static extern void trackLevel4Complete();

        [DllImport("__Internal")]
        private static extern void trackVideo1Start();

        [DllImport("__Internal")]
        private static extern void trackVideo2Start();

        [DllImport("__Internal")]
        private static extern void trackVideo3Start();

        [DllImport("__Internal")]
        private static extern void trackVideo4Start();

        [DllImport("__Internal")]
        private static extern void trackVideo1Complete();

        [DllImport("__Internal")]
        private static extern void trackVideo2Complete();

        [DllImport("__Internal")]
        private static extern void trackVideo3Complete();

        [DllImport("__Internal")]
        private static extern void trackVideo4Complete();

        [DllImport("__Internal")]
        private static extern void trackVideo1Skip();

        [DllImport("__Internal")]
        private static extern void trackVideo2Skip();

        [DllImport("__Internal")]
        private static extern void trackVideo3Skip();

        [DllImport("__Internal")]
        private static extern void trackVideo4Skip();

        [DllImport("__Internal")]
        private static extern void trackPlayerDeath(string cause);

        [DllImport("__Internal")]
        private static extern void trackGameOver();
#endif

        // ============================================================
        // Public API - Call these methods from anywhere in the game
        // ============================================================

        /// <summary>
        /// Track when the game starts (player begins playing for the first time in a session)
        /// </summary>
        public void TrackGameStart()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            try
            {
                trackGameStart();
                Debug.Log("[Analytics] Tracked: game_start");
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[Analytics] Failed to track game_start: {e.Message}");
            }
#else
            Debug.Log("[Analytics] (Simulated) Tracked: game_start");
#endif
        }

        /// <summary>
        /// Track when a level starts (player enters the level and gains control)
        /// </summary>
        /// <param name="levelNumber">The level number that is starting (1-4)</param>
        public void TrackLevelStart(int levelNumber)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            try
            {
                switch (levelNumber)
                {
                    case 1:
                        trackLevel1Start();
                        break;
                    case 2:
                        trackLevel2Start();
                        break;
                    case 3:
                        trackLevel3Start();
                        break;
                    case 4:
                        trackLevel4Start();
                        break;
                    default:
                        Debug.LogWarning($"[Analytics] Level {levelNumber} start tracking not implemented");
                        return;
                }
                Debug.Log($"[Analytics] Tracked: level{levelNumber}_start");
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[Analytics] Failed to track level{levelNumber}_start: {e.Message}");
            }
#else
            Debug.Log($"[Analytics] (Simulated) Tracked: level{levelNumber}_start");
#endif
        }

        /// <summary>
        /// Track when a level is completed
        /// </summary>
        /// <param name="levelNumber">The level number that was completed (1-4)</param>
        public void TrackLevelComplete(int levelNumber)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            try
            {
                switch (levelNumber)
                {
                    case 1:
                        trackLevel1Complete();
                        break;
                    case 2:
                        trackLevel2Complete();
                        break;
                    case 3:
                        trackLevel3Complete();
                        break;
                    case 4:
                        trackLevel4Complete();
                        break;
                    default:
                        Debug.LogWarning($"[Analytics] Level {levelNumber} tracking not implemented");
                        return;
                }
                Debug.Log($"[Analytics] Tracked: level{levelNumber}_complete");
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[Analytics] Failed to track level{levelNumber}_complete: {e.Message}");
            }
#else
            Debug.Log($"[Analytics] (Simulated) Tracked: level{levelNumber}_complete");
#endif
        }

        /// <summary>
        /// Track when a video starts playing
        /// </summary>
        /// <param name="videoNumber">The video number that is starting (1-4)</param>
        public void TrackVideoStart(int videoNumber)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            try
            {
                switch (videoNumber)
                {
                    case 1:
                        trackVideo1Start();
                        break;
                    case 2:
                        trackVideo2Start();
                        break;
                    case 3:
                        trackVideo3Start();
                        break;
                    case 4:
                        trackVideo4Start();
                        break;
                    default:
                        Debug.LogWarning($"[Analytics] Video {videoNumber} start tracking not implemented");
                        return;
                }
                Debug.Log($"[Analytics] Tracked: video{videoNumber}_start");
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[Analytics] Failed to track video{videoNumber}_start: {e.Message}");
            }
#else
            Debug.Log($"[Analytics] (Simulated) Tracked: video{videoNumber}_start");
#endif
        }

        /// <summary>
        /// Track when a video completes (plays to the end)
        /// </summary>
        /// <param name="videoNumber">The video number that was completed (1-4)</param>
        public void TrackVideoComplete(int videoNumber)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            try
            {
                switch (videoNumber)
                {
                    case 1:
                        trackVideo1Complete();
                        break;
                    case 2:
                        trackVideo2Complete();
                        break;
                    case 3:
                        trackVideo3Complete();
                        break;
                    case 4:
                        trackVideo4Complete();
                        break;
                    default:
                        Debug.LogWarning($"[Analytics] Video {videoNumber} complete tracking not implemented");
                        return;
                }
                Debug.Log($"[Analytics] Tracked: video{videoNumber}_complete");
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[Analytics] Failed to track video{videoNumber}_complete: {e.Message}");
            }
#else
            Debug.Log($"[Analytics] (Simulated) Tracked: video{videoNumber}_complete");
#endif
        }

        /// <summary>
        /// Track when a video is skipped by the player
        /// </summary>
        /// <param name="videoNumber">The video number that was skipped (1-4)</param>
        public void TrackVideoSkip(int videoNumber)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            try
            {
                switch (videoNumber)
                {
                    case 1:
                        trackVideo1Skip();
                        break;
                    case 2:
                        trackVideo2Skip();
                        break;
                    case 3:
                        trackVideo3Skip();
                        break;
                    case 4:
                        trackVideo4Skip();
                        break;
                    default:
                        Debug.LogWarning($"[Analytics] Video {videoNumber} skip tracking not implemented");
                        return;
                }
                Debug.Log($"[Analytics] Tracked: video{videoNumber}_skip");
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[Analytics] Failed to track video{videoNumber}_skip: {e.Message}");
            }
#else
            Debug.Log($"[Analytics] (Simulated) Tracked: video{videoNumber}_skip");
#endif
        }

        /// <summary>
        /// Track when the player dies
        /// </summary>
        /// <param name="cause">The cause of death (e.g., "enemy", "fall", "projectile")</param>
        public void TrackPlayerDeath(string cause = "unknown")
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            try
            {
                trackPlayerDeath(cause);
                Debug.Log($"[Analytics] Tracked: player_death (cause: {cause})");
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[Analytics] Failed to track player_death: {e.Message}");
            }
#else
            Debug.Log($"[Analytics] (Simulated) Tracked: player_death (cause: {cause})");
#endif
        }

        /// <summary>
        /// Track when the game is over (all levels completed or player quits)
        /// </summary>
        public void TrackGameOver()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            try
            {
                trackGameOver();
                Debug.Log("[Analytics] Tracked: game_over");
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[Analytics] Failed to track game_over: {e.Message}");
            }
#else
            Debug.Log("[Analytics] (Simulated) Tracked: game_over");
#endif
        }
    }
}
