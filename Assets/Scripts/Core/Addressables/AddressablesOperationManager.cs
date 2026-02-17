using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using System.Collections;
using System.Collections.Generic;
using AddressablesAPI = UnityEngine.AddressableAssets.Addressables;

namespace SickOfIt.Core.Addressables
{
    /// <summary>
    /// Coordinates Addressables operations to prevent duplicate concurrent loads.
    /// Unity's Addressables system handles caching - we only provide deduplication.
    /// </summary>
    public class AddressablesOperationManager : MonoBehaviour
    {
        private static AddressablesOperationManager _instance;
        public static AddressablesOperationManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    GameObject go = new GameObject("AddressablesOperationManager");
                    _instance = go.AddComponent<AddressablesOperationManager>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }

        [Header("Debug")]
        [SerializeField] private bool showDebugLogs = true;
        [SerializeField] private List<string> activeOperations = new List<string>();

        [Header("Load Settings")]
        [Tooltip("Timeout for loading labels (seconds). Increase for large video assets.")]
        [SerializeField] private float labelLoadTimeout = 180f; // Increased from 90s for large video frames

        [Tooltip("Timeout for loading scenes (seconds).")]
        [SerializeField] private float sceneLoadTimeout = 120f;

        [Header("Scene Tracking")]
        [SerializeField] private string currentSceneName = "";
        [SerializeField] private string previousSceneName = "";

        private Dictionary<string, bool> inProgressLabelLoads = new Dictionary<string, bool>();
        private Dictionary<string, List<System.Action<bool>>> pendingLabelLoadCallbacks = new Dictionary<string, List<System.Action<bool>>>();

        private Dictionary<string, bool> inProgressTypedLabelLoads = new Dictionary<string, bool>();
        private Dictionary<string, bool> inProgressAssetLoads = new Dictionary<string, bool>();

        private Dictionary<string, bool> inProgressSceneLoads = new Dictionary<string, bool>();
        private Dictionary<string, List<System.Action<bool>>> pendingSceneLoadCallbacks = new Dictionary<string, List<System.Action<bool>>>();

        private Dictionary<string, AsyncOperationHandle<SceneInstance>> activeSceneHandles = new Dictionary<string, AsyncOperationHandle<SceneInstance>>();

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

        #region Loading Operations

        public void LoadLabel(string label, System.Action<bool> onComplete = null)
        {
            StartCoroutine(LoadLabelAsync(label, onComplete));
        }

        public void LoadLabel<T>(string label, System.Action<IList<T>> onComplete) where T : UnityEngine.Object
        {
            StartCoroutine(LoadLabelAsync<T>(label, onComplete));
        }

        public void LoadLabelWithHandle<T>(string label, System.Action<AsyncOperationHandle<IList<T>>, IList<T>> onComplete) where T : UnityEngine.Object
        {
            StartCoroutine(LoadLabelWithHandleAsync<T>(label, onComplete));
        }

        public void LoadAsset<T>(string address, System.Action<T> onComplete) where T : UnityEngine.Object
        {
            StartCoroutine(LoadAssetAsync<T>(address, onComplete));
        }

        public void LoadScene(string sceneName, System.Action<bool> onComplete = null, UnityEngine.SceneManagement.LoadSceneMode mode = UnityEngine.SceneManagement.LoadSceneMode.Additive)
        {
            StartCoroutine(LoadSceneAsync(sceneName, mode, onComplete));
        }

        public void UnloadScene(string sceneName, System.Action<bool> onComplete = null)
        {
            StartCoroutine(UnloadSceneAsync(sceneName, onComplete));
        }

        public void GetLabelDownloadSize(string label, System.Action<long> onComplete)
        {
            StartCoroutine(GetDownloadSizeAsync(label, onComplete));
        }

        #endregion

        #region Status & Info

        public bool IsSceneLoadedThroughAddressables(string sceneName)
        {
            return activeSceneHandles.ContainsKey(sceneName);
        }

