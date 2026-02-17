using UnityEngine;

namespace SickOfIt.Core.Input
{
    /// <summary>
    /// Add this component to platforms that can be dropped through.
    /// Automatically registers with the TopDownController on the player.
    /// </summary>
    public class DropThroughPlatform : MonoBehaviour
    {
        private Collider2D platformCollider;

        private void Awake()
        {
            platformCollider = GetComponent<Collider2D>();
        }

        private void Start()
        {
            RegisterWithPlayer();
        }

        private void RegisterWithPlayer()
        {
            TopDownController controller = FindAnyObjectByType<TopDownController>(FindObjectsInactive.Include);
            
            if (controller != null)
            {
                controller.RegisterPlatform(this);
            }
        }

        private void OnDestroy()
        {
            TopDownController controller = FindAnyObjectByType<TopDownController>(FindObjectsInactive.Include);
            if (controller != null)
            {
                controller.UnregisterPlatform(this);
            }
        }

        public void EnablePlatform()
        {
            if (platformCollider != null)
            {
                platformCollider.enabled = true;
            }
        }

        public void DisablePlatform()
        {
            if (platformCollider != null)
            {
                platformCollider.enabled = false;
            }
        }
    }
}
