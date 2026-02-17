using UnityEngine;

namespace SickOfIt.Combat.Enemies
{
    /// <summary>
    /// Enemy that chases the player
    /// </summary>
    public class EnemyChaser : EnemyBase
    {
        [Header("Chaser Settings")]
        [SerializeField] private float stopDistance = 0.5f; // How close before stopping
        [SerializeField] private bool faceMovementDirection = true;

        private Vector3 facingRight = Vector3.one;
        private Vector3 facingLeft = new Vector3(-1, 1, 1);

        protected override void UpdateBehavior()
        {
            if (player == null)
                return;

            float distanceToPlayer = Vector2.Distance(transform.position, player.position);

            // Only move if not too close
            if (distanceToPlayer > stopDistance)
            {
                // Move toward player
                Vector2 direction = (player.position - transform.position).normalized;
                Vector2 newPosition = Vector2.MoveTowards(
                    transform.position,
                    player.position,
                    moveSpeed * Time.deltaTime
                );

                rb.MovePosition(newPosition);

                // Face the direction of movement
                if (faceMovementDirection && spriteRenderer != null)
                {
                    if (direction.x > 0)
                    {
                        transform.localScale = facingRight;
                    }
                    else if (direction.x < 0)
                    {
                        transform.localScale = facingLeft;
                    }
                }
            }
        }
    }
}