        /// <summary>
        /// Release an orphaned scene handle (e.g., when a scene was forcefully unloaded
        /// by SceneManager.LoadScene in Single mode). This prevents stale handles from
        /// blocking future loads of the same scene.
        /// </summary>
        public void ReleaseSceneHandle(string sceneName)
        {
            if (activeSceneHandles.ContainsKey(sceneName))
            {
                var handle = activeSceneHandles[sceneName];
                activeSceneHandles.Remove(sceneName);
                if (handle.IsValid())
                {
                    try { AddressablesAPI.Release(handle); }
                    catch (System.Exception e)
                    {
                        // Handle may already be invalid if scene was force-unloaded
                        Log($"[SCENE] Warning releasing handle for '{sceneName}': {e.Message}");
                    }
                }
                Log($"[SCENE] Released orphaned handle for '{sceneName}'");
            }
            // Also clean up any in-progress tracking
            inProgressSceneLoads.Remove(sceneName);
        }

        public bool IsLoading(string label)
        {
            return (inProgressLabelLoads.ContainsKey(label) && inProgressLabelLoads[label]) ||
                   (inProgressTypedLabelLoads.ContainsKey(label) && inProgressTypedLabelLoads[label]) ||
                   (inProgressAssetLoads.ContainsKey(label) && inProgressAssetLoads[label]);
        }

        public bool IsLoadingScene(string sceneName)
        {
            return inProgressSceneLoads.ContainsKey(sceneName) && inProgressSceneLoads[sceneName];
        }

        public float GetCurrentSceneLoadProgress(string sceneName)
        {
            if (activeSceneHandles.ContainsKey(sceneName))
            {
                var handle = activeSceneHandles[sceneName];
                if (handle.IsValid()) return handle.PercentComplete;
            }
            return 0f;
        }

        public List<string> GetActiveOperations() => new List<string>(activeOperations);

        public int GetActiveOperationsCount() => activeOperations.Count;

        public void GetMemoryInfo(out long totalMemoryMB, out long usedMemoryMB, out long monoUsedMB, out long monoHeapMB)
        {
            totalMemoryMB = UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong() / (1024 * 1024);
            usedMemoryMB = UnityEngine.Profiling.Profiler.GetTotalReservedMemoryLong() / (1024 * 1024);
            monoUsedMB = UnityEngine.Profiling.Profiler.GetMonoUsedSizeLong() / (1024 * 1024);
            monoHeapMB = UnityEngine.Profiling.Profiler.GetMonoHeapSizeLong() / (1024 * 1024);
        }

        #endregion

        #region Scene Tracking

        public void SetCurrentScene(string sceneName)
        {
            if (currentSceneName == sceneName) return;
            Log($"[SCENE] {currentSceneName} -> {sceneName}");
            previousSceneName = currentSceneName;
            currentSceneName = sceneName;
        }

        public void CompleteSceneTransition()
        {
            Log($"[SCENE] Transition complete: {currentSceneName}");
        }

        #endregion

        #region Implementation

