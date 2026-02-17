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
    public class Analog : MonoBehaviour, IDragHandler, IPointerDownHandler, IPointerUpHandler
    {
        public enum InitialState { Normal, Disabled }
        public enum AnalogState { Normal, Disabled }

        [Header("Behaviour")]
        [Tooltip("Initial state of the analog stick.")]
        public InitialState initialState = InitialState.Normal;

        [Min(0)]
        [Tooltip("Maximum distance the stick can move from its origin.")]
        public float analogRadius = 70f;

        [Min(0)]
        [Tooltip("Speed at which the stick returns to its origin.")]
        public float stickReturnSpeed = 10f;

        [Min(0)]
        [Tooltip("Speed at which the stick follows the pointer.")]
        public float stickFollowSpeed = 100f;

        [Tooltip("Smoothly interpolate stick movement.")]
        public bool smoothStickOnMovement = true;

        [Tooltip("Snap stick to pointer position on click.")]
        public bool snapStickOnClick = false;

        [Tooltip("Snap stick to origin on release.")]
        public bool snapStickOnRelease = false;

        [Header("References")]
        [Tooltip("RectTransform of the stick.")]
        public RectTransform StickTarget;

        [Header("Events")]
        public UnityEvent StickTapEvent;
        public UnityEvent StickDragEvent;
        public UnityEvent StickExitEvent;
        public UnityEvent EnableEvent;
        public UnityEvent DisableEvent;

        private bool isDragging = false;
        private bool isStickInOrigin = false;
        private Vector2 originalStickPosition;
        private Vector2 pointerStartPosition;

        private AnalogState analogState;
        private Animator anim;


        /**/


        #region Setup

        private void Start()
        {
            anim = GetComponent<Animator>();
            originalStickPosition = StickTarget.anchoredPosition;
            SetInitialState();
        }


        private void SetInitialState()
        {
            if (initialState == InitialState.Normal)
            {
                anim.SetTrigger("Normal");
                analogState = AnalogState.Normal;
            }
            else
            {
                anim.SetTrigger("Disable");
                analogState = AnalogState.Disabled;
            }
        }

        #endregion


        #region Update Stick

        private void Update()
        {
            if (isDragging || isStickInOrigin) return;

            if (snapStickOnRelease)
            {
                StickTarget.anchoredPosition = originalStickPosition;
                isStickInOrigin = true;
            }
            else
            {
                StickTarget.anchoredPosition = Vector2.Lerp(StickTarget.anchoredPosition, originalStickPosition, stickReturnSpeed * Time.deltaTime);
                if (Vector2.Distance(StickTarget.anchoredPosition, originalStickPosition) < 0.1f)
                {
                    StickTarget.anchoredPosition = originalStickPosition;
                    isStickInOrigin = true;
                }
            }
        }

        #endregion


        #region Pointer Events

        public void OnPointerDown(PointerEventData eventData)
        {
            if (analogState == AnalogState.Disabled) return;

            isDragging = true;
            isStickInOrigin = false;
            pointerStartPosition = eventData.position;

            Vector2 offset = snapStickOnClick ? Vector2.zero : (Vector2)StickTarget.position - eventData.position;
            StickTarget.position = pointerStartPosition + offset;

            StickTapEvent.Invoke();
        }


        public void OnDrag(PointerEventData eventData)
        {
            if (!isDragging) return;

            Vector2 pointerPosition = eventData.position;
            Vector2 direction = (pointerPosition - pointerStartPosition).normalized;
            float distance = Mathf.Clamp(Vector2.Distance(pointerPosition, pointerStartPosition), 0f, analogRadius);
            Vector2 newPosition = originalStickPosition + direction * distance;

            StickTarget.anchoredPosition = smoothStickOnMovement
                ? Vector2.Lerp(StickTarget.anchoredPosition, newPosition, stickFollowSpeed * Time.deltaTime)
                : newPosition;

            StickDragEvent.Invoke();
        }


        public void OnPointerUp(PointerEventData eventData)
        {
            isDragging = false;
            StickExitEvent.Invoke();
        }

        #endregion


        #region Getters / Setters

        public Vector2 GetStickPosition() => (StickTarget.anchoredPosition - originalStickPosition) / analogRadius;
        public AnalogState GetAnalogState() => analogState;


        public void Enable()
        {
            EnableEvent.Invoke();
            anim.SetTrigger("Normal");
            analogState = AnalogState.Normal;
        }


        public void Disable()
        {
            DisableEvent.Invoke();
            anim.SetTrigger("Disable");
            analogState = AnalogState.Disabled;
        }

        #endregion
    }
}