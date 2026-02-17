using UnityEngine;


namespace MobileControllerFramework
{
    [RequireComponent(typeof(CharacterController))]
    public class FPSController : MonoBehaviour
    {
        [Header("Controller Settings")]
        [Min(0)] public float moveSpeed = 8f;
        [Min(0)] public float smoothMovement = 0.025f;
        public float normalHeight = 2f;
        public float crouchHeight = 1f;
        public float throwForce = 10f;

        [Header("References")]
        public Analog analog;
        public Swap swap;
        public GameObject type_1;
        public GameObject type_2;
        public GameObject type_3;
        public Animator gunAnim;

        private CharacterController characterController;
        private Transform cameraTransform;
        private Vector3 moveVelocity;
        private bool isCrouched = false;


        /**/


        #region Setup

        private void Start()
        {
            characterController = GetComponent<CharacterController>();
            cameraTransform = Camera.main?.transform;

            if (cameraTransform == null)
                Debug.LogError("FPSController: No Main Camera found in the scene.");
        }

        #endregion


        #region Movement

        private void Update()
        {
            HandleMovement();
        }


        private void HandleMovement()
        {
            if (analog == null || cameraTransform == null) return;

            Vector2 input = analog.GetStickPosition();
            Vector3 inputDir = cameraTransform.forward * input.y + cameraTransform.right * input.x;
            inputDir.y = 0; // prevent vertical movement from camera tilt
            inputDir.Normalize();

            // Apply movement
            Vector3 targetVelocity = inputDir * moveSpeed;
            moveVelocity.x = Mathf.Lerp(moveVelocity.x, targetVelocity.x, smoothMovement);
            moveVelocity.z = Mathf.Lerp(moveVelocity.z, targetVelocity.z, smoothMovement);

            // Apply gravity
            if (characterController.isGrounded && moveVelocity.y < 0) moveVelocity.y = -1f;
            moveVelocity.y += Physics.gravity.y * Time.deltaTime;

            characterController.Move(moveVelocity * Time.deltaTime);
        }

        #endregion


        #region Gun / Object Handling

        public void GunSwap()
        {
            if (swap == null) return;

            SwapContent currentItem = swap.GetCurrentItem();
            if (currentItem == null) return;

            type_1.SetActive((int)currentItem.contentType == 0);
            type_2.SetActive((int)currentItem.contentType == 1);
            type_3.SetActive((int)currentItem.contentType == 2);

            if (gunAnim != null)
            {
                gunAnim.Play("Idle");
                gunAnim.Play("Swap");
            }
        }


        public void ToggleHeight()
        {
            isCrouched = !isCrouched;
            characterController.height = isCrouched ? crouchHeight : normalHeight;
        }


        public void ThrowObject(GameObject objPrefab)
        {
            if (objPrefab == null || cameraTransform == null) return;

            // Instantiate object slightly in front of the camera
            Vector3 spawnPos = cameraTransform.position + cameraTransform.forward;
            GameObject throwable = Instantiate(objPrefab, spawnPos, Random.rotation);

            Rigidbody rb = throwable.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.AddForce(cameraTransform.forward * throwForce, ForceMode.Impulse);
            }
        }

        #endregion
    }
}