        private IEnumerator LoadLabelAsync(string label, System.Action<bool> onComplete)
        {
            if (inProgressLabelLoads.ContainsKey(label) && inProgressLabelLoads[label])
            {
                Log($"[DEDUP] Label '{label}' already loading, queuing callback");
                if (!pendingLabelLoadCallbacks.ContainsKey(label))
                    pendingLabelLoadCallbacks[label] = new List<System.Action<bool>>();
                pendingLabelLoadCallbacks[label].Add(onComplete);
                yield break;
            }

            inProgressLabelLoads[label] = true;
            activeOperations.Add($"Load: {label}");
            Log($"Loading label '{label}'...");

            var loadHandle = AddressablesAPI.LoadAssetsAsync<UnityEngine.Object>(label, null);

            float elapsed = 0f;
            float lastProgress = 0f;

            while (!loadHandle.IsDone)
            {
                if (loadHandle.PercentComplete > lastProgress + 0.1f)
                {
                    lastProgress = loadHandle.PercentComplete;
                    Log($"Label '{label}' progress: {(loadHandle.PercentComplete * 100f):F1}%");
                }

                elapsed += Time.unscaledDeltaTime;
                if (elapsed > labelLoadTimeout)
                {
                    Debug.LogError($"Label '{label}' load timeout after {labelLoadTimeout}s! Consider increasing labelLoadTimeout in AddressablesOperationManager.");
                    break;
                }

                yield return null;
            }

            bool success = loadHandle.Status == AsyncOperationStatus.Succeeded;
            if (success) Log($"Loaded label '{label}' ({loadHandle.Result.Count} assets)");
            else Debug.LogError($"Failed to load label '{label}': {loadHandle.OperationException}");

            inProgressLabelLoads[label] = false;
            activeOperations.Remove($"Load: {label}");
            onComplete?.Invoke(success);

            if (pendingLabelLoadCallbacks.ContainsKey(label))
            {
                Log($"[DEDUP] Calling {pendingLabelLoadCallbacks[label].Count} queued callbacks for '{label}'");
                foreach (var callback in pendingLabelLoadCallbacks[label])
                    callback?.Invoke(success);
                pendingLabelLoadCallbacks.Remove(label);
            }
        }

        private IEnumerator LoadLabelAsync<T>(string label, System.Action<IList<T>> onComplete) where T : UnityEngine.Object
        {
            string key = $"{label}+{typeof(T).Name}";

            if (inProgressTypedLabelLoads.ContainsKey(key) && inProgressTypedLabelLoads[key])
            {
                Log($"[DEDUP] Typed label '{label}<{typeof(T).Name}>' already loading");
                yield return new WaitUntil(() => !inProgressTypedLabelLoads.ContainsKey(key) || !inProgressTypedLabelLoads[key]);
            }

            inProgressTypedLabelLoads[key] = true;
            activeOperations.Add($"LoadTyped: {label}<{typeof(T).Name}>");
            Log($"Loading typed label '{label}' as <{typeof(T).Name}>...");

            var loadHandle = AddressablesAPI.LoadAssetsAsync<T>(label, null);

            float elapsed = 0f;
            float lastProgress = 0f;

            while (!loadHandle.IsDone)
            {
                if (loadHandle.PercentComplete > lastProgress + 0.1f)
                {
                    lastProgress = loadHandle.PercentComplete;
                    Log($"Label<{typeof(T).Name}> '{label}' progress: {(loadHandle.PercentComplete * 100f):F1}%");
                }

                elapsed += Time.unscaledDeltaTime;
                if (elapsed > labelLoadTimeout)
                {
                    Debug.LogError($"Label<{typeof(T).Name}> '{label}' load timeout after {labelLoadTimeout}s!");
                    break;
                }

                yield return null;
            }

            bool success = loadHandle.Status == AsyncOperationStatus.Succeeded;
            if (success) Log($"Loaded typed label '{label}<{typeof(T).Name}>' ({loadHandle.Result.Count} assets)");
            else Debug.LogError($"Failed to load typed label '{label}<{typeof(T).Name}>': {loadHandle.OperationException}");

            inProgressTypedLabelLoads[key] = false;
            activeOperations.Remove($"LoadTyped: {label}<{typeof(T).Name}>");
            onComplete?.Invoke(success ? loadHandle.Result : null);
        }

