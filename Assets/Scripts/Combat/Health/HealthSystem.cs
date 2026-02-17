using UnityEngine;
using UnityEngine.Events;

namespace SickOfIt.Combat.Health
{
    /// <summary>
    /// Generic health system that can be used by player, enemies, or spawners
    /// </summary>
    public class HealthSystem : MonoBehaviour
    {
        [Header("Health Settings")]
        [SerializeField] private int maxHealth = 3;
        [SerializeField] private int currentHealth;

        [Header("Death Settings")]
        [SerializeField] private bool destroyOnDeath = true;
        [SerializeField] private float deathDelay = 0f;

        [Header("Invincibility")]
        [SerializeField] private bool hasInvincibilityFrames = false;
        [SerializeField] private float invincibilityDuration = 0.5f;
        private float invincibilityTimer = 0f;

        [Header("Events")]
        public UnityEvent<int> onHealthChanged; // Passes current health
        public UnityEvent onDamaged;
        public UnityEvent onHealed;
        public UnityEvent onDeath;

        public int CurrentHealth => currentHealth;
        public int MaxHealth => maxHealth;
        public bool IsAlive => currentHealth > 0;
        public bool IsInvincible => invincibilityTimer > 0;

        private void Awake()
        {
            currentHealth = maxHealth;
        }

        private void Update()
        {
            if (invincibilityTimer > 0)
            {
                invincibilityTimer -= Time.deltaTime;
            }
        }

        /// <summary>
        /// Take damage
        /// </summary>
        public void TakeDamage(int damage)
        {
            if (!IsAlive || IsInvincible)
                return;

            currentHealth = Mathf.Max(0, currentHealth - damage);

            onDamaged?.Invoke();
            onHealthChanged?.Invoke(currentHealth);

            if (hasInvincibilityFrames)
            {
                invincibilityTimer = invincibilityDuration;
            }

            if (currentHealth <= 0)
            {
                Die();
            }
        }

        /// <summary>
        /// Heal
        /// </summary>
        public void Heal(int amount)
        {
            if (!IsAlive)
                return;

            int oldHealth = currentHealth;
            currentHealth = Mathf.Min(maxHealth, currentHealth + amount);

            if (currentHealth > oldHealth)
            {
                onHealed?.Invoke();
                onHealthChanged?.Invoke(currentHealth);
            }
        }

        /// <summary>
        /// Reset health to max
        /// </summary>
        public void ResetHealth()
        {
            currentHealth = maxHealth;
            invincibilityTimer = 0f;
            onHealthChanged?.Invoke(currentHealth);
        }

        /// <summary>
        /// Handle death
        /// </summary>
        private void Die()
        {
            onDeath?.Invoke();

            if (destroyOnDeath)
            {
                if (deathDelay > 0)
                {
                    Destroy(gameObject, deathDelay);
                }
                else
                {
                    Destroy(gameObject);
                }
            }
        }
    }
}
