using UnityEngine;
using SickOfIt.Combat.Health;

namespace SickOfIt.Combat.Enemies
{
    /// <summary>
    /// Base class for all enemy types
    /// </summary>
    [RequireComponent(typeof(HealthSystem))]
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(AudioSource))]
    public abstract class EnemyBase : MonoBehaviour
    {
        [Header("Enemy Settings")]
        [SerializeField] protected float moveSpeed = 2f;
        [SerializeField] protected int contactDamage = 1;

        [Header("Detection")]
        [SerializeField] protected float detectionRange = 10f;
        [SerializeField] protected LayerMask playerLayer;

        [Header("Audio")]
        [SerializeField] private AudioClip[] hurtSounds;
        [SerializeField] private AudioClip[] deathSounds;
        [SerializeField] [Range(0f, 1f)] [Tooltip("Play hurt sounds only when health is below this percentage (0 = always, 1 = never)")]
        private float hurtSoundHealthThreshold = 1f;
        [SerializeField] [Range(0f, 0.3f)] [Tooltip("Random pitch variation for audio variety")]
        private float pitchVariation = 0.1f;

        [SerializeField] protected SpriteRenderer spriteRenderer;
        protected Transform player;
        protected Rigidbody2D rb;
        protected HealthSystem healthSystem;
        protected AudioSource audioSource;

        protected virtual void Awake()
        {
            if (spriteRenderer == null)
                spriteRenderer = GetComponent<SpriteRenderer>();

            rb = GetComponent<Rigidbody2D>();
            healthSystem = GetComponent<HealthSystem>();
            audioSource = GetComponent<AudioSource>();

            // Setup rigidbody for 2D movement
            rb.gravityScale = 0; // Top-down style, no gravity
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;

            // Subscribe to health events
            if (healthSystem != null)
            {
                healthSystem.onDamaged.AddListener(OnDamaged);
                healthSystem.onDeath.AddListener(OnDeath);
            }

            // Find player (including inactive)
            var allObjects = FindObjectsOfType<GameObject>(true);
            var playerObj = System.Array.Find(allObjects, obj => obj.CompareTag("Player"));
            if (playerObj != null)
            {
                player = playerObj.transform;
            }
        }

        protected virtual void OnDestroy()
        {
            // Unsubscribe from health events
            if (healthSystem != null)
            {
                healthSystem.onDamaged.RemoveListener(OnDamaged);
                healthSystem.onDeath.RemoveListener(OnDeath);
            }
        }

        protected virtual void Update()
        {
            if (player == null || !healthSystem.IsAlive)
                return;

            // Check if player is in range
            float distanceToPlayer = Vector2.Distance(transform.position, player.position);
            if (distanceToPlayer <= detectionRange)
            {
                UpdateBehavior();
            }
        }

        /// <summary>
        /// Override this for specific enemy behavior
        /// </summary>
        protected abstract void UpdateBehavior();

        /// <summary>
        /// Deal damage to player on contact
        /// </summary>
        protected virtual void OnCollisionEnter2D(Collision2D collision)
        {
            if (collision.gameObject.CompareTag("Player"))
            {
                HealthSystem playerHealth = collision.gameObject.GetComponent<HealthSystem>();
                if (playerHealth != null)
                {
                    playerHealth.TakeDamage(contactDamage);
                }
            }
        }

        protected virtual void OnDrawGizmosSelected()
        {
            // Draw detection range
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, detectionRange);
        }

        /// <summary>
        /// Called when enemy takes damage
        /// </summary>
        private void OnDamaged()
        {
            // Check if health is below threshold
            float healthPercent = (float)healthSystem.CurrentHealth / healthSystem.MaxHealth;
            if (healthPercent <= hurtSoundHealthThreshold)
            {
                PlayRandomSound(hurtSounds);
            }
        }

        /// <summary>
        /// Called when enemy dies
        /// </summary>
        private void OnDeath()
        {
            PlayRandomSound(deathSounds);
        }

        /// <summary>
        /// Play a random audio clip from the provided array with pitch variation
        /// </summary>
        private void PlayRandomSound(AudioClip[] clips)
        {
            if (clips == null || clips.Length == 0 || audioSource == null)
                return;

            // Pick a random clip
            AudioClip clip = clips[Random.Range(0, clips.Length)];
            if (clip == null)
                return;

            // Apply pitch variation
            audioSource.pitch = 1f + Random.Range(-pitchVariation, pitchVariation);
            
            // Play the sound
            audioSource.PlayOneShot(clip);
        }
    }
}
