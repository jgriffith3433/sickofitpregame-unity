using UnityEngine;
#if UNITY_WEBGL && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

namespace MobileControllerFramework
{
    /// <summary>
    /// Automatically hides this GameObject on non-mobile platforms.
    /// Add this component to any mobile UI element to hide it on PC/WebGL.
    /// </summary>
    public class HideOnPC : MonoBehaviour
    {
        [Header("Debug")]
        [Tooltip("Force show this object even on PC (for testing in editor)")]
        public bool forceShow = false;

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern bool IsMobileBrowser();
#endif

        private bool IsMobileDevice()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            // For WebGL, use JavaScript to check user agent
            return IsMobileBrowser();
#else
            // For native builds, check the platform
            return Application.platform == RuntimePlatform.Android ||
                   Application.platform == RuntimePlatform.IPhonePlayer;
#endif
        }

        private void Awake()
        {
            if (forceShow)
            {
                // Debug mode - keep object visible
                return;
            }

            // Check if we're on a mobile device
            bool isMobile = IsMobileDevice();

            // If not mobile, deactivate this GameObject
            if (!isMobile)
            {
                gameObject.SetActive(false);
            }
        }
    }
}
