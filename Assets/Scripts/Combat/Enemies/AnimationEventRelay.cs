using UnityEngine;

namespace SickOfIt.Combat.Enemies
{
    /// <summary>
    /// Relays animation events from a child object (with Animator) to the parent EnemyShooter.
    /// Attach this to the same GameObject that has the Animator component.
    /// </summary>
    public class AnimationEventRelay : MonoBehaviour
    {
        private EnemyShooter _shooter;

        private void Awake()
        {
            _shooter = GetComponentInParent<EnemyShooter>();
            
            if (_shooter == null)
            {
                Debug.LogError($"AnimationEventRelay on '{gameObject.name}' could not find EnemyShooter in parent hierarchy.");
            }
        }

        /// <summary>
        /// Called by animation event to fire a projectile.
        /// Add this as an Animation Event on the frame where the projectile should spawn.
        /// </summary>
        public void FireProjectile()
        {
            if (_shooter != null)
            {
                _shooter.FireProjectile();
            }
        }
    }
}
