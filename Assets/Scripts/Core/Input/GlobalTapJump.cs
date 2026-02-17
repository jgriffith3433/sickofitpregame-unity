using UnityEngine;
using System.Collections.Generic;
using MobileControllerFramework;

namespace SickOfIt.Core.Input
{
    /// <summary>
    /// Detects tap gestures anywhere on the screen and broadcasts jump
    /// through GameInputManager. Excludes configurable UI regions
    /// (e.g., fire analog area) so normal controls aren't interrupted.
    /// </summary>
    public class GlobalTapJump : MonoBehaviour
    {
        [Header("Input")]
        [SerializeField] private GameInputManager gameInputManager;

        [Header("Tap Detection")]
        [Tooltip("Maximum duration for a tap (seconds). Longer presses won't trigger jump.")]
        [SerializeField] private float maxTapDuration = 0.2f;

        [Tooltip("Maximum movement distance (screen pixels) to still count as a tap.")]
        [SerializeField] private float maxTapMovement = 30f;

        [Header("Exclusion Zones")]
        [Tooltip("Don't trigger jump if tap starts in these UI regions (e.g., fire analog area).")]
        [SerializeField] private List<RectTransform> excludedRegions = new List<RectTransform>();

        [Tooltip("Automatically detect and exclude TouchRegionAnalog components.")]
        [SerializeField] private bool autoExcludeTouchRegions = false;

        [Header("Debug")]
        [SerializeField] private bool debugMode = false;

        private readonly Dictionary<int, TouchData> activeTouches = new Dictionary<int, TouchData>();
        private Canvas parentCanvas;

        private class TouchData
        {
            public float startTime;
            public Vector2 startPosition;
            public bool isInExcludedRegion;
        }

        private void Awake()
        {
            parentCanvas = GetComponentInParent<Canvas>();
            if (parentCanvas == null)
                parentCanvas = FindFirstObjectByType<Canvas>();

            if (autoExcludeTouchRegions)
            {
                foreach (var region in FindObjectsByType<TouchRegionAnalog>(FindObjectsSortMode.None))
                {
                    RectTransform rect = region.GetComponent<RectTransform>();
                    if (rect != null && !excludedRegions.Contains(rect))
                    {
                        excludedRegions.Add(rect);
                        if (debugMode)
                            Debug.Log($"[GlobalTapJump] Auto-excluded region: {region.gameObject.name}");
                    }
                }
            }
        }

        private void Update()
        {
            if (UnityEngine.Input.touchCount > 0)
            {
                foreach (Touch touch in UnityEngine.Input.touches)
                    ProcessPointerInput(touch.fingerId, touch.position, touch.phase);
            }
            else if (UnityEngine.Input.GetMouseButtonDown(0) || UnityEngine.Input.GetMouseButtonUp(0))
            {
                TouchPhase phase = UnityEngine.Input.GetMouseButtonDown(0) ? TouchPhase.Began : TouchPhase.Ended;
                ProcessPointerInput(0, UnityEngine.Input.mousePosition, phase);
            }
        }

        private void ProcessPointerInput(int pointerId, Vector2 position, TouchPhase phase)
        {
            switch (phase)
            {
                case TouchPhase.Began:
                    activeTouches[pointerId] = new TouchData
                    {
                        startTime = Time.time,
                        startPosition = position,
                        isInExcludedRegion = IsPositionInExcludedRegion(position)
                    };
                    break;

                case TouchPhase.Ended:
                    if (!activeTouches.TryGetValue(pointerId, out TouchData data))
                        return;

                    float duration = Time.time - data.startTime;
                    float movement = Vector2.Distance(position, data.startPosition);
                    bool isTap = duration <= maxTapDuration && movement <= maxTapMovement;

                    if (isTap && !data.isInExcludedRegion)
                    {
                        gameInputManager.RequestJump();
                        if (debugMode)
                            Debug.Log("[GlobalTapJump] Jump triggered!");
                    }

                    activeTouches.Remove(pointerId);
                    break;

                case TouchPhase.Canceled:
                    activeTouches.Remove(pointerId);
                    break;
            }
        }

        private bool IsPositionInExcludedRegion(Vector2 screenPosition)
        {
            Camera cam = parentCanvas != null && parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay
                ? null : Camera.main;

            foreach (RectTransform rect in excludedRegions)
            {
                if (rect == null || !rect.gameObject.activeInHierarchy)
                    continue;

                if (RectTransformUtility.RectangleContainsScreenPoint(rect, screenPosition, cam))
                    return true;
            }

            return false;
        }

        public void AddExcludedRegion(RectTransform region)
        {
            if (!excludedRegions.Contains(region))
                excludedRegions.Add(region);
        }

        public void RemoveExcludedRegion(RectTransform region)
        {
            excludedRegions.Remove(region);
        }
    }
}
