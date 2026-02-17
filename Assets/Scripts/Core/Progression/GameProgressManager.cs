using UnityEngine;

namespace SickOfIt.Core
{
    /// <summary>
    /// Manages game progression, level completion, and video unlocks using PlayerPrefs
    /// </summary>
    public class GameProgressManager : MonoBehaviour
    {
        [Header("Game Configuration")]
        [SerializeField] private int totalLevels = 1; // Total number of levels in the game
        private static GameProgressManager _instance;
        public static GameProgressManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    GameObject go = new GameObject("GameProgressManager");
                    _instance = go.AddComponent<GameProgressManager>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        // Level Management
        public void SetLevelCompleted(int levelNumber)
        {
            PlayerPrefs.SetInt($"Level_{levelNumber}_Completed", 1);

            // Unlock the corresponding video
            UnlockVideo(levelNumber);

            // Update highest level reached
            int currentHighest = GetHighestLevelReached();
            if (levelNumber >= currentHighest)
            {
                SetHighestLevelReached(levelNumber + 1); // Next level is now accessible
            }

            PlayerPrefs.Save();
        }

        public bool IsLevelCompleted(int levelNumber)
        {
            return PlayerPrefs.GetInt($"Level_{levelNumber}_Completed", 0) == 1;
        }

        public int GetHighestLevelReached()
        {
            return PlayerPrefs.GetInt("HighestLevelReached", 1); // Start at level 1
        }

        public void SetHighestLevelReached(int levelNumber)
        {
            int current = GetHighestLevelReached();
            if (levelNumber > current)
            {
                PlayerPrefs.SetInt("HighestLevelReached", levelNumber);
                PlayerPrefs.Save();
            }
        }

        // Video Management
        public void UnlockVideo(int videoNumber)
        {
            PlayerPrefs.SetInt($"Video_{videoNumber}_Unlocked", 1);
            PlayerPrefs.Save();
        }

        public bool IsVideoUnlocked(int videoNumber)
        {
            // Video 1 (intro) is always unlocked
            if (videoNumber == 1)
                return true;

            return PlayerPrefs.GetInt($"Video_{videoNumber}_Unlocked", 0) == 1;
        }

        // Current Progress
        public int GetCurrentLevel()
        {
            // The current level is the highest level that hasn't been completed yet
            int highestReached = GetHighestLevelReached();
            return highestReached;
        }

        public int GetTotalLevels()
        {
            return totalLevels;
        }

        public bool HasBeatenGame()
        {
            // Player has beaten the game if their current level exceeds total levels
            return GetCurrentLevel() > totalLevels;
        }

        // Reset Progress (useful for testing)
        public void ResetAllProgress()
        {
            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();
        }

        // Initialize first time player
        public void InitializeNewPlayer()
        {
            if (!PlayerPrefs.HasKey("GameInitialized"))
            {
                PlayerPrefs.SetInt("GameInitialized", 1);
                PlayerPrefs.SetInt("HighestLevelReached", 1);
                UnlockVideo(1); // Unlock intro video
                PlayerPrefs.Save();
            }
        }
    }
}
