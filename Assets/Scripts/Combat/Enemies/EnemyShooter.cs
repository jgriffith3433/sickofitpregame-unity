using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using SickOfIt.Combat.Damage;

namespace SickOfIt.Combat.Enemies
{
    /// <summary>
    /// Enemy that shoots projectiles at the player
    /// </summary>
    public class EnemyShooter : EnemyBase
    {
        [Header("Shooter Settings")]
        [SerializeField] private string projectileAddress = "Projectile_Enemy";
        [SerializeField] private float projectileSpeed = 5f;
        [SerializeField] private Transform leftFirePoint;
        [SerializeField] private Transform rightFirePoint;
        [SerializeField] private bool flipToFacePlayer = true;

        private GameObject cachedProjectilePrefab;
        private bool isProjectileReady = false;

        protected override void Awake()
        {
            base.Awake();

            // Shooter enemies should not move
            rb.constraints = RigidbodyConstraints2D.FreezeAll;

            // Load projectile prefab
            LoadProjectilePrefab();
        }

        private void LoadProjectilePrefab()
        {
            Addressables.LoadAssetAsync<GameObject>(projectileAddress).Completed += (AsyncOperationHandle<GameObject> handle) =>
            {
                if (handle.Status == AsyncOperationStatus.Succeeded)
                {
                    cachedProjectilePrefab = handle.Result;
                    isProjectileReady = true;
                }
                else
                {
                    Debug.LogError($"Failed to load projectile: {projectileAddress}");
                }
            };
        }

        protected override void UpdateBehavior()
        {
            if (player == null)
                return;

            if (flipToFacePlayer)
            {
                // Flip sprite to face player
                if (player.position.x < transform.position.x)
                    spriteRenderer.flipX = true;
                else
                    spriteRenderer.flipX = false;
            }
        }

        private void Shoot()
        {
            // Choose fire point based on facing direction
            Transform firePoint = spriteRenderer.flipX ? leftFirePoint : rightFirePoint;
            
            if (cachedProjectilePrefab == null || firePoint == null)
                return;

            // Calculate direction to player
            Vector2 direction = (player.position - firePoint.position).normalized;

            // Spawn projectile
            GameObject projectile = Instantiate(cachedProjectilePrefab, firePoint.position, Quaternion.identity);

            // Set projectile velocity
            Rigidbody2D projectileRb = projectile.GetComponent<Rigidbody2D>();
            if (projectileRb != null)
            {
                projectileRb.linearVelocity = direction * projectileSpeed;
            }

            // Make sure it can damage player
            DamageDealer damageDealer = projectile.GetComponent<DamageDealer>();
            if (damageDealer != null)
            {
                damageDealer.SetTargetTags("Player");
            }

            // Auto-destroy after a while
            Destroy(projectile, 5f);
        }

        /// <summary>
        /// Public method to be called by animation events (via AnimationEventRelay)
        /// </summary>
        /// <param name="spawnPoint">Optional spawn location. If null, uses the default firePoint.</param>
        public void FireProjectile()
        {
            if (isProjectileReady)
            {
                Shoot();
            }
        }
    }
}
