using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using SickOfIt.Core.Addressables;
using SickOfIt.Data;

namespace SickOfIt.Core.Flow
{
    /// <summary>
    /// Handles scene loading and transitions
    /// Supports both traditional scene loading and Addressables for optimized WebGL builds
    /// </summary>
    public class SceneTransitionManager : MonoBehaviour
    {
        [Header("Loading Mode")]
        [SerializeField] private bool useAddressables = true; // Set to true for optimized WebGL builds
        private static SceneTransitionManager _instance;
        public static SceneTransitionManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    GameObject go = new GameObject("SceneTransitionManager");
                    _instance = go.AddComponent<SceneTransitionManager>();
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

        // Scene name constants (you can modify these to match your actual scene names)
        public const string MAIN_MENU_SCENE = "MainMenu";
        public const string LEVEL_SELECTION_SCENE = "LevelSelection";
        public const string VIDEO_SELECTION_SCENE = "VideoSelection";

        public void LoadMainMenu()
        {
            // Always load MainMenu normally (not via Addressables) as it's part of the base build
            SceneManager.LoadScene(MAIN_MENU_SCENE);
        }

        public void LoadLevel(int levelNumber)
        {
            string sceneName = $"Level{levelNumber}";

            // Set current scene for memory management
            if (useAddressables)
            {
                AddressablesOperationManager.Instance.SetCurrentScene(sceneName);
            }

            if (useAddressables)
            {
                AddressablesSceneLoader.Instance.LoadScene(sceneName);
            }
            else
            {
                SceneManager.LoadScene(sceneName);
            }
        }

        public void LoadLevelSelection()
        {
            // Always load LevelSelection normally (not via Addressables) as it's part of the base build
            SceneManager.LoadScene(LEVEL_SELECTION_SCENE);
        }

        public void LoadVideoSelection()
        {
            // Always load VideoSelection normally (not via Addressables) as it's part of the base build
            SceneManager.LoadScene(VIDEO_SELECTION_SCENE);
        }

        public void LoadVideo(int videoNumber)
        {
            string sceneName = $"Video{videoNumber}";

            // Set current scene for memory management
            if (useAddressables)
            {
                AddressablesOperationManager.Instance.SetCurrentScene(sceneName);
            }

            if (useAddressables)
            {
                AddressablesSceneLoader.Instance.LoadScene(sceneName);
            }
            else
            {
                SceneManager.LoadScene(sceneName);
            }
        }

        public void LoadDeathScene(string deathSceneName)
        {
            // Set current scene for memory management
            if (useAddressables)
            {
                AddressablesOperationManager.Instance.SetCurrentScene(deathSceneName);
            }

            if (useAddressables)
            {
                AddressablesSceneLoader.Instance.LoadScene(deathSceneName);
            }
            else
            {
                SceneManager.LoadScene(deathSceneName);
            }
        }

        public void ContinueGame()
        {
            int currentLevel = GameProgressManager.Instance.GetCurrentLevel();
            LoadLevel(currentLevel);
        }

        // Reload methods removed - use Death scene flow instead
        // Call LevelManager.RestartLevel() or trigger player death to reload a level

        /// <summary>
        /// Preload the next level's assets during gameplay (only works with Addressables)
        /// Call this while player is playing to cache the next level
        /// </summary>
        public void PreloadNextLevel(int currentLevel)
        {
            if (useAddressables)
            {
                int nextLevel = currentLevel + 1;
                string nextSceneName = $"Level{nextLevel}";
                AddressablesSceneLoader.Instance.CacheSceneAssets(nextSceneName);
            }
        }

        public void QuitGame()
        {
            StartCoroutine(QuitGameCoroutine());
        }

        private IEnumerator QuitGameCoroutine()
        {
            // Exit fullscreen before quitting
            Screen.fullScreen = false;

            // Wait 2 seconds for fullscreen transition
            yield return new WaitForSeconds(2f);

    #if UNITY_WEBGL && !UNITY_EDITOR
            // Close browser window/tab for WebGL builds
            Application.ExternalEval("window.close();");
    #endif

            Application.Quit();
    #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
    #endif
        }
    }
}
