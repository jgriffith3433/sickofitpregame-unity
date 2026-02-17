using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace SickOfIt.Combat.Health
{
    /// <summary>
    /// Simple health UI that shows hearts/squares for player health
    /// </summary>
    public class HealthUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private HealthSystem playerHealth;
        [SerializeField] private Transform heartsContainer;
        [SerializeField] private GameObject heartPrefab;

        [Header("Settings")]
        [SerializeField] private bool autoFindPlayer = true;
        [SerializeField] private Color fullHealthColor = Color.white;
        [SerializeField] private Color emptyHealthColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);

        private List<Image> heartImages = new List<Image>();

        private void Start()
        {
            if (autoFindPlayer && playerHealth == null)
            {
                var allObjects = FindObjectsOfType<GameObject>(true);
                GameObject player = System.Array.Find(allObjects, obj => obj.CompareTag("Player"));
                if (player != null)
                {
                    playerHealth = player.GetComponent<HealthSystem>();
                }
            }

            if (playerHealth == null)
            {
                Debug.LogError("HealthUI: No player health found!");
                return;
            }

            // Listen to health changes
            playerHealth.onHealthChanged.AddListener(UpdateHealthDisplay);

            // Create hearts (initialized to full health)
            CreateHearts();

            // Don't call UpdateHealthDisplay here - the hearts are already white/full
            // They'll update correctly when health changes via the event listener
        }

        private void CreateHearts()
        {
            // Clear existing hearts
            foreach (Transform child in heartsContainer)
            {
                Destroy(child.gameObject);
            }
            heartImages.Clear();

            // Create new hearts with full health color initially
            for (int i = 0; i < playerHealth.MaxHealth; i++)
            {
                GameObject heart = Instantiate(heartPrefab, heartsContainer);
                Image heartImage = heart.GetComponent<Image>();
                if (heartImage != null)
                {
                    heartImage.color = fullHealthColor; // Initialize to full health
                    heartImages.Add(heartImage);
                }
            }
        }

        private void UpdateHealthDisplay(int currentHealth)
        {
            for (int i = 0; i < heartImages.Count; i++)
            {
                if (i < currentHealth)
                {
                    // Full heart
                    heartImages[i].color = fullHealthColor;
                }
                else
                {
                    // Empty heart
                    heartImages[i].color = emptyHealthColor;
                }
            }
        }
    }
}
