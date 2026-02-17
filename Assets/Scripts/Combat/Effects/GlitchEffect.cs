using UnityEngine;

namespace SickOfIt.Combat.Effects
{
    /// <summary>
    /// Creates a glitchy visual effect by flickering opacity and color
    /// Perfect for "AI glitch" aesthetic
    /// </summary>
    public class GlitchEffect : MonoBehaviour
    {
        [Header("Flicker Settings")]
        [SerializeField] private bool enableFlicker = true;
        [SerializeField] private float flickerSpeed = 10f;
        [SerializeField] private float minAlpha = 0.3f;
        [SerializeField] private float maxAlpha = 1f;

        [Header("Color Glitch")]
        [SerializeField] private bool enableColorGlitch = true;
        [SerializeField] private float colorGlitchSpeed = 5f;
        [SerializeField] private Color glitchColor1 = Color.cyan;
        [SerializeField] private Color glitchColor2 = Color.magenta;

        [Header("Position Jitter")]
        [SerializeField] private bool enableJitter = false;
        [SerializeField] private float jitterAmount = 0.05f;
        [SerializeField] private float jitterSpeed = 20f;

        private SpriteRenderer spriteRenderer;
        private Material material;
        private Vector3 originalPosition;
        private Color originalColor;
        private float flickerTimer = 0f;
        private float colorTimer = 0f;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();

            if (spriteRenderer != null)
            {
                // Create a material instance so each object has its own
                material = new Material(spriteRenderer.material);
                spriteRenderer.material = material;
                originalColor = spriteRenderer.color;
            }

            originalPosition = transform.localPosition;
        }

        private void Update()
        {
            if (spriteRenderer == null)
                return;

            // Flicker effect
            if (enableFlicker)
            {
                flickerTimer += Time.deltaTime * flickerSpeed;
                float alpha = Mathf.Lerp(minAlpha, maxAlpha, (Mathf.Sin(flickerTimer) + 1f) * 0.5f);

                Color currentColor = spriteRenderer.color;
                currentColor.a = alpha;
                spriteRenderer.color = currentColor;
            }

            // Color glitch
            if (enableColorGlitch)
            {
                colorTimer += Time.deltaTime * colorGlitchSpeed;
                Color glitchColor = Color.Lerp(glitchColor1, glitchColor2, (Mathf.Sin(colorTimer) + 1f) * 0.5f);
                spriteRenderer.color = Color.Lerp(originalColor, glitchColor, 0.5f);
            }

            // Position jitter
            if (enableJitter)
            {
                Vector3 jitter = new Vector3(
                    Mathf.PerlinNoise(Time.time * jitterSpeed, 0) - 0.5f,
                    Mathf.PerlinNoise(0, Time.time * jitterSpeed) - 0.5f,
                    0
                ) * jitterAmount;

                transform.localPosition = originalPosition + jitter;
            }
        }

        private void OnDestroy()
        {
            // Clean up material instance
            if (material != null)
            {
                Destroy(material);
            }
        }
    }
}
