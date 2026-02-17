using UnityEngine;

namespace SickOfIt.Core.Input
{
    /// <summary>
    /// Centralized input event bus. Input detectors (tap jump, buttons, etc.)
    /// fire events here; consumers (TopDownController, etc.) subscribe.
    /// Place on a GameObject in your scene and wire references in the Inspector.
    /// </summary>
    public class GameInputManager : MonoBehaviour
    {
        /// <summary>Raised when any jump input source detects a jump request.</summary>
        public event System.Action OnJumpPressed;

        /// <summary>
        /// Call this from any input detector (GlobalTapJump, UI buttons, etc.)
        /// to broadcast a jump request to all subscribers.
        /// </summary>
        public void RequestJump()
        {
            OnJumpPressed?.Invoke();
        }
    }
}
