using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using SickOfIt.Core.Flow;

namespace SickOfIt.Core.Addressables
{
    public class AddressablesSceneLoader : MonoBehaviour
    {
        private static AddressablesSceneLoader _instance;
        public static AddressablesSceneLoader Instance
        {
            get
            {
                if (_instance == null)
                {
                    GameObject go = new GameObject("AddressablesSceneLoader");
                    _instance = go.AddComponent<AddressablesSceneLoader>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }

        [Header("Loading UI")]
        [SerializeField] private GameObject loadingScreenPrefab;

        [Header("Scene Tracking")]
        [SerializeField] private List<string> loadedScenes = new List<string>();
        [SerializeField] private string currentGameplayScene = "";

        private GameObject currentLoadingScreen;
        private bool isLoading = false;
        private HashSet<string> preloadingScenes = new HashSet<string>();

        // Base scenes that should be unloaded when entering gameplay
        private static readonly string[] BASE_SCENES = { "MainMenu", "LevelSelection", "VideoSelection" };

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

        private void OnEnable()
        {
            UnityEngine.SceneManagement.SceneManager.sceneUnloaded += OnSceneUnloaded;
        }

        private void OnDisable()
        {
            UnityEngine.SceneManagement.SceneManager.sceneUnloaded -= OnSceneUnloaded;
        }

        /// <summary>
        /// Called when any scene is unloaded. Handles two cases:
        /// 1. Force-unload: SceneManager.LoadScene(Single) destroyed a preloaded additive scene.
        ///    The Addressables handle is still in activeSceneHandles, so we release it.
        /// 2. Intentional unload: Our UnloadSceneAsync already released the handle via
        ///    AddressablesOperationManager, so activeSceneHandles no longer contains it.
        ///    We just clean up our local tracking.
        /// </summary>
        private void OnSceneUnloaded(Scene unloadedScene)
        {
            string sceneName = unloadedScene.name;

            if (loadedScenes.Remove(sceneName))
            {
                preloadingScenes.Remove(sceneName);

                // If the Addressables handle is still tracked, this was a force-unload.
                // Our intentional UnloadSceneAsync path removes the handle before the scene
                // finishes unloading, so it won't be present here for intentional unloads.
                if (AddressablesOperationManager.Instance.IsSceneLoadedThroughAddressables(sceneName))
                {
                    AddressablesOperationManager.Instance.ReleaseSceneHandle(sceneName);
                    Debug.Log($"[AddressablesSceneLoader] Released orphaned handle for force-unloaded scene '{sceneName}'");
                }
            }

            // Clear currentGameplayScene if it was destroyed out from under us
            if (currentGameplayScene == sceneName)
            {
                currentGameplayScene = "";
            }
        }

        private void Start()
        {
            // Initialize currentGameplayScene if empty (for editor testing)
            if (string.IsNullOrEmpty(currentGameplayScene))
            {
                Scene activeScene = SceneManager.GetActiveScene();
                // Only set for gameplay scenes, not base menu scenes
                if (activeScene.isLoaded && !System.Array.Exists(BASE_SCENES, s => s == activeScene.name))
                {
                    currentGameplayScene = activeScene.name;
                    if (!loadedScenes.Contains(activeScene.name))
                    {
                        loadedScenes.Add(activeScene.name);
                    }
                }
            }
        }

        /// <summary>
        /// Load and activate a scene, unloading the previous gameplay scene
        /// </summary>
        public void LoadScene(string sceneName, System.Action onComplete = null)
        {
            // If scene is preloading, wait for it to finish then activate
            if (preloadingScenes.Contains(sceneName))
            {
                StartCoroutine(WaitForPreloadThenActivate(sceneName, onComplete));
                return;
            }

            if (isLoading)
            {
                Debug.LogWarning($"Already loading a scene. Rejecting request for: {sceneName}");
                onComplete?.Invoke();
                return;
            }

            StartCoroutine(LoadSceneAsync(sceneName, true, onComplete));
        }

        // REMOVED: ReloadCurrentScene() - Use Death scene flow instead
        // Trigger player death or call LevelManager.RestartLevel() to reload a level

        /// <summary>
        /// Preload a scene additively in the background without activating it
        /// </summary>
        public void PreloadScene(string sceneName, System.Action<bool> onComplete = null)
        {
            if (preloadingScenes.Contains(sceneName) || loadedScenes.Contains(sceneName))
            {
                // Scene already loaded
                onComplete?.Invoke(true);
                return;
            }

            StartCoroutine(PreloadSceneAsync(sceneName, onComplete));
        }

        public void LoadLevel(int levelNumber, System.Action onComplete = null)
        {
            LoadScene($"Level{levelNumber}", onComplete);
        }

        public void LoadVideo(int videoNumber, System.Action onComplete = null)
        {
            LoadScene($"Video{videoNumber}", onComplete);
        }

        public void PreloadLevel(int levelNumber, System.Action<bool> onComplete = null)
        {
            PreloadScene($"Level{levelNumber}", onComplete);
        }

        public void PreloadVideo(int videoNumber, System.Action<bool> onComplete = null)
        {
            PreloadScene($"Video{videoNumber}", onComplete);
        }

        private IEnumerator LoadSceneAsync(string sceneName, bool showLoadingUI, System.Action onComplete)
        {
            isLoading = true;
            bool shouldHideLoadingScreen = false;

            try
            {
                // Check if already preloaded
                bool wasPreloaded = loadedScenes.Contains(sceneName);

                if (!wasPreloaded)
                {
                    // Scene not preloaded - load it now with loading UI
                    if (showLoadingUI)
                    {
                        ShowLoadingScreen();
                        shouldHideLoadingScreen = true;
                    }

                    bool loadCompleted = false;
                    bool loadSuccess = false;
                    float currentProgress = 0f;

                    AddressablesOperationManager.Instance.LoadScene(sceneName, (success) =>
                    {
                        loadSuccess = success;
                        loadCompleted = true;
                    }, LoadSceneMode.Additive);

                    while (!loadCompleted)
                    {
                        float progress = AddressablesOperationManager.Instance.GetCurrentSceneLoadProgress(sceneName);
                        if (progress != currentProgress)
                        {
                            currentProgress = progress;
                            if (showLoadingUI) UpdateLoadingProgress(currentProgress);
                        }
                        yield return null;
                    }

                    if (!loadSuccess)
                    {
                        Debug.LogError($"Failed to load scene: {sceneName}");
                        if (shouldHideLoadingScreen) HideLoadingScreen();
                        onComplete?.Invoke();
                        yield break;
                    }

                    loadedScenes.Add(sceneName);
                }
                else
                {
                    // Scene was preloaded - activate immediately (no loading UI needed)
                }

                // Activate the scene
                Scene scene = SceneManager.GetSceneByName(sceneName);
                if (scene.isLoaded)
                {
                    SceneManager.SetActiveScene(scene);

                    // Show the scene's content
                    ActivateSceneContent(scene);
                }
                else if (wasPreloaded)
                {
                    // Scene was tracked as preloaded but is no longer loaded (edge case)
                    Debug.LogWarning($"[AddressablesSceneLoader] Scene '{sceneName}' was tracked as preloaded but is not loaded. Loading fresh.");
                    loadedScenes.Remove(sceneName);

                    if (showLoadingUI)
                    {
                        ShowLoadingScreen();
                        shouldHideLoadingScreen = true;
                    }

                    bool loadCompleted = false;
                    bool loadSuccess = false;

                    AddressablesOperationManager.Instance.LoadScene(sceneName, (success) =>
                    {
                        loadSuccess = success;
                        loadCompleted = true;
                    }, LoadSceneMode.Additive);

                    while (!loadCompleted)
                        yield return null;

                    if (!loadSuccess)
                    {
                        Debug.LogError($"Failed to load scene: {sceneName}");
                        if (shouldHideLoadingScreen) HideLoadingScreen();
                        onComplete?.Invoke();
                        yield break;
                    }

                    loadedScenes.Add(sceneName);
                    scene = SceneManager.GetSceneByName(sceneName);
                    if (scene.isLoaded)
                    {
                        SceneManager.SetActiveScene(scene);
                        ActivateSceneContent(scene);
                    }
                }

                // Unload base menu scenes when entering gameplay
                foreach (string baseSceneName in BASE_SCENES)
                {
                    Scene baseScene = SceneManager.GetSceneByName(baseSceneName);
                    if (baseScene.isLoaded && baseScene.name != sceneName)
                    {
                        SceneManager.UnloadSceneAsync(baseScene);
                    }
                }

                // Simple rule: Unload the previous scene (whatever we came from)
                if (!string.IsNullOrEmpty(currentGameplayScene) && currentGameplayScene != sceneName)
                {
                    // Hide previous scene's content before unloading
                    Scene previousScene = SceneManager.GetSceneByName(currentGameplayScene);
                    if (previousScene.isLoaded)
                    {
                        DeactivateSceneContent(previousScene);
                    }

                    // Unload in background
                    StartCoroutine(UnloadSceneAsync(currentGameplayScene));
                }

                // Update current scene (Video, Death, and Level scenes all become "current")
                currentGameplayScene = sceneName;

                AddressablesOperationManager.Instance.SetCurrentScene(sceneName);
                AddressablesOperationManager.Instance.CompleteSceneTransition();

                if (shouldHideLoadingScreen) HideLoadingScreen();
                onComplete?.Invoke();
            }
            finally
            {
                // Always reset the loading flag, even if an exception occurs
                isLoading = false;
            }
        }

        private IEnumerator WaitForPreloadThenActivate(string sceneName, System.Action onComplete)
        {
            // Wait for preload to finish
            while (preloadingScenes.Contains(sceneName))
            {
                yield return null;
            }

            // Now activate the preloaded scene
            StartCoroutine(LoadSceneAsync(sceneName, false, onComplete));
        }

        private IEnumerator PreloadSceneAsync(string sceneName, System.Action<bool> onComplete)
        {
            if (loadedScenes.Contains(sceneName))
            {
                onComplete?.Invoke(true);
                yield break;
            }

            if (preloadingScenes.Contains(sceneName))
            {
                onComplete?.Invoke(true);
                yield break;
            }

            preloadingScenes.Add(sceneName);

            bool loadCompleted = false;
            bool loadSuccess = false;

            AddressablesOperationManager.Instance.LoadScene(sceneName, (success) =>
            {
                loadSuccess = success;
                loadCompleted = true;
            }, LoadSceneMode.Additive);

            while (!loadCompleted)
                yield return null;

            preloadingScenes.Remove(sceneName);

            if (loadSuccess)
            {
                loadedScenes.Add(sceneName);
            }
            else
            {
                Debug.LogError($"[AddressablesSceneLoader] Failed to preload scene '{sceneName}'");
            }

            onComplete?.Invoke(loadSuccess);
        }

        private IEnumerator UnloadSceneAsync(string sceneName)
        {
            if (!loadedScenes.Contains(sceneName))
            {
                Debug.LogWarning($"[AddressablesSceneLoader] Scene '{sceneName}' not tracked as loaded");
                yield break;
            }

            // Check if scene was loaded through Addressables or directly (editor testing)
            bool isAddressablesLoaded = AddressablesOperationManager.Instance.IsSceneLoadedThroughAddressables(sceneName);

            if (isAddressablesLoaded)
            {
                // Addressables unload: removes the handle from activeSceneHandles first,
                // then unloads the scene. When OnSceneUnloaded fires, it sees no handle
                // and just cleans up loadedScenes without trying to release again.
                bool unloadCompleted = false;
                AddressablesOperationManager.Instance.UnloadScene(sceneName, (success) =>
                {
                    unloadCompleted = true;
                });

                while (!unloadCompleted)
                    yield return null;
            }
            else
            {
                // Scene was loaded directly (e.g., in editor) - use Unity's SceneManager
                Scene scene = SceneManager.GetSceneByName(sceneName);
                if (scene.isLoaded)
                {
                    yield return SceneManager.UnloadSceneAsync(scene);
                }
            }

            // OnSceneUnloaded handles loadedScenes cleanup, but remove here too as a safety net
            loadedScenes.Remove(sceneName);
        }

        /// <summary>
        /// Unload all preloaded scenes that aren't the current gameplay scene.
        /// Useful when the player's intent changes (e.g., New Game after a preload for Continue).
        /// </summary>
        public void UnloadAllPreloadedScenes()
        {
            // Copy the list since we'll modify it during iteration
            List<string> scenesToUnload = new List<string>(loadedScenes);

            foreach (string sceneName in scenesToUnload)
            {
                // Don't unload the current gameplay scene or base menu scenes
                if (sceneName == currentGameplayScene)
                    continue;
                if (System.Array.Exists(BASE_SCENES, s => s == sceneName))
                    continue;

                Debug.Log($"[AddressablesSceneLoader] Unloading stale preloaded scene: {sceneName}");
                StartCoroutine(UnloadSceneAsync(sceneName));
            }
        }

        public void CacheSceneAssets(string sceneName)
        {
            PreloadManager.Instance.PreloadLabel(sceneName, null);
        }

        public void GetSceneDownloadSize(string sceneName, System.Action<long> onSizeRetrieved)
        {
            AddressablesOperationManager.Instance.GetLabelDownloadSize(sceneName, onSizeRetrieved);
        }

        private void ShowLoadingScreen()
        {
            if (loadingScreenPrefab != null && currentLoadingScreen == null)
            {
                currentLoadingScreen = Instantiate(loadingScreenPrefab);
                DontDestroyOnLoad(currentLoadingScreen);
            }
        }

        private void HideLoadingScreen()
        {
            if (currentLoadingScreen != null)
            {
                Destroy(currentLoadingScreen);
                currentLoadingScreen = null;
            }
        }

        private void UpdateLoadingProgress(float progress)
        {
            if (currentLoadingScreen != null)
            {
                var progressText = currentLoadingScreen.GetComponentInChildren<TMPro.TextMeshProUGUI>();
                if (progressText != null)
                    progressText.text = $"Loading... {progress * 100:F0}%";
            }
        }

        private void ActivateSceneContent(Scene scene)
        {
            foreach (GameObject rootObj in scene.GetRootGameObjects())
            {
                var controller = rootObj.GetComponentInChildren<SceneContentController>(true);
                if (controller != null)
                {
                    controller.ActivateScene();
                    return;
                }
            }
            Debug.LogWarning($"[AddressablesSceneLoader] No SceneContentController found in scene '{scene.name}'");
        }

        private void DeactivateSceneContent(Scene scene)
        {
            foreach (GameObject rootObj in scene.GetRootGameObjects())
            {
                var controller = rootObj.GetComponentInChildren<SceneContentController>(true);
                if (controller != null)
                {
                    controller.DeactivateScene();
                    return;
                }
            }
        }
    }
}
