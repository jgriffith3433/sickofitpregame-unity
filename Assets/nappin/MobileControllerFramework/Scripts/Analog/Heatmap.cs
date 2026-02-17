using UnityEngine;
using UnityEngine.UI;


namespace MobileControllerFramework
{
    /*
    * NOTE:
    * This component relies on Analog Script.
    * It works out of the box with both the Old Input Manager
    * and the New Input System — no additional setup required.
    */


    [RequireComponent(typeof(Analog))]
    public class Heatmap : MonoBehaviour
    {
        [Header("Behaviour")]
        [Tooltip("Minimum stick magnitude to start showing the heatmap.")]
        [Range(0f, 1f)] public float transparencyThreshold = 0.35f;

        [Tooltip("Maximum alpha of the heatmap.")]
        [Range(0f, 1f)] public float maxAlpha = 0.9f;

        [Tooltip("Gradient used to color the heatmap based on magnitude.")]
        public Gradient colorGradient;

        [Header("References")]
        [Tooltip("Reference to the stick RectTransform.")]
        public RectTransform stickTarget;

        [Tooltip("Parent RectTransform of the heatmap.")]
        public RectTransform heatmapParent;

        [Tooltip("Image component representing the heatmap.")]
        public Image heatmapImage;

        private Analog analogStick;


        /**/


        #region Setup 

        private void Awake()
        {
            analogStick = GetComponent<Analog>();
        }

        #endregion


        #region Heatmap

        private void Update()
        {
            if (stickTarget == null || heatmapParent == null || heatmapImage == null || analogStick == null) return;

            UpdateRotation();
            UpdateColor();
        }


        private void UpdateRotation()
        {
            Vector2 dir = stickTarget.position - heatmapParent.position;
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            heatmapParent.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
        }


        private void UpdateColor()
        {
            float magnitude = analogStick.GetStickPosition().magnitude;

            // compute normalized step for gradient and alpha
            float heatStep = 0f;
            if (magnitude > transparencyThreshold)
            {
                heatStep = (magnitude - transparencyThreshold) / (1f - transparencyThreshold) * maxAlpha;
            }

            Color heatmapColor = colorGradient.Evaluate(heatStep);
            heatmapColor.a = heatStep;
            heatmapImage.color = heatmapColor;
        }

        #endregion
    }
}