using System.Collections;
using UnityEngine;


namespace MobileControllerFramework
{
    [RequireComponent(typeof(Transform))]
    public class FPSCamera : MonoBehaviour
    {
        [Header("Camera Behaviour")]
        public float rotationSpeed = 5f;
        public float smoothness = 10f;
        public float shakeIntensity = 0.1f;
        public float shakeDuration = 0.5f;

        [Header("Mobile / Editor Multipliers")]
        public Vector2 mobileMoveMultiplier = new Vector2(3f, 1.3f);
        public Vector2 editorMoveMultiplier = Vector2.one;

        [Header("References")]
        public TouchDrag touchDrag;

        private Vector3 currentRotation;
        private Vector3 targetRotation;
        private Vector2 moveMultiplier;


        /**/


        #region Setup

        private void Start()
        {
            moveMultiplier = Application.isMobilePlatform ? mobileMoveMultiplier : editorMoveMultiplier;
            currentRotation = transform.localEulerAngles;
            targetRotation = currentRotation;

            if (touchDrag == null)
                Debug.LogWarning("FPSCamera: TouchDrag reference is missing.");
        }

        #endregion


        #region Camera Rotation

        private void Update()
        {
            HandleRotation();
        }


        private void HandleRotation()
        {
            if (touchDrag == null) return;

            Vector2 rotationInput = touchDrag.GetTouchDragMovement();

            // Apply input multipliers and rotation speed
            targetRotation.x += rotationInput.y * moveMultiplier.y * rotationSpeed;
            targetRotation.y += -rotationInput.x * moveMultiplier.x * rotationSpeed;

            // Clamp vertical rotation to prevent flipping
            targetRotation.x = Mathf.Clamp(targetRotation.x, -90f, 90f);

            // Smoothly interpolate rotation
            currentRotation = Vector3.Lerp(currentRotation, targetRotation, Time.deltaTime * smoothness);
            transform.localRotation = Quaternion.Euler(currentRotation);
        }

        #endregion


        #region Camera Shake

        public void ShakeCamera()
        {
            StartCoroutine(Shake());
        }


        private IEnumerator Shake()
        {
            Vector3 originalPosition = transform.localPosition;
            float elapsedTime = 0f;

            while (elapsedTime < shakeDuration)
            {
                Vector3 offset = new Vector3(
                    Random.Range(-shakeIntensity, shakeIntensity),
                    Random.Range(-shakeIntensity, shakeIntensity),
                    0f
                );

                transform.localPosition = originalPosition + offset;
                elapsedTime += Time.deltaTime;
                yield return null;
            }

            transform.localPosition = originalPosition;
        }

        #endregion
    }
}