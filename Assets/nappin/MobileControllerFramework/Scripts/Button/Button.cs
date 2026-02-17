using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;


namespace MobileControllerFramework
{
    /*
    * NOTE:
    * This component uses Unity's EventSystem (Pointer Events).
    * It works out of the box with both the Old Input Manager
    * and the New Input System — no additional setup required.
    */


    [RequireComponent(typeof(Animator))]
    public class Button : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        public enum InitialState { Normal, Disabled }
        public enum ButtonState { Normal, Tap, Hold, Disabled }

        [Header("Behaviour")]
        [Tooltip("Initial state of the button.")]
        public InitialState initialState = InitialState.Normal;

        [Tooltip("How long the button must be held before triggering a Hold event.")]
        [Min(0.1f)] public float holdTimeThreshold = 1.0f;

        [Tooltip("Enable or disable Hold functionality.")]
        public bool enableHold = false;

        [Header("Events")]
        public UnityEvent TapStartEvent;
        public UnityEvent TapEndEvent;
        public UnityEvent HoldStartEvent;
        public UnityEvent HoldEndEvent;
        public UnityEvent EnableEvent;
        public UnityEvent DisableEvent;

        private ButtonState buttonState;
        private Animator animator;

        private float currentHoldTime = 0f;
        private bool isHolding = false;


        /**/


        #region Setup

        private void Awake()
        {
            animator = GetComponent<Animator>();
            SetInitialState();
        }


        private void SetInitialState()
        {
            if (initialState == InitialState.Normal)
            {
                animator.SetTrigger("Normal");
                buttonState = ButtonState.Normal;
            }
            else
            {
                animator.SetTrigger("Disable");
                buttonState = ButtonState.Disabled;
            }
        }

        #endregion


        #region Pointer Events

        public void OnPointerDown(PointerEventData eventData)
        {
            if (buttonState == ButtonState.Disabled) return;

            TapStartEvent.Invoke();
            animator.SetTrigger("Tap");
            buttonState = ButtonState.Tap;

            isHolding = true;
        }


        public void OnPointerUp(PointerEventData eventData) => HandlePointerUp();
        public void OnPointerExit(PointerEventData eventData) => HandlePointerUp();


        private void HandlePointerUp()
        {
            switch (buttonState)
            {
                case ButtonState.Hold:
                    HoldEndEvent.Invoke();
                    animator.SetTrigger("Normal");
                    buttonState = ButtonState.Normal;
                    break;

                case ButtonState.Tap:
                    TapEndEvent.Invoke();
                    animator.SetTrigger("Normal");
                    buttonState = ButtonState.Normal;
                    ResetHold();
                    break;
            }
        }

        #endregion


        #region Update

        private void Update()
        {
            if (!isHolding || !enableHold) return;

            currentHoldTime += Time.deltaTime;
            if (currentHoldTime >= holdTimeThreshold)
            {
                HoldStartEvent.Invoke();
                animator.SetTrigger("Hold");
                buttonState = ButtonState.Hold;
                ResetHold();
            }
        }


        private void ResetHold()
        {
            isHolding = false;
            currentHoldTime = 0f;
        }

        #endregion


        #region Getters / Setters

        public ButtonState GetState() => buttonState;


        public void Enable()
        {
            EnableEvent.Invoke();
            animator.SetTrigger("Normal");
            buttonState = ButtonState.Normal;
        }


        public void Disable()
        {
            DisableEvent.Invoke();
            animator.SetTrigger("Disable");
            buttonState = ButtonState.Disabled;
        }

        #endregion
    }
}