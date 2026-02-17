using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using System.Collections.Generic;
using AddressablesAPI = UnityEngine.AddressableAssets.Addressables;

namespace SickOfIt.Core.Addressables
{
    public class AddressablesAssetLoader : MonoBehaviour
    {
        private static AddressablesAssetLoader _instance;
        public static AddressablesAssetLoader Instance
        {
            get
            {
                if (_instance == null)
                {
                    GameObject go = new GameObject("AddressablesAssetLoader");
                    _instance = go.AddComponent<AddressablesAssetLoader>();
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

        #region Instantiate

        public void InstantiatePrefab(string address, System.Action<GameObject> onComplete, Transform parent = null)
        {
            StartCoroutine(InstantiatePrefabAsync(address, onComplete, parent));
        }

        private System.Collections.IEnumerator InstantiatePrefabAsync(string address, System.Action<GameObject> onComplete, Transform parent)
        {
            var handle = AddressablesAPI.InstantiateAsync(address, parent);
            yield return handle;

            if (handle.Status == AsyncOperationStatus.Succeeded)
                onComplete?.Invoke(handle.Result);
            else
            {
                Debug.LogError($"Failed to instantiate prefab: {address}. Error: {handle.OperationException}");
                onComplete?.Invoke(null);
            }
        }

        public void InstantiatePrefabAtPosition(string address, Vector3 position, Quaternion rotation, 
            System.Action<GameObject> onComplete, Transform parent = null)
        {
            StartCoroutine(InstantiatePrefabAtPositionAsync(address, position, rotation, onComplete, parent));
        }

        private System.Collections.IEnumerator InstantiatePrefabAtPositionAsync(string address, Vector3 position, 
            Quaternion rotation, System.Action<GameObject> onComplete, Transform parent)
        {
            var handle = AddressablesAPI.InstantiateAsync(address, position, rotation, parent);
            yield return handle;

            if (handle.Status == AsyncOperationStatus.Succeeded)
                onComplete?.Invoke(handle.Result);
            else
            {
                Debug.LogError($"Failed to instantiate prefab: {address}. Error: {handle.OperationException}");
                onComplete?.Invoke(null);
            }
        }

        #endregion

        #region Load Assets

        public void LoadAsset<T>(string address, System.Action<T> onComplete) where T : Object
        {
            StartCoroutine(LoadAssetAsync(address, onComplete));
        }

        private System.Collections.IEnumerator LoadAssetAsync<T>(string address, System.Action<T> onComplete) where T : Object
        {
            bool callbackInvoked = false;
            T result = null;

            AddressablesOperationManager.Instance.LoadAsset<T>(address, (asset) =>
            {
                result = asset;
                callbackInvoked = true;
            });

            while (!callbackInvoked)
                yield return null;

            onComplete?.Invoke(result);
        }

        public void LoadPrefabThenInstantiate(string address, System.Action<GameObject> onComplete, Transform parent = null)
        {
            LoadAsset<GameObject>(address, (prefab) =>
            {
                if (prefab != null)
                {
                    GameObject instance = Instantiate(prefab, parent);
                    onComplete?.Invoke(instance);
                }
                else
                    onComplete?.Invoke(null);
            });
        }

        public void LoadAssetsByLabel<T>(string label, System.Action<IList<T>> onComplete) where T : Object
        {
            StartCoroutine(LoadAssetsByLabelAsync(label, onComplete));
        }

        private System.Collections.IEnumerator LoadAssetsByLabelAsync<T>(string label, System.Action<IList<T>> onComplete) where T : Object
        {
            bool callbackInvoked = false;
            IList<T> result = null;

            AddressablesOperationManager.Instance.LoadLabel<T>(label, (assets) =>
            {
                result = assets;
                callbackInvoked = true;
            });

            while (!callbackInvoked)
                yield return null;

            onComplete?.Invoke(result);
        }

        #endregion

        #region Preload

        public void PreloadAsset<T>(string address) where T : Object
        {
            AddressablesOperationManager.Instance.LoadAsset<T>(address, (asset) =>
            {
                if (asset != null) Debug.Log($"Preloaded asset: {address}");
                else Debug.LogError($"Failed to preload asset: {address}");
            });
        }

        public void PreloadAssetsByLabel(string label)
        {
            AddressablesOperationManager.Instance.LoadLabel(label, (success) =>
            {
                if (success) Debug.Log($"Preloaded label: {label}");
                else Debug.LogError($"Failed to preload label: {label}");
            });
        }

        #endregion
    }
}
