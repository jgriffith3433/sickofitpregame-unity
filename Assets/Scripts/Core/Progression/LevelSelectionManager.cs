using UnityEngine;
using MobileControllerFramework;
using SickOfIt.Core.Flow;
using SickOfIt.Core.UI;
using SickOfIt.Data;

namespace SickOfIt.Core.Progression
{
    /// <summary>
    /// Manages the Level Selection scene. Dynamically creates or updates level buttons.
    /// </summary>
    public class LevelSelectionManager : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private int totalLevels = 1; // Set this to your total number of levels

        [Header("UI References")]
        [SerializeField] private Transform levelButtonContainer; // Parent object for level buttons
        [SerializeField] private GameObject levelButtonPrefab; // Prefab for level buttons
        [SerializeField] private MobileControllerFramework.Button backButton;

        [Header("Scene Loading")]
        [SerializeField] private SceneLoadingManager sceneLoadingManager;

        private void Start()
        {
            GenerateLevelButtons();

            if (backButton != null)
                backButton.TapEndEvent.AddListener(OnBackClicked);
        }

        private void GenerateLevelButtons()
        {
            if (levelButtonContainer == null || levelButtonPrefab == null)
            {
                Debug.LogError("LevelSelectionManager: Missing references! Please assign levelButtonContainer and levelButtonPrefab in the inspector.");
                return;
            }

            // Clear existing buttons
            foreach (Transform child in levelButtonContainer)
            {
                Destroy(child.gameObject);
            }

            int highestReached = GameProgressManager.Instance.GetHighestLevelReached();

            // Create buttons for each level
            for (int i = 1; i <= totalLevels; i++)
            {
                int levelNumber = i; // Capture for lambda

                GameObject buttonObj = Instantiate(levelButtonPrefab, levelButtonContainer);
                LevelButton levelButton = buttonObj.GetComponent<LevelButton>();

                if (levelButton != null)
                {
                    bool isCompleted = GameProgressManager.Instance.IsLevelCompleted(levelNumber);
                    bool isUnlocked = levelNumber <= highestReached;

                    levelButton.Setup(levelNumber, isCompleted, isUnlocked, () => OnLevelClicked(levelNumber));
                }
            }
        }

        private void OnLevelClicked(int levelNumber)
        {
            // For Level 1, check if it's the first time visiting
            if (levelNumber == 1 && !HasVisitedLevel(1))
            {
                // Mark as visited and play Video1 first
                MarkLevelAsVisited(1);
                PlayerPrefs.SetInt("ReturnToLevel", 1);
                PlayerPrefs.Save();

                if (sceneLoadingManager != null)
                    sceneLoadingManager.LoadVideo(1);
                else
                    SceneTransitionManager.Instance.LoadVideo(1);
            }
            else
            {
                if (sceneLoadingManager != null)
                    sceneLoadingManager.LoadLevel(levelNumber);
                else
                    SceneTransitionManager.Instance.LoadLevel(levelNumber);
            }
        }

        private bool HasVisitedLevel(int levelNumber)
        {
            return PlayerPrefs.GetInt($"Level_{levelNumber}_Visited", 0) == 1;
        }

        private void MarkLevelAsVisited(int levelNumber)
        {
            PlayerPrefs.SetInt($"Level_{levelNumber}_Visited", 1);
            PlayerPrefs.Save();
        }

        private void OnBackClicked()
        {
            SceneTransitionManager.Instance.LoadMainMenu();
        }
    }
}
