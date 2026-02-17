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
    public class Toggle : MonoBehaviour, IPointerDownHandler
    {
        public enum InitialState { Normal, Disabled }
        public enum ToggleState { Normal, Toggled, Disabled }

        [Header("Behaviour")]
        [Tooltip("Initial state of the toggle.")]
        public InitialState initialState;

        [Header("Events")]
        public UnityEvent ToggleEvent;
        public UnityEvent EnableEvent;
        public UnityEvent DisableEvent;

        private ToggleState toggleState;
        private Animator anim;
        private bool isToggled = false;


        /**/


        #region Setup

        private void Awake()
        {
            anim = GetComponent<Animator>();

            toggleState = initialState == InitialState.Normal ? ToggleState.Normal : ToggleState.Disabled;
            anim.SetTrigger(toggleState == ToggleState.Normal ? "Normal" : "Disable");
        }

        #endregion


        #region Pointer Events

        public void OnPointerDown(PointerEventData eventData)
        {
            if (toggleState == ToggleState.Disabled) return;

            isToggled = !isToggled;
            anim.SetTrigger(isToggled ? "Toggle" : "Normal");
            ToggleEvent.Invoke();
        }

        #endregion


        #region Getters / Setters

        public ToggleState GetToggleState() => toggleState;


        public void Enable()
        {
            EnableEvent.Invoke();
            toggleState = ToggleState.Normal;
            anim.SetTrigger("Normal");
        }


        public void Disable()
        {
            DisableEvent.Invoke();
            toggleState = ToggleState.Disabled;
            anim.SetTrigger("Disable");
        }

        #endregion
    }
}