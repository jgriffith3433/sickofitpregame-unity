using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace SickOfIt.Core.Addressables
{
    public class PreloadManager : MonoBehaviour
    {
        private static PreloadManager _instance;
        public static PreloadManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    GameObject go = new GameObject("PreloadManager");
                    _instance = go.AddComponent<PreloadManager>();
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

        public void PreloadLabel(string label, System.Action<bool> onComplete = null)
        {
            AddressablesOperationManager.Instance.LoadLabel(label, onComplete);
        }

        public void PreloadLabels(List<string> labels, System.Action<bool> onComplete = null)
        {
            StartCoroutine(PreloadMultipleLabelsAsync(labels, onComplete));
        }

        public void GetLabelDownloadSize(string label, System.Action<long> onComplete)
        {
            AddressablesOperationManager.Instance.GetLabelDownloadSize(label, onComplete);
        }

        private IEnumerator PreloadMultipleLabelsAsync(List<string> labels, System.Action<bool> onComplete)
        {
            bool allSucceeded = true;
            int remaining = labels.Count;

            foreach (string label in labels)
            {
                AddressablesOperationManager.Instance.LoadLabel(label, (success) =>
                {
                    if (!success) allSucceeded = false;
                    remaining--;
                });
            }

            while (remaining > 0)
                yield return null;

            onComplete?.Invoke(allSucceeded);
        }
    }
}
