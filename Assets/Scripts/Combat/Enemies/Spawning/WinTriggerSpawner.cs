using UnityEngine;

namespace SickOfIt.Combat.Enemies.Spawning
{
    /// <summary>
    /// Spawns the win trigger when all enemy spawners are destroyed
    /// Add this to your level scene
    /// </summary>
    public class WinTriggerSpawner : MonoBehaviour
    {
        [Header("Win Trigger Settings")]
        [SerializeField] private GameObject winTriggerPrefab;
        [SerializeField] private Transform spawnPoint;
        [SerializeField] private bool spawnAtPlayerPosition = false;

        [Header("Spawn Effects")]
        [SerializeField] private bool playSpawnEffect = true;
        [SerializeField] private GameObject spawnEffectPrefab;

        private bool hasSpawned = false;

        /// <summary>
        /// Spawn the win trigger - called by EnemySpawner when all spawners are destroyed
        /// </summary>
        public void SpawnWinTrigger()
        {
            if (hasSpawned)
                return;

            hasSpawned = true;

            // Determine spawn position
            Vector3 spawnPos;
            if (spawnAtPlayerPosition)
            {
                var allObjects = FindObjectsOfType<GameObject>(true);
                GameObject player = System.Array.Find(allObjects, obj => obj.CompareTag("Player"));
                spawnPos = player != null ? player.transform.position : transform.position;
            }
            else if (spawnPoint != null)
            {
                spawnPos = spawnPoint.position;
            }
            else
            {
                spawnPos = transform.position;
            }

            // Spawn effect
            if (playSpawnEffect && spawnEffectPrefab != null)
            {
                Instantiate(spawnEffectPrefab, spawnPos, Quaternion.identity);
            }

            // Spawn win trigger
            if (winTriggerPrefab != null)
            {
                GameObject trigger = Instantiate(winTriggerPrefab, spawnPos, Quaternion.identity);
            }
            else
            {
                Debug.LogError("WinTriggerSpawner: No win trigger prefab assigned!");
            }
        }
    }
}
