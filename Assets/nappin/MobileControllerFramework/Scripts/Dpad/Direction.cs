using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static MobileControllerFramework.Dpad;


namespace MobileControllerFramework
{
    /*
    * NOTE:
    * This component uses Unity's EventSystem (Pointer Events).
    * It works out of the box with both the Old Input Manager
    * and the New Input System — no additional setup required.
    */


    [RequireComponent(typeof(Image))]
    public class Direction : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler, IPointerEnterHandler
    {
        public enum DirectionState { Normal, Tap, Disabled }

        [Header("References")]
        [Tooltip("Reference to the parent Dpad.")]
        public Dpad dpad;

        private DirectionState directionState;
        private Animator anim;
        private bool isPointerDown = false;


        /**/


        #region Setup

        private void Awake()
        {
            anim = GetComponent<Animator>();
        }

        #endregion


        #region Pointer Events

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (eventData.pointerPress != null)
            {
                isPointerDown = true;
                dpad.UpdateDirections();
            }
        }


        public void OnPointerDown(PointerEventData eventData)
        {
            isPointerDown = true;
            dpad.UpdateDirections();
        }


        public void OnPointerUp(PointerEventData eventData)
        {
            isPointerDown = false;
            dpad.ClearDirections();
        }


        public void OnPointerExit(PointerEventData eventData)
        {
            isPointerDown = false;
        }

        #endregion


        #region Getters / Setters

        public bool GetDragged() => isPointerDown;


        public void SetState(DpadState dpadState)
        {
            if (dpadState == DpadState.Normal)
            {
                anim?.SetTrigger("Normal");
                directionState = DirectionState.Normal;
            }
            else
            {
                anim?.SetTrigger("Disable");
                directionState = DirectionState.Disabled;
            }
        }


        public void SetDirectionState(bool isActive)
        {
            if (directionState == DirectionState.Normal && isActive)
            {
                anim?.SetTrigger("Tap");
                directionState = DirectionState.Tap;
            }
            else if (directionState == DirectionState.Tap && !isActive)
            {
                anim?.SetTrigger("Normal");
                directionState = DirectionState.Normal;
                isPointerDown = false;
            }
        }

        #endregion
    }
}