        private IEnumerator LoadLabelWithHandleAsync<T>(string label, System.Action<AsyncOperationHandle<IList<T>>, IList<T>> onComplete) where T : UnityEngine.Object
        {
            string key = $"{label}+{typeof(T).Name}";

            if (inProgressTypedLabelLoads.ContainsKey(key) && inProgressTypedLabelLoads[key])
            {
                Log($"[DEDUP] Typed label '{label}<{typeof(T).Name}>' already loading with handle");
                yield return new WaitUntil(() => !inProgressTypedLabelLoads.ContainsKey(key) || !inProgressTypedLabelLoads[key]);
            }

            inProgressTypedLabelLoads[key] = true;
            activeOperations.Add($"LoadTypedWithHandle: {label}<{typeof(T).Name}>");
            Log($"Loading typed label '{label}' as <{typeof(T).Name}> (returning handle)...");

            var loadHandle = AddressablesAPI.LoadAssetsAsync<T>(label, null);

            float elapsed = 0f;
            float lastProgress = 0f;

            while (!loadHandle.IsDone)
            {
                if (loadHandle.PercentComplete > lastProgress + 0.1f)
                {
                    lastProgress = loadHandle.PercentComplete;
                    Log($"Label<{typeof(T).Name}> '{label}' progress: {(loadHandle.PercentComplete * 100f):F1}%");
                }

                elapsed += Time.unscaledDeltaTime;
                if (elapsed > labelLoadTimeout)
                {
                    Debug.LogError($"Label<{typeof(T).Name}> '{label}' load timeout after {labelLoadTimeout}s!");
                    break;
                }

                yield return null;
            }

            bool success = loadHandle.Status == AsyncOperationStatus.Succeeded;
            if (success) Log($"Loaded typed label '{label}<{typeof(T).Name}>' ({loadHandle.Result.Count} assets) - returning handle");
            else Debug.LogError($"Failed to load typed label '{label}<{typeof(T).Name}>': {loadHandle.OperationException}");

            inProgressTypedLabelLoads[key] = false;
            activeOperations.Remove($"LoadTypedWithHandle: {label}<{typeof(T).Name}>");

            // Return both handle and result
            onComplete?.Invoke(loadHandle, success ? loadHandle.Result : null);
        }

        private IEnumerator LoadAssetAsync<T>(string address, System.Action<T> onComplete) where T : UnityEngine.Object
        {
            string key = $"{address}+{typeof(T).Name}";

            if (inProgressAssetLoads.ContainsKey(key) && inProgressAssetLoads[key])
            {
                Log($"[DEDUP] Asset '{address}<{typeof(T).Name}>' already loading");
                yield return new WaitUntil(() => !inProgressAssetLoads.ContainsKey(key) || !inProgressAssetLoads[key]);
            }

            inProgressAssetLoads[key] = true;
            activeOperations.Add($"LoadAsset: {address}<{typeof(T).Name}>");
            Log($"Loading asset '{address}' as <{typeof(T).Name}>...");

            var loadHandle = AddressablesAPI.LoadAssetAsync<T>(address);
            yield return loadHandle;

            bool success = loadHandle.Status == AsyncOperationStatus.Succeeded;
            if (success) Log($"Loaded asset '{address}<{typeof(T).Name}>'");
            else Debug.LogError($"Failed to load asset '{address}<{typeof(T).Name}>': {loadHandle.OperationException}");

            inProgressAssetLoads[key] = false;
            activeOperations.Remove($"LoadAsset: {address}<{typeof(T).Name}>");
            onComplete?.Invoke(success ? loadHandle.Result : default(T));
        }

