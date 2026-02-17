using UnityEngine;

namespace SickOfIt.Core.Progression
{
    public class LevelWinTrigger : MonoBehaviour
    {
        private LevelManager _levelManager;

        private void Awake()
        {
            // Find LevelManager in the scene
            _levelManager = FindFirstObjectByType<LevelManager>();

            if (_levelManager == null)
            {
                Debug.LogError("LevelWinTrigger: No LevelManager found in scene!");
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.CompareTag("Player") && _levelManager != null)
            {
                _levelManager.CompleteLevel();
            }
        }
    }
}
