using UnityEngine;

namespace SickOfIt.Combat.Enemies
{
    /// <summary>
    /// Flips sprite X scale based on character position
    /// </summary>
    public class SpriteLookAtCharacter : MonoBehaviour
    {
        [Header("Target Settings")]
        [Tooltip("The character/player to look at. If null, will search for 'Player' tag")]
        [SerializeField] private Transform targetCharacter;
        
        [Header("Flip Settings")]
        [Tooltip("Invert the flip direction")]
        [SerializeField] private bool invertFlip = false;
        
        [Tooltip("Should flip on X axis")]
        [SerializeField] private bool flipX = true;
        
        [Tooltip("Minimum distance difference before flipping (prevents jitter)")]
        [SerializeField] private float flipThreshold = 0.1f;

        private Vector3 originalScale;

        private void Start()
        {
            // Store original scale
            originalScale = transform.localScale;
            
            // Find player if target not set
            if (targetCharacter == null)
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                {
                    targetCharacter = player.transform;
                }
                else
                {
                    Debug.LogWarning($"SpriteLookAtCharacter on {gameObject.name}: No target character assigned and no Player tag found!");
                }
            }
        }

        private void Update()
        {
            if (targetCharacter == null)
                return;

            UpdateFlip();
        }

        private void UpdateFlip()
        {
            // Calculate relative position
            float directionX = targetCharacter.position.x - transform.position.x;
            
            // Apply threshold to prevent jittering
            if (Mathf.Abs(directionX) < flipThreshold)
                return;
            
            // Determine if character is to the right
            bool characterIsRight = directionX > 0;
            
            // Apply invert logic
            if (invertFlip)
                characterIsRight = !characterIsRight;
            
            // Apply flip
            if (flipX)
            {
                Vector3 newScale = originalScale;
                newScale.x = characterIsRight ? Mathf.Abs(originalScale.x) : -Mathf.Abs(originalScale.x);
                transform.localScale = newScale;
            }
        }

        /// <summary>
        /// Set a new target character to look at
        /// </summary>
        public void SetTarget(Transform newTarget)
        {
            targetCharacter = newTarget;
        }

        /// <summary>
        /// Get the current direction to the character
        /// </summary>
        public Vector3 GetDirectionToCharacter()
        {
            if (targetCharacter == null)
                return Vector3.zero;
            
            return (targetCharacter.position - transform.position).normalized;
        }

        /// <summary>
        /// Check if character is to the right
        /// </summary>
        public bool IsCharacterRight()
        {
            if (targetCharacter == null)
                return false;
            
            return targetCharacter.position.x > transform.position.x;
        }
    }
}
