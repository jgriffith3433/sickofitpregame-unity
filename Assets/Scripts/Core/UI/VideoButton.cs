using UnityEngine;
using UnityEngine.UI;
using MobileControllerFramework;
using TMPro;

namespace SickOfIt.Core.UI
{
    /// <summary>
    /// Component for individual video selection buttons using MobileControllerFramework
    /// </summary>
    public class VideoButton : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private MobileControllerFramework.Button button;
        [SerializeField] private TextMeshProUGUI videoNameText;
        [SerializeField] private GameObject lockedIndicator; // Lock icon, etc.
        [SerializeField] private Image thumbnailImage; // Optional: Video thumbnail

        [Header("Visual Feedback (Optional)")]
        [SerializeField] private CanvasGroup canvasGroup; // For opacity control on locked buttons
        [SerializeField] private float lockedAlpha = 0.5f;
        [SerializeField] private float unlockedAlpha = 1f;

        private int videoNumber;
        private bool isUnlocked;

        private void Awake()
        {
            if (button == null)
                button = GetComponent<MobileControllerFramework.Button>();

            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>();
        }

        public void Setup(int video, string videoName, bool unlocked, System.Action onClickAction)
        {
            videoNumber = video;
            isUnlocked = unlocked;

            // Set video name text
            if (videoNameText != null)
            {
                videoNameText.text = unlocked ? videoName : "???";
            }

            // Show/hide locked indicator
            if (lockedIndicator != null)
            {
                lockedIndicator.SetActive(!unlocked);
            }

            // Set button interactability and event
            if (button != null)
            {
                // Use Enable()/Disable() methods from MobileControllerFramework
                if (unlocked)
                {
                    button.Enable();
                }
                else
                {
                    button.Disable();
                }

                button.TapEndEvent.RemoveAllListeners();

                if (unlocked)
                {
                    button.TapEndEvent.AddListener(() => onClickAction?.Invoke());
                }
            }

            // Set visual feedback for locked state
            if (canvasGroup != null)
            {
                canvasGroup.alpha = unlocked ? unlockedAlpha : lockedAlpha;
            }
        }
    }
}
