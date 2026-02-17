using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static MobileControllerFramework.ArrowKeys;


namespace MobileControllerFramework
{
    /*
    * NOTE:
    * This component uses Unity's EventSystem (Pointer Events).
    * It works out of the box with both the Old Input Manager
    * and the New Input System — no additional setup required.
    */


    [RequireComponent(typeof(Image))]
    public class Key : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler, IPointerEnterHandler
    {
        public enum KeyState { Normal, Tap, Disabled }

        [Header("References")]
        [Tooltip("Parent ArrowKeys controller.")]
        public ArrowKeys arrowKeys;

        private KeyState keyState;
        private Animator anim;
        private bool isPressed = false;


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
            if (eventData.pointerPress == null) return;
            isPressed = true;
            arrowKeys.UpdateKeys();
        }


        public void OnPointerDown(PointerEventData eventData)
        {
            isPressed = true;
            arrowKeys.UpdateKeys();
        }


        public void OnPointerUp(PointerEventData eventData)
        {
            isPressed = false;
            arrowKeys.ClearKeys();
        }


        public void OnPointerExit(PointerEventData eventData)
        {
            isPressed = false;
        }

        #endregion


        #region Getters / Setters

        public bool IsPressed() => isPressed;


        public void SetState(ArrowKeysState state)
        {
            keyState = state switch
            {
                ArrowKeysState.Normal => KeyState.Normal,
                ArrowKeysState.Disabled => KeyState.Disabled,
                _ => keyState
            };

            if (anim == null) return;

            if (keyState == KeyState.Normal) anim.SetTrigger("Normal");
            else if (keyState == KeyState.Disabled) anim.SetTrigger("Disable");
        }


        public void SetKeyState(bool pressed)
        {
            if (keyState == KeyState.Disabled) return;

            if (pressed && keyState != KeyState.Tap)
            {
                anim?.SetTrigger("Tap");
                keyState = KeyState.Tap;
            }
            else if (!pressed && keyState == KeyState.Tap)
            {
                anim?.SetTrigger("Normal");
                keyState = KeyState.Normal;
                isPressed = false;
            }
        }

        #endregion
    }
}