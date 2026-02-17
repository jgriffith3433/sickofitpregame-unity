using UnityEngine;
using SickOfIt.Combat.Health;

namespace SickOfIt.Combat.Damage
{
    /// <summary>
    /// Deals damage on collision with objects that have HealthSystem
    /// Use this on projectiles, enemies, etc.
    /// </summary>
    public class DamageDealer : MonoBehaviour
    {
        [Header("Damage Settings")]
        [SerializeField] private int damageAmount = 1;
        [SerializeField] private bool destroyOnHit = true;

        [Header("Target Filtering")]
        [Tooltip("What tags can this damage? Default for player projectiles: Enemy")]
        [SerializeField] private string[] targetTags = { "Enemy" }; // What can this damage?
        [SerializeField] private bool canDamageMultipleTargets = false;

        private bool hasDealtDamage = false;

        private void OnTriggerEnter2D(Collider2D other)
        {
            // Skip if already dealt damage and can't damage multiple
            if (hasDealtDamage && !canDamageMultipleTargets)
                return;

            // Check if target has correct tag
            bool isValidTarget = false;
            foreach (string tag in targetTags)
            {
                if (other.CompareTag(tag))
                {
                    isValidTarget = true;
                    break;
                }
            }

            if (!isValidTarget)
                return;

            // Try to deal damage
            HealthSystem healthSystem = other.GetComponent<HealthSystem>();
            if (healthSystem != null && healthSystem.IsAlive)
            {
                healthSystem.TakeDamage(damageAmount);
                hasDealtDamage = true;

                if (destroyOnHit)
                {
                    Destroy(gameObject);
                }
            }
        }

        /// <summary>
        /// Set damage amount at runtime (useful for different enemy types)
        /// </summary>
        public void SetDamage(int damage)
        {
            damageAmount = damage;
        }

        /// <summary>
        /// Set what this can damage
        /// </summary>
        public void SetTargetTags(params string[] tags)
        {
            targetTags = tags;
        }
    }
}
