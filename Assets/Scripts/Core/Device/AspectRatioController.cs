using UnityEngine;

namespace SickOfIt.Core.Device
{
    /// <summary>
    /// Manages camera viewport to maintain a target aspect ratio (e.g., 16:9)
    /// by adding letterboxing (black bars on top/bottom) or pillarboxing (black bars on sides)
    /// when the screen aspect ratio differs from the target.
    /// </summary>
    public class AspectRatioController : MonoBehaviour
    {
        [Header("Target Aspect Ratio")]
        [Tooltip("Target width of aspect ratio (e.g., 16 for 16:9)")]
        public float targetAspectWidth = 16f;

        [Tooltip("Target height of aspect ratio (e.g., 9 for 16:9)")]
        public float targetAspectHeight = 9f;

        [Header("Letterbox Settings")]
        [Tooltip("Color of the letterbox bars")]
        public Color letterboxColor = Color.black;

        [Header("Fit Mode")]
        [Tooltip("Fit Inside: Letterbox to fit entire game (recommended). Fit Outside: Crop edges to fill screen.")]
        public FitMode fitMode = FitMode.FitInside;

        public enum FitMode
        {
            FitInside,  // Letterbox/pillarbox to show all content
            FitOutside  // Crop edges to fill the screen
        }

        private Camera cam;
        private float lastScreenWidth;
        private float lastScreenHeight;

        private void Awake()
        {
            cam = GetComponent<Camera>();
            if (cam == null)
            {
                Debug.LogError("AspectRatioController: No Camera component found!");
                enabled = false;
                return;
            }

            UpdateCameraRect();
        }

        private void Update()
        {
            // Check if screen size changed (e.g., orientation change on mobile)
            if (Screen.width != lastScreenWidth || Screen.height != lastScreenHeight)
            {
                UpdateCameraRect();
            }
        }

        private void UpdateCameraRect()
        {
            lastScreenWidth = Screen.width;
            lastScreenHeight = Screen.height;

            // Calculate target aspect ratio
            float targetAspect = targetAspectWidth / targetAspectHeight;

            // Calculate current screen aspect ratio
            float screenAspect = (float)Screen.width / Screen.height;

            if (fitMode == FitMode.FitInside)
            {
                FitInsideScreen(targetAspect, screenAspect);
            }
            else
            {
                FitOutsideScreen(targetAspect, screenAspect);
            }

            // Set background color for letterbox bars
            cam.backgroundColor = letterboxColor;
        }

        private void FitInsideScreen(float targetAspect, float screenAspect)
        {
            if (screenAspect > targetAspect)
            {
                // Screen is wider than target: add pillarboxing (vertical bars on sides)
                float viewportWidth = targetAspect / screenAspect;
                float viewportX = (1f - viewportWidth) / 2f;

                cam.rect = new Rect(viewportX, 0f, viewportWidth, 1f);

                // Pillarboxing applied
            }
            else if (screenAspect < targetAspect)
            {
                // Screen is taller than target: add letterboxing (horizontal bars on top/bottom)
                float viewportHeight = screenAspect / targetAspect;
                float viewportY = (1f - viewportHeight) / 2f;

                cam.rect = new Rect(0f, viewportY, 1f, viewportHeight);

                // Letterboxing applied
            }
            else
            {
                // Perfect match
                cam.rect = new Rect(0f, 0f, 1f, 1f);

                // Perfect aspect ratio match
            }
        }

        private void FitOutsideScreen(float targetAspect, float screenAspect)
        {
            // This mode crops the edges to fill the screen completely
            // Useful if you have extra space in your game design that can be safely cropped

            if (screenAspect > targetAspect)
            {
                // Screen is wider: show more width, maintain height
                float orthoSizeMultiplier = screenAspect / targetAspect;
                // You would adjust orthographic size here for 2D cameras
                // cam.orthographicSize *= orthoSizeMultiplier;
            }
            else if (screenAspect < targetAspect)
            {
                // Screen is taller: show more height, maintain width
                float orthoSizeMultiplier = targetAspect / screenAspect;
                // cam.orthographicSize *= orthoSizeMultiplier;
            }

            // For FitOutside, always use full screen
            cam.rect = new Rect(0f, 0f, 1f, 1f);

            Debug.LogWarning("AspectRatioController: FitOutside mode - edges may be cropped on different aspect ratios!");
        }

        // Public method to get the safe area in world coordinates
        public Rect GetSafeAreaWorldBounds()
        {
            if (cam == null) return new Rect();

            // Get viewport corners in world space
            Vector3 bottomLeft = cam.ViewportToWorldPoint(new Vector3(0, 0, cam.nearClipPlane));
            Vector3 topRight = cam.ViewportToWorldPoint(new Vector3(1, 1, cam.nearClipPlane));

            return new Rect(bottomLeft.x, bottomLeft.y, topRight.x - bottomLeft.x, topRight.y - bottomLeft.y);
        }

        private void OnValidate()
        {
            // Update in editor when values change
            if (Application.isPlaying && cam != null)
            {
                UpdateCameraRect();
            }
        }
    }
}
