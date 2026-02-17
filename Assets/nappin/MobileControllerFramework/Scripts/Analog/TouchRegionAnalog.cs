using UnityEngine;
using UnityEngine.EventSystems;

namespace MobileControllerFramework
{
    /// <summary>
    /// Defines a screen region where touches will spawn and control an analog stick.
    /// The analog appears at the touch point and can be dragged from there.
    /// Perfect for mobile games where you want touch-anywhere controls on left/right sides of screen.
    /// </summary>
    public class TouchRegionAnalog : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        [Header("References")]
        [Tooltip("The Analog component to control. It will be positioned at touch point.")]
        [SerializeField] private Analog analogController;
        
        [Header("Behavior")]
        [Tooltip("Show the analog visual when touched, hide when released.")]
        [SerializeField] private bool showOnTouch = true;
        
        [Tooltip("Fade analog visual instead of instant show/hide.")]
        [SerializeField] private bool fadeAnalog = true;
        
        [Tooltip("Fade duration in seconds.")]
        [SerializeField] private float fadeDuration = 0.15f;
        
        [Header("Touch Tracking")]
        [Tooltip("Only respond to the first touch in this region (prevents multi-touch conflicts).")]
        [SerializeField] private bool singleTouchMode = true;
        
        private RectTransform analogRect;
        private CanvasGroup analogCanvasGroup;
        private Canvas parentCanvas;
        
        private bool isTouching = false;
        private int currentPointerId = -1;
        private Vector2 touchStartPosition;
        private float currentAlpha = 0f;
        private float targetAlpha = 0f;
        
        private void Awake()
        {
            if (analogController == null)
            {
                Debug.LogError($"[TouchRegionAnalog] Analog component not assigned on {gameObject.name}!");
                enabled = false;
                return;
            }
            
            analogRect = analogController.GetComponent<RectTransform>();
            parentCanvas = analogController.GetComponentInParent<Canvas>();
            
            // Setup canvas group for fading
            analogCanvasGroup = analogController.GetComponent<CanvasGroup>();
            if (analogCanvasGroup == null && fadeAnalog)
            {
                analogCanvasGroup = analogController.gameObject.AddComponent<CanvasGroup>();
            }
            
            // Start hidden if showOnTouch is enabled
            if (showOnTouch)
            {
                if (fadeAnalog && analogCanvasGroup != null)
                {
                    analogCanvasGroup.alpha = 0f;
                    analogCanvasGroup.blocksRaycasts = false; // Don't block panel raycasts initially
                    currentAlpha = 0f;
                    targetAlpha = 0f;
                }
                else
                {
                    analogController.gameObject.SetActive(false);
                }
            }
            else
            {
                // If always visible, ensure it can receive events
                if (analogCanvasGroup != null)
                {
                    analogCanvasGroup.alpha = 1f;
                    analogCanvasGroup.blocksRaycasts = true;
                }
            }
        }
        
        private void Update()
        {
            // Handle fading
            if (fadeAnalog && analogCanvasGroup != null && !Mathf.Approximately(currentAlpha, targetAlpha))
            {
                currentAlpha = Mathf.MoveTowards(currentAlpha, targetAlpha, Time.deltaTime / fadeDuration);
                analogCanvasGroup.alpha = currentAlpha;
            }
        }
        
        public void OnPointerDown(PointerEventData eventData)
        {
            // In single touch mode, ignore if already touching
            if (singleTouchMode && isTouching)
                return;
            
            isTouching = true;
            currentPointerId = eventData.pointerId;
            touchStartPosition = eventData.position;
            
            // Position analog at touch point
            PositionAnalogAtScreenPoint(eventData.position);
            
            // Show analog
            if (showOnTouch)
            {
                if (fadeAnalog && analogCanvasGroup != null)
                {
                    targetAlpha = 1f;
                    analogCanvasGroup.blocksRaycasts = true;
                }
                else
                {
                    analogController.gameObject.SetActive(true);
                }
            }
            
            // Forward event to analog
            analogController.OnPointerDown(eventData);
        }
        
        public void OnDrag(PointerEventData eventData)
        {
            // Only respond to the touch that started in this region
            if (singleTouchMode && eventData.pointerId != currentPointerId)
                return;
            
            if (!isTouching)
                return;
            
            // Forward drag to analog
            analogController.OnDrag(eventData);
        }
        
        public void OnPointerUp(PointerEventData eventData)
        {
            // Only respond to the touch that started in this region
            if (singleTouchMode && eventData.pointerId != currentPointerId)
                return;
            
            if (!isTouching)
                return;
            
            isTouching = false;
            currentPointerId = -1;
            
            // Forward event to analog
            analogController.OnPointerUp(eventData);
            
            // Hide analog
            if (showOnTouch)
            {
                if (fadeAnalog && analogCanvasGroup != null)
                {
                    targetAlpha = 0f;
                    analogCanvasGroup.blocksRaycasts = false;
                }
                else
                {
                    analogController.gameObject.SetActive(false);
                }
            }
        }
        
        private void PositionAnalogAtScreenPoint(Vector2 screenPosition)
        {
            if (analogRect == null)
                return;
            
            // Get the parent RectTransform (should be this touch region for proper coordinates)
            RectTransform parentRect = analogRect.parent as RectTransform;
            if (parentRect == null)
            {
                Debug.LogWarning("[TouchRegionAnalog] Analog must be a child of the TouchRegion for positioning to work correctly!");
                return;
            }
            
            // Convert screen position to local position within the parent (this region)
            Vector2 localPoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parentRect,
                screenPosition,
                parentCanvas != null && parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : Camera.main,
                out localPoint
            );
            
            // Position the analog at this point
            analogRect.anchoredPosition = localPoint;
        }
        
        /// <summary>
        /// Get the current analog stick position (normalized -1 to 1).
        /// </summary>
        public Vector2 GetStickPosition()
        {
            return analogController.GetStickPosition();
        }
        
        /// <summary>
        /// Check if this region is currently being touched.
        /// </summary>
        public bool IsTouching()
        {
            return isTouching;
        }
    }
}
