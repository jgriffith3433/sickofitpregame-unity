using UnityEngine;
using UnityEngine.EventSystems;
using MobileControllerFramework;
using System.Collections.Generic;

namespace SickOfIt.Core.Input
{
    /// <summary>
    /// Extends mobile touch input to the letterboxed/pillarboxed regions of the screen.
    /// 
    /// When AspectRatioController shrinks the camera viewport to maintain 16:9,
    /// the resulting black bars (letterbox/pillarbox) fall outside the UI canvas,
    /// so EventSystem-based controls (TouchRegionAnalog) can't receive touches there.
    /// 
    /// This component reads raw Input.touches, detects touches that begin in the
    /// letterbox area, and forwards them as synthetic PointerEventData to the
    /// appropriate TouchRegionAnalog (left half → move, right half → fire).
    /// 
    /// GlobalTapJump already reads raw Input.touches and works in letterbox areas
    /// without modification. Both systems coexist: quick taps trigger jump,
    /// tap-and-drag controls the analog stick.
    /// </summary>
    public class LetterboxInputExtender : MonoBehaviour
    {
        [Header("Analog Regions")]
        [Tooltip("Touch region for movement (left side of screen).")]
        [SerializeField] private TouchRegionAnalog moveRegion;

        [Tooltip("Touch region for firing (right side of screen).")]
        [SerializeField] private TouchRegionAnalog fireRegion;

        [Header("Settings")]
        [Tooltip("Horizontal screen ratio (0-1) dividing left/right regions. 0.5 = center of screen.")]
        [Range(0f, 1f)]
        [SerializeField] private float screenSplitRatio = 0.5f;

        [Header("Debug")]
        [SerializeField] private bool debugMode = false;

        private Camera mainCamera;
        private readonly Dictionary<int, TrackedTouch> letterboxTouches = new Dictionary<int, TrackedTouch>();

        private class TrackedTouch
        {
            public TouchRegionAnalog region;
            /// <summary>
            /// Offset from the raw screen position to the clamped viewport-edge position.
            /// Applied to all subsequent drag events so relative finger movement maps 1:1
            /// to analog stick direction, without the clamping skewing the angle.
            /// </summary>
            public Vector2 positionOffset;
        }

        private void Start()
        {
            mainCamera = Camera.main;
        }

        private void Update()
        {
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
                if (mainCamera == null) return;
            }

            if (UnityEngine.Input.touchCount > 0)
            {
                foreach (Touch touch in UnityEngine.Input.touches)
                    ProcessTouch(touch.fingerId, touch.position, touch.phase);
            }
#if UNITY_EDITOR
            else
            {
                // Mouse fallback for editor testing
                if (UnityEngine.Input.GetMouseButtonDown(0))
                    ProcessTouch(-1, UnityEngine.Input.mousePosition, TouchPhase.Began);
                else if (UnityEngine.Input.GetMouseButton(0) && letterboxTouches.ContainsKey(-1))
                    ProcessTouch(-1, UnityEngine.Input.mousePosition, TouchPhase.Moved);
                else if (UnityEngine.Input.GetMouseButtonUp(0))
                    ProcessTouch(-1, UnityEngine.Input.mousePosition, TouchPhase.Ended);
            }
#endif
        }

        private void ProcessTouch(int fingerId, Vector2 screenPos, TouchPhase phase)
        {
            switch (phase)
            {
                case TouchPhase.Began:
                    HandleTouchBegan(fingerId, screenPos);
                    break;

                case TouchPhase.Moved:
                case TouchPhase.Stationary:
                    HandleTouchMoved(fingerId, screenPos);
                    break;

                case TouchPhase.Ended:
                case TouchPhase.Canceled:
                    HandleTouchEnded(fingerId, screenPos);
                    break;
            }
        }

        private void HandleTouchBegan(int fingerId, Vector2 screenPos)
        {
            // Only handle touches that start in the letterbox area
            if (!IsInLetterboxArea(screenPos)) return;

            // Determine which analog to control based on horizontal screen position
            bool isLeftSide = screenPos.x < Screen.width * screenSplitRatio;
            TouchRegionAnalog target = isLeftSide ? moveRegion : fireRegion;

            if (target == null || !target.gameObject.activeInHierarchy) return;

            // Clamp to just inside the viewport edge so the analog visual appears
            // within the visible area (not behind the letterbox bar)
            Vector2 clampedPos = ClampToViewport(screenPos);

            // Store the offset so we can adjust drag positions to preserve
            // the user's intended direction without clamping-induced skew
            Vector2 offset = clampedPos - screenPos;

            letterboxTouches[fingerId] = new TrackedTouch
            {
                region = target,
                positionOffset = offset
            };

            var eventData = CreatePointerEventData(fingerId, clampedPos);
            target.OnPointerDown(eventData);

            if (debugMode)
                Debug.Log($"[LetterboxInput] Touch {fingerId} began in letterbox at {screenPos}, " +
                          $"forwarded to {target.name} at viewport edge {clampedPos}");
        }

        private void HandleTouchMoved(int fingerId, Vector2 screenPos)
        {
            if (!letterboxTouches.TryGetValue(fingerId, out TrackedTouch tracked)) return;

            // Apply the same offset that was used for the initial clamping.
            // This preserves relative finger movement so the analog direction
            // matches the actual drag direction without angular skew.
            Vector2 adjustedPos = screenPos + tracked.positionOffset;
            var eventData = CreatePointerEventData(fingerId, adjustedPos);
            tracked.region.OnDrag(eventData);
        }

        private void HandleTouchEnded(int fingerId, Vector2 screenPos)
        {
            if (!letterboxTouches.TryGetValue(fingerId, out TrackedTouch tracked)) return;

            Vector2 adjustedPos = screenPos + tracked.positionOffset;
            var eventData = CreatePointerEventData(fingerId, adjustedPos);
            tracked.region.OnPointerUp(eventData);
            letterboxTouches.Remove(fingerId);

            if (debugMode)
                Debug.Log($"[LetterboxInput] Touch {fingerId} ended, released from {tracked.region.name}");
        }

        /// <summary>
        /// Returns true if the screen position falls outside the camera's rendered viewport
        /// (i.e., in the letterbox or pillarbox bars).
        /// </summary>
        private bool IsInLetterboxArea(Vector2 screenPos)
        {
            Rect viewport = mainCamera.pixelRect;
            return !viewport.Contains(screenPos);
        }

        /// <summary>
        /// Clamps a screen position to just inside the camera viewport edge.
        /// The 1-pixel inset ensures the point is clearly within the rendered area
        /// so ScreenPointToLocalPointInRectangle conversions work correctly.
        /// </summary>
        private Vector2 ClampToViewport(Vector2 screenPos)
        {
            Rect vp = mainCamera.pixelRect;
            return new Vector2(
                Mathf.Clamp(screenPos.x, vp.xMin + 1f, vp.xMax - 1f),
                Mathf.Clamp(screenPos.y, vp.yMin + 1f, vp.yMax - 1f)
            );
        }

        private PointerEventData CreatePointerEventData(int pointerId, Vector2 position)
        {
            return new PointerEventData(EventSystem.current)
            {
                pointerId = pointerId,
                position = position,
                pressPosition = position
            };
        }
    }
}