        private IEnumerator LoadSceneAsync(string sceneName, UnityEngine.SceneManagement.LoadSceneMode mode, System.Action<bool> onComplete)
        {
            if (inProgressSceneLoads.ContainsKey(sceneName) && inProgressSceneLoads[sceneName])
            {
                Log($"[DEDUP] Scene '{sceneName}' already loading, queuing callback");
                if (!pendingSceneLoadCallbacks.ContainsKey(sceneName))
                    pendingSceneLoadCallbacks[sceneName] = new List<System.Action<bool>>();
                pendingSceneLoadCallbacks[sceneName].Add(onComplete);
                yield break;
            }

            inProgressSceneLoads[sceneName] = true;
            activeOperations.Add($"Scene: {sceneName}");
            Log($"Loading scene '{sceneName}' ({mode})...");

            var sceneHandle = AddressablesAPI.LoadSceneAsync(sceneName, mode);
            activeSceneHandles[sceneName] = sceneHandle;

            float lastProgress = 0f;
            float elapsed = 0f;

            while (!sceneHandle.IsDone)
            {
                if (sceneHandle.PercentComplete > lastProgress + 0.1f)
                {
                    lastProgress = sceneHandle.PercentComplete;
                    Log($"Scene '{sceneName}' progress: {(sceneHandle.PercentComplete * 100f):F1}%");
                }

                elapsed += Time.unscaledDeltaTime;
                if (elapsed > sceneLoadTimeout)
                {
                    Debug.LogError($"Scene '{sceneName}' load timeout after {sceneLoadTimeout}s!");
                    Debug.LogError($"Status: {sceneHandle.Status}, Progress: {sceneHandle.PercentComplete}");
                    if (sceneHandle.OperationException != null)
                        Debug.LogError($"Exception: {sceneHandle.OperationException}");
                    break;
                }

                yield return null;
            }

            bool success = sceneHandle.Status == AsyncOperationStatus.Succeeded;
            if (success) Log($"Loaded scene '{sceneName}'");
            else Debug.LogError($"Failed to load scene '{sceneName}': {sceneHandle.OperationException}");

            inProgressSceneLoads[sceneName] = false;
            activeOperations.Remove($"Scene: {sceneName}");
            // DON'T remove handle - keep it for unloading later
            onComplete?.Invoke(success);

            if (pendingSceneLoadCallbacks.ContainsKey(sceneName))
            {
                Log($"[DEDUP] Calling {pendingSceneLoadCallbacks[sceneName].Count} queued scene callbacks for '{sceneName}'");
                foreach (var callback in pendingSceneLoadCallbacks[sceneName])
                    callback?.Invoke(success);
                pendingSceneLoadCallbacks.Remove(sceneName);
            }
        }

        private IEnumerator UnloadSceneAsync(string sceneName, System.Action<bool> onComplete)
        {
            if (!activeSceneHandles.ContainsKey(sceneName))
            {
                Debug.LogWarning($"Cannot unload scene '{sceneName}' - not tracked as loaded");
                onComplete?.Invoke(false);
                yield break;
            }

            Log($"Unloading scene '{sceneName}'...");
            var sceneHandle = activeSceneHandles[sceneName];

            // Validate handle before attempting unload
            if (!sceneHandle.IsValid())
            {
                Debug.LogWarning($"Scene handle for '{sceneName}' is invalid - may have been unloaded already");
                activeSceneHandles.Remove(sceneName);
                onComplete?.Invoke(true); // Consider this success since scene is already gone
                yield break;
            }

            activeSceneHandles.Remove(sceneName);

            var unloadHandle = AddressablesAPI.UnloadSceneAsync(sceneHandle);

            // Check if the unload operation handle is valid before waiting on it
            if (!unloadHandle.IsValid())
            {
                Debug.LogWarning($"Unload handle for scene '{sceneName}' is invalid - scene may have been unloaded already");
                onComplete?.Invoke(true); // Consider this success since scene is gone
                yield break;
            }

            yield return unloadHandle;

            // Check again if handle became invalid during the wait (race condition)
            if (!unloadHandle.IsValid())
            {
                Debug.LogWarning($"Unload handle for scene '{sceneName}' became invalid during unload - scene may have been unloaded already");
                onComplete?.Invoke(true); // Consider success since scene is gone
                yield break;
            }

            bool success = unloadHandle.Status == AsyncOperationStatus.Succeeded;
            if (success) Log($"Unloaded scene '{sceneName}'");
            else Debug.LogError($"Failed to unload scene '{sceneName}': {unloadHandle.OperationException}");

            onComplete?.Invoke(success);
        }

        private IEnumerator GetDownloadSizeAsync(string label, System.Action<long> onComplete)
        {
            var handle = AddressablesAPI.GetDownloadSizeAsync(label);
            yield return handle;
            onComplete?.Invoke(handle.Result);
            AddressablesAPI.Release(handle);
        }

        private void Log(string message)
        {
            if (showDebugLogs) Debug.Log($"[AddressablesOp] {message}");
        }

        #endregion
    }
}
