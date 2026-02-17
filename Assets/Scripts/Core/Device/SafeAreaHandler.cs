using UnityEngine;

namespace SickOfIt.Core.Device
{
    /// <summary>
    /// Handles Safe Area for devices with notches (iPhone X, etc.)
    /// Attach this to a UI Canvas or Panel that should respect the safe area.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class SafeAreaHandler : MonoBehaviour
    {
        [Header("Settings")]
        [Tooltip("Apply safe area adjustments")]
        public bool applySafeArea = true;

        [Tooltip("Show debug information")]
        public bool showDebug = false;

        private RectTransform rectTransform;
        private Rect lastSafeArea;
        private Vector2Int lastScreenSize;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            ApplySafeArea();
        }

        private void Update()
        {
            // Check if safe area or screen size changed
            if (Screen.safeArea != lastSafeArea || 
                Screen.width != lastScreenSize.x || 
                Screen.height != lastScreenSize.y)
            {
                ApplySafeArea();
            }
        }

        private void ApplySafeArea()
        {
            if (!applySafeArea)
            {
                // Reset to full screen
                rectTransform.anchorMin = Vector2.zero;
                rectTransform.anchorMax = Vector2.one;
                return;
            }

            Rect safeArea = Screen.safeArea;
            lastSafeArea = safeArea;
            lastScreenSize = new Vector2Int(Screen.width, Screen.height);

            // Convert safe area to normalized coordinates (0-1)
            Vector2 anchorMin = safeArea.position;
            Vector2 anchorMax = safeArea.position + safeArea.size;

            anchorMin.x /= Screen.width;
            anchorMin.y /= Screen.height;
            anchorMax.x /= Screen.width;
            anchorMax.y /= Screen.height;

            // Apply to RectTransform
            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;

            if (showDebug)
            {
                Debug.Log($"SafeArea Applied: Screen={Screen.width}x{Screen.height}, " +
                          $"SafeArea=({safeArea.x}, {safeArea.y}, {safeArea.width}, {safeArea.height})");
            }
        }

        private void OnValidate()
        {
            if (Application.isPlaying && rectTransform != null)
            {
                ApplySafeArea();
            }
        }

    #if UNITY_EDITOR
        [ContextMenu("Apply Safe Area")]
        private void ApplySafeAreaMenu()
        {
            if (rectTransform == null)
                rectTransform = GetComponent<RectTransform>();
            ApplySafeArea();
        }

        [ContextMenu("Reset to Full Screen")]
        private void ResetToFullScreen()
        {
            if (rectTransform == null)
                rectTransform = GetComponent<RectTransform>();

            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
        }
    #endif
    }
}
