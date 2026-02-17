using UnityEngine;
using System.Collections.Generic;

namespace SickOfIt.Combat.Enemies.Spawning
{
    [System.Serializable]
    public class SpawnWave
    {
        [Tooltip("Enemy prefabs that can spawn in this wave")]
        public List<GameObject> enemyPrefabs = new List<GameObject>();
        
        [Tooltip("Total enemies to spawn in this wave (-1 for infinite)")]
        public int enemiesToSpawn = 10;
        
        [Tooltip("Time between spawns in this wave")]
        public float spawnInterval = 3f;
        
        [Tooltip("Delay before this wave starts")]
        public float delayBeforeStart = 0f;
    }

    /// <summary>
    /// Spawns enemies periodically until destroyed with wave-based spawning
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class EnemySpawner : MonoBehaviour
    {
        [Header("Spawn Zone")]
        [SerializeField] private Vector2 spawnBoundsMin = new Vector2(-2f, -2f);
        [SerializeField] private Vector2 spawnBoundsMax = new Vector2(2f, 2f);
        
        [Header("Spawn Settings")]
        [SerializeField] private int maxEnemies = 5; // Max alive at once
        
        [Header("Wave Configuration")]
        [SerializeField] private List<SpawnWave> waves = new List<SpawnWave>();
        [SerializeField] private bool loopWaves = false;

        [Header("Spawner Info")]
        [SerializeField] private int currentWaveIndex = 0;
        [SerializeField] private int enemiesSpawnedInWave = 0;
        [SerializeField] private int totalEnemiesSpawned = 0;
        [SerializeField] private List<GameObject> aliveEnemies = new List<GameObject>();

        private float nextSpawnTime = 0f;
        private float waveStartTime = 0f;
        private bool waveStarted = false;
        private bool isActive = true;
        private bool isKilled = false;

        // Static tracking for win condition
        private static List<EnemySpawner> allSpawners = new List<EnemySpawner>();

        private void Awake()
        {
            allSpawners.Add(this);
        }

        private void OnEnable()
        {
            // No longer using HealthSystem events
        }

        private void OnDisable()
        {
            // No longer using HealthSystem events
        }

        private void OnDestroy()
        {
            allSpawners.Remove(this);
        }

        private void Update()
        {
            if (!isActive || isKilled || waves.Count == 0)
                return;

            // Clean up dead enemies from list
            aliveEnemies.RemoveAll(enemy => enemy == null);

            // Check if current wave is valid
            if (currentWaveIndex >= waves.Count)
            {
                if (loopWaves)
                {
                    currentWaveIndex = 0;
                    enemiesSpawnedInWave = 0;
                    waveStarted = false;
                }
                else
                {
                    return; // All waves complete
                }
            }

            SpawnWave currentWave = waves[currentWaveIndex];
            
            // Check if wave has valid prefabs
            if (currentWave.enemyPrefabs == null || currentWave.enemyPrefabs.Count == 0)
            {
                currentWaveIndex++;
                waveStarted = false;
                return;
            }

            // Start wave after delay
            if (!waveStarted)
            {
                waveStartTime = Time.time + currentWave.delayBeforeStart;
                waveStarted = true;
                nextSpawnTime = waveStartTime;
            }

            // Wait for wave delay
            if (Time.time < waveStartTime)
                return;

            // Check if wave is complete
            if (currentWave.enemiesToSpawn >= 0 && enemiesSpawnedInWave >= currentWave.enemiesToSpawn)
            {
                currentWaveIndex++;
                enemiesSpawnedInWave = 0;
                waveStarted = false;
                return;
            }

            // Spawn new enemy if needed
            if (Time.time >= nextSpawnTime && aliveEnemies.Count < maxEnemies)
            {
                SpawnEnemy();
                nextSpawnTime = Time.time + currentWave.spawnInterval;
            }
        }

        private void SpawnEnemy()
        {
            if (currentWaveIndex >= waves.Count)
                return;

            SpawnWave currentWave = waves[currentWaveIndex];
            if (currentWave.enemyPrefabs == null || currentWave.enemyPrefabs.Count == 0)
                return;

            // Pick random prefab from current wave
            GameObject prefabToSpawn = currentWave.enemyPrefabs[Random.Range(0, currentWave.enemyPrefabs.Count)];
            if (prefabToSpawn == null)
                return;

            // Random position within spawn bounds
            float randomX = Random.Range(spawnBoundsMin.x, spawnBoundsMax.x);
            float randomY = Random.Range(spawnBoundsMin.y, spawnBoundsMax.y);
            Vector3 spawnPosition = transform.position + new Vector3(randomX, randomY, 0);

            // Spawn enemy
            GameObject enemy = Instantiate(prefabToSpawn, spawnPosition, Quaternion.identity);
            aliveEnemies.Add(enemy);
            enemiesSpawnedInWave++;
            totalEnemiesSpawned++;
        }

        /// <summary>
        /// Manually kill this spawner - called by LevelManager
        /// </summary>
        public void KillSpawner()
        {
            if (isKilled)
                return;

            isKilled = true;
            isActive = false;

            // Clean up all spawned enemies
            foreach (GameObject enemy in aliveEnemies)
            {
                if (enemy != null)
                {
                    Destroy(enemy);
                }
            }
            aliveEnemies.Clear();

            // Check if all spawners are killed
            CheckWinCondition();
        }

        private void OnSpawnerDestroyed()
        {
            // Legacy method - kept for compatibility
            KillSpawner();
        }

        private void CheckWinCondition()
        {
            // Check if all spawners are killed
            bool allKilled = true;
            foreach (var spawner in allSpawners)
            {
                if (spawner != null && !spawner.isKilled)
                {
                    allKilled = false;
                    break;
                }
            }

            if (allKilled)
            {
                // Spawn win trigger
                WinTriggerSpawner winSpawner = FindFirstObjectByType<WinTriggerSpawner>();
                if (winSpawner != null)
                {
                    winSpawner.SpawnWinTrigger();
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            // Draw spawn bounds
            Gizmos.color = Color.red;
            Vector3 center = transform.position;
            Vector3 size = new Vector3(
                spawnBoundsMax.x - spawnBoundsMin.x,
                spawnBoundsMax.y - spawnBoundsMin.y,
                0.1f
            );
            Vector3 boundsCenter = center + new Vector3(
                (spawnBoundsMin.x + spawnBoundsMax.x) / 2f,
                (spawnBoundsMin.y + spawnBoundsMax.y) / 2f,
                0
            );
            Gizmos.DrawWireCube(boundsCenter, size);
        }

        /// <summary>
        /// Get count of all active spawners in the level
        /// </summary>
        public static int GetActiveSpawnerCount()
        {
            int count = 0;
            foreach (var spawner in allSpawners)
            {
                if (spawner != null && !spawner.isKilled)
                {
                    count++;
                }
            }
            return count;
        }
    }
}
