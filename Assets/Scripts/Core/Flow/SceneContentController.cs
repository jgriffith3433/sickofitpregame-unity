using UnityEngine;
using System.Collections.Generic;

namespace SickOfIt.Core.Flow
{
    /// <summary>
    /// Controls visibility of scene content for additive scene loading.
    /// Add this to each Level/Video scene. Put all scene content under a root GameObject
    /// and add that root to the contentRoots list.
    /// </summary>
    public class SceneContentController : MonoBehaviour
    {
        [Header("Scene Content")]
        [Tooltip("Root GameObjects containing all scene content - will be hidden when scene is preloaded")]
        [SerializeField] private List<GameObject> contentRoots = new List<GameObject>();

        [Header("Loading UI")]
        [Tooltip("GameObjects to show while waiting for scene to activate (optional)")]
        [SerializeField] private List<GameObject> loadingIndicators = new List<GameObject>();

        [Header("Debug")]
        [SerializeField] private bool isActive = false;

        private void Awake()
        {
            // Always start inactive (scene might be preloaded before being shown)
            isActive = false;
            HideContent();
            ShowLoadingIndicators();
        }

        private void Start()
        {
            // If this is the active scene (e.g., opened directly in editor), activate immediately
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene() == gameObject.scene)
            {
                ActivateScene();
            }
        }

        /// <summary>
        /// Call this when the scene should become visible and active
        /// </summary>
        public void ActivateScene()
        {

            if (isActive) 
            {
                Debug.LogWarning($"[SceneContent] Already active, skipping");
                return;
            }

            isActive = true;
            ShowContent();
            HideLoadingIndicators();
        }

        /// <summary>
        /// Call this when transitioning away from this scene
        /// </summary>
        public void DeactivateScene()
        {
            if (!isActive) return;

            isActive = false;
            HideContent();
            ShowLoadingIndicators();
        }

        private void ShowContent()
        {
            if (contentRoots != null)
            {
                foreach (var root in contentRoots)
                {
                    if (root != null)
                    {
                        root.SetActive(true);
                    }
                    else
                    {
                        Debug.LogWarning("[SceneContent] Found null content root in list");
                    }
                }
            }
            else
            {
                Debug.LogWarning("[SceneContent] contentRoots list is null!");
            }
        }

        private void HideContent()
        {
            foreach (var root in contentRoots)
            {
                if (root != null)
                    root.SetActive(false);
            }
        }

        private void ShowLoadingIndicators()
        {
            foreach (var indicator in loadingIndicators)
            {
                if (indicator != null)
                    indicator.SetActive(true);
            }
        }

        private void HideLoadingIndicators()
        {
            foreach (var indicator in loadingIndicators)
            {
                if (indicator != null)
                    indicator.SetActive(false);
            }
        }
    }
}
