using UnityEngine;
using MobileControllerFramework;
using TMPro;

namespace SickOfIt.Core.UI
{
    /// <summary>
    /// Component for individual level selection buttons using MobileControllerFramework
    /// </summary>
    public class LevelButton : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Button button;
        [SerializeField] private TextMeshProUGUI levelText;
        [SerializeField] private GameObject completedIndicator; // Star, checkmark, etc.
        [SerializeField] private GameObject lockedIndicator; // Lock icon, etc.

        [Header("Visual Feedback (Optional)")]
        [SerializeField] private CanvasGroup canvasGroup; // For opacity control on locked buttons
        [SerializeField] private float lockedAlpha = 0.5f;
        [SerializeField] private float unlockedAlpha = 1f;

        private int levelNumber;
        private bool isUnlocked;

        private void Awake()
        {
            if (button == null)
                button = GetComponent<Button>();

            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>();
        }

        public void Setup(int level, bool completed, bool unlocked, System.Action onClickAction)
        {
            levelNumber = level;
            isUnlocked = unlocked;

            // Set level text
            if (levelText != null)
            {
                levelText.text = levelNumber.ToString();
            }

            // Show/hide completed indicator
            if (completedIndicator != null)
            {
                completedIndicator.SetActive(completed);
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
