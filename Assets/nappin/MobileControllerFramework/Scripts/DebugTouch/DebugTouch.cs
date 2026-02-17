using UnityEngine;
using UnityEngine.EventSystems;


namespace MobileControllerFramework
{
    /*
    * NOTE:
    * This component uses Unity's EventSystem (Pointer Events).
    * It works out of the box with both the Old Input Manager
    * and the New Input System — no additional setup required.
    */


    [RequireComponent(typeof(RectTransform))]
    public class DebugTouch : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
    {
        [Header("Behaviour")]
        [Tooltip("Whether the system cursor should remain visible while moving.")]
        public bool displayCursorOnMove = true;

        [Header("References")]
        [Tooltip("RectTransform representing the touch cursor.")]
        public RectTransform touchRect;

        private bool isFollowing = false;
        private bool isMobile = false;
        private bool deviceChecked = false;


        /**/


        #region Setup

        private void CheckDevice()
        {
            isMobile = Application.platform == RuntimePlatform.Android ||
                       Application.platform == RuntimePlatform.IPhonePlayer;
            deviceChecked = true;
        }


        private void Awake()
        {
            if (!deviceChecked) CheckDevice();

            if (touchRect != null)
                touchRect.gameObject.SetActive(false);
        }

        #endregion


        #region Pointer Events

        public void OnPointerDown(PointerEventData eventData)
        {
            if (isMobile || touchRect == null) return;

            ShowTouch();
        }


        public void OnDrag(PointerEventData eventData)
        {
            if (!isFollowing || touchRect == null) return;

            MoveTouch(eventData.position);
        }


        public void OnPointerUp(PointerEventData eventData)
        {
            if (isMobile || touchRect == null) return;

            HideTouch();
        }

        #endregion


        #region Public Controls

        public void ShowTouch()
        {
            if (touchRect == null) return;

            isFollowing = true;
            touchRect.gameObject.SetActive(true);
            Cursor.visible = displayCursorOnMove;

            // Snap immediately to current input position
            Vector3 position = GetCurrentInputPosition();
            MoveTouch(position);
        }


        public void HideTouch()
        {
            if (touchRect == null) return;

            isFollowing = false;
            touchRect.gameObject.SetActive(false);
            Cursor.visible = true;
        }


        private void Update()
        {
            if (!isFollowing || touchRect == null) return;

            Vector3 position = GetCurrentInputPosition();
            MoveTouch(position);
        }

        #endregion


        #region Movement

        private void MoveTouch(Vector3 screenPosition)
        {
            if (touchRect == null) return;

            screenPosition.z = 0f;
            touchRect.position = screenPosition;
        }


        private Vector3 GetCurrentInputPosition()
        {
            Vector3 position = Input.mousePosition;

            if (Application.isMobilePlatform && Input.touchCount > 0)
            {
                position = Input.GetTouch(0).position;
            }

            return position;
        }

        #endregion
    }